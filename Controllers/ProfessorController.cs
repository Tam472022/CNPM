using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Duan_CNPM.Data;
using Duan_CNPM.Models;

namespace Duan_CNPM.Controllers
{
    public class ProfessorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfessorController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        private int GetCurrentUserID()
        {
            var userID = HttpContext.Session.GetString("UserID");
            return string.IsNullOrEmpty(userID) ? 0 : int.Parse(userID);
        }

        private bool CheckAuth() => HttpContext.Session.GetString("Role") == "Professor";

        // DASHBOARD
        public async Task<IActionResult> Dashboard(string status = "All")
        {
            if (!CheckAuth())
                return RedirectToAction("Login", "Home");

            var userID = GetCurrentUserID();
            ViewBag.UserName = HttpContext.Session.GetString("FullName") ?? "Giảng viên";

            // Lấy danh sách tất cả dự án của giảng viên
            var allProjects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Council)
                .Where(p => p.ProfessorID == userID)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            // Set ViewBag CurrentStatus
            ViewBag.CurrentStatus = string.IsNullOrEmpty(status) ? "All" : status;

            // Lọc dự án theo status nếu không phải "All"
            IEnumerable<Project> projectsToShow = allProjects;

            if (!string.Equals(ViewBag.CurrentStatus, "All", StringComparison.OrdinalIgnoreCase))
            {
                projectsToShow = allProjects
                    .Where(p => string.Equals(p.Status, ViewBag.CurrentStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Thống kê
            ViewBag.TotalProjects = allProjects.Count(p => p.Status != "Rejected");
            ViewBag.PendingApprovals = allProjects.Count(p => p.Status == "Pending");
            ViewBag.InProgressProjects = allProjects.Count(p => p.Status == "Approved" || p.Status == "InProgress");

            // Hội đồng
            var councilAssignments = await _context.CouncilMembers
                .Where(cm => cm.ProfessorID == userID)
                .Include(cm => cm.Council)
                .ToListAsync();
            ViewBag.CouncilAssignments = councilAssignments.Count;

            // Thông báo chưa đọc
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();
            ViewBag.Notifications = notifications;

            // Debug log
            Console.WriteLine($"[DEBUG] ProfessorID: {userID}, TotalProjects: {allProjects.Count}, FilteredProjects: {projectsToShow.Count()}");

            return View(projectsToShow);
        }

        // PROJECTS
        public async Task<IActionResult> Projects()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var userID = GetCurrentUserID();
            if (userID == 0) return RedirectToAction("Login", "Home");

            var projects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Council)
                .Where(p => p.ProfessorID.HasValue && p.ProfessorID.Value == userID)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(projects);
        }

        // Sửa action ProjectDetail
        public async Task<IActionResult> ProjectDetail(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var project = await _context.Projects
                .Include(p => p.Professor)
                .Include(p => p.Student)
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectProgresses)
                .Include(p => p.Council)
                    .ThenInclude(c => c.CouncilMembers)
                        .ThenInclude(cm => cm.Professor)
                .Include(p => p.Scores)
                    .ThenInclude(s => s.CouncilMember)
                        .ThenInclude(cm => cm.Professor)
                .Include(p => p.ProfessorComments) // Thêm dòng này
                    .ThenInclude(pc => pc.Professor)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null) return NotFound();

            bool hasAccess = project.ProfessorID == GetCurrentUserID() ||
                             (project.Council != null && project.Council.CouncilMembers
                                 .Any(cm => cm.ProfessorID == GetCurrentUserID()));

            if (!hasAccess) return Forbid();

            return View(project);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCompletion(int id)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectProgresses)
                    .FirstOrDefaultAsync(p => p.ProjectID == id);

                if (project == null || project.ProfessorID != GetCurrentUserID())
                    return Json(new { success = false, message = "Project not found" });

                // Kiểm tra tiến độ đã đạt 100%
                var latestProgress = project.ProjectProgresses
                    .OrderByDescending(p => p.UpdateDate)
                    .FirstOrDefault();

                if (latestProgress == null || latestProgress.Percentage < 100)
                    return Json(new { success = false, message = "Đề tài chưa hoàn thành 100% tiến độ!" });

                // Cập nhật trạng thái
                project.Status = "Completed";
                project.EndDate = DateTime.Now;
                project.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Thông báo cho sinh viên
                await CreateNotification(project.StudentID, "Đề tài hoàn thành",
                    $"Giảng viên đã xác nhận đề tài '{project.Title}' hoàn thành!", "Success",
                    $"/Student/ProjectDetail/{id}");

                return Json(new { success = true, message = "Đã xác nhận đề tài hoàn thành!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProposeForCouncil(int id, string recommendation)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var project = await _context.Projects
                    .Include(p => p.Student)
                    .FirstOrDefaultAsync(p => p.ProjectID == id);

                if (project == null || project.ProfessorID != GetCurrentUserID())
                    return Json(new { success = false, message = "Project not found" });

                if (project.Status != "Completed")
                    return Json(new { success = false, message = "Đề tài chưa được xác nhận hoàn thành!" });

                // Lưu đề xuất vào comment
                project.ProfessorComment = $"[ĐỀ XUẤT HỘI ĐỒNG] {recommendation}";
                project.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Thông báo cho Admin
                var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
                foreach (var admin in admins)
                {
                    await CreateNotification(admin.UserID, "Đề xuất hội đồng",
                        $"Giảng viên đề xuất đưa đề tài '{project.Title}' vào hội đồng bảo vệ", "Info",
                        $"/Admin/Projects");
                }

                // Thông báo cho sinh viên
                await CreateNotification(project.StudentID, "Đề xuất hội đồng",
                    $"Giảng viên đã đề xuất đưa đề tài của bạn vào hội đồng bảo vệ", "Success",
                    $"/Student/ProjectDetail/{id}");

                return Json(new { success = true, message = "Đã gửi đề xuất đến Admin!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveProject(int id)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            var project = await _context.Projects.FindAsync(id);
            if (project == null || project.ProfessorID != GetCurrentUserID())
                return Json(new { success = false, message = "Project not found" });

            project.Status = "Approved";
            project.StartDate = DateTime.Now;
            project.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            await CreateNotification(project.StudentID, "Đề tài được duyệt",
                $"Đề tài '{project.Title}' đã được phê duyệt!", "Success",
                $"/Student/ProjectDetail/{id}");

            return Json(new { success = true, message = "Đã phê duyệt đề tài!" });
        }

        [HttpPost]
        public async Task<IActionResult> RejectProject(int id, string reason)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            var project = await _context.Projects.FindAsync(id);
            if (project == null || project.ProfessorID != GetCurrentUserID())
                return Json(new { success = false, message = "Project not found" });

            project.Status = "Rejected";
            project.ProfessorComment = reason;
            project.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            await CreateNotification(project.StudentID, "Đề tài bị từ chối",
                $"Đề tài '{project.Title}' đã bị từ chối. Lý do: {reason}", "Error",
                $"/Student/ProjectDetail/{id}");

            return Json(new { success = true, message = "Đã từ chối đề tài!" });
        }

        // Sửa action AddComment
        [HttpPost]
        public async Task<IActionResult> AddComment(int projectId, string comment)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return Json(new { success = false, message = "Project not found" });

            // Kiểm tra quyền
            bool hasAccess = project.ProfessorID == GetCurrentUserID();
            if (!hasAccess) return Json(new { success = false, message = "Không có quyền nhận xét đề tài này" });

            try
            {
                // Tạo comment mới
                var professorComment = new ProfessorComment
                {
                    ProjectID = projectId,
                    ProfessorID = GetCurrentUserID(),
                    CommentText = comment,
                    CreatedDate = DateTime.Now
                };

                _context.ProfessorComments.Add(professorComment);

                // Cập nhật thời gian project
                project.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Thông báo cho sinh viên
                await CreateNotification(project.StudentID, "Nhận xét mới",
                    $"Giảng viên đã thêm nhận xét cho đề tài: {project.Title}", "Info",
                    $"/Student/ProjectDetail/{projectId}");

                return Json(new { success = true, message = "Đã thêm nhận xét!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // COUNCILS
        public async Task<IActionResult> Councils()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var userID = GetCurrentUserID();
            if (userID == 0) return RedirectToAction("Login", "Home");

            // ✅ SỬA: Thay vì dùng Distinct(), dùng GroupBy hoặc query trực tiếp từ Councils
            var councils = await _context.Councils
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Student)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Scores)
                        .ThenInclude(s => s.CouncilMember)
                .Include(c => c.CouncilMembers)
                    .ThenInclude(cm => cm.Professor)
                .Where(c => c.CouncilMembers.Any(cm => cm.ProfessorID == userID))
                .OrderByDescending(c => c.DefenseDate)
                .ToListAsync();

            return View(councils);
        }

        public async Task<IActionResult> CouncilDetail(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var council = await _context.Councils
                .Include(c => c.CouncilMembers)
                    .ThenInclude(cm => cm.Professor)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Student)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Scores)
                        .ThenInclude(s => s.CouncilMember)
                .FirstOrDefaultAsync(c => c.CouncilID == id);

            if (council == null) return NotFound();

            if (!council.CouncilMembers.Any(cm => cm.ProfessorID == GetCurrentUserID()))
                return Forbid();

            return View(council);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitScore(int projectId, int councilMemberId, decimal score, string comment)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            var councilMember = await _context.CouncilMembers
                .FirstOrDefaultAsync(cm => cm.CouncilMemberID == councilMemberId &&
                                           cm.ProfessorID == GetCurrentUserID());

            if (councilMember == null)
                return Json(new { success = false, message = "Not authorized" });

            var existingScore = await _context.Scores
                .FirstOrDefaultAsync(s => s.ProjectID == projectId && s.CouncilMemberID == councilMemberId);

            if (existingScore != null)
            {
                existingScore.ScoreValue = score;
                existingScore.Comment = comment;
            }
            else
            {
                _context.Scores.Add(new Score
                {
                    ProjectID = projectId,
                    CouncilMemberID = councilMemberId,
                    ScoreValue = score,
                    Comment = comment,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await CalculateProjectFinalScore(projectId);

            return Json(new { success = true, message = "Đã lưu điểm!" });
        }

        private async Task CalculateProjectFinalScore(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Scores)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project == null || !project.Scores.Any()) return;

            project.FinalScore = project.Scores.Average(s => s.ScoreValue);

            var council = await _context.Councils
                .Include(c => c.CouncilMembers)
                .FirstOrDefaultAsync(c => c.CouncilID == project.CouncilID);

            if (council != null && project.Scores.Count == council.CouncilMembers.Count)
            {
                project.Status = "Completed";
                await CreateNotification(project.StudentID, "Hoàn thành bảo vệ",
                    $"Đề tài '{project.Title}' đã hoàn thành. Điểm: {project.FinalScore:F2}", "Success",
                    $"/Student/ProjectDetail/{projectId}");
            }

            await _context.SaveChangesAsync();
        }

        // STATISTICS
        public async Task<IActionResult> Statistics()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var userID = GetCurrentUserID();
            var projects = await _context.Projects
                .Include(p => p.Student)
                .Where(p => p.ProfessorID.HasValue && p.ProfessorID.Value == userID)
                .ToListAsync();

            ViewBag.TotalProjects = projects.Count;
            ViewBag.CompletedProjects = projects.Count(p => p.Status == "Completed");
            ViewBag.InProgressProjects = projects.Count(p => p.Status == "InProgress" || p.Status == "Approved");
            ViewBag.AverageScore = projects.Where(p => p.FinalScore.HasValue)
                                          .DefaultIfEmpty()
                                          .Average(p => p.FinalScore ?? 0);

            var projectsByYear = projects
                .GroupBy(p => p.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToList();

            ViewBag.ProjectsByYear = projectsByYear;

            return View();
        }

        // MESSAGES & NOTIFICATIONS
        public async Task<IActionResult> Messages()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var userID = GetCurrentUserID();
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderID == userID || m.ReceiverID == userID)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();

            return View(messages);
        }

        public async Task<IActionResult> Notifications()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var notifications = await _context.Notifications
                .Where(n => n.UserID == GetCurrentUserID())
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        // PROFILE
        public async Task<IActionResult> Profile()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");
            var user = await _context.Users.FindAsync(GetCurrentUserID());
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(IFormFile avatar)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            try
            {
                var user = await _context.Users.FindAsync(GetCurrentUserID());
                if (user == null) return NotFound();

                // Xử lý upload avatar nếu có file mới
                if (avatar != null && avatar.Length > 0)
                {
                    var webRoot = _environment?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsFolder = Path.Combine(webRoot, "uploads", "avatars");

                    // Tạo thư mục nếu chưa tồn tại
                    Directory.CreateDirectory(uploadsFolder);

                    // Tạo tên file unique
                    var safeFileName = Path.GetFileName(avatar.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }

                    // Xóa avatar cũ nếu có (không phải default)
                    if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar != "/images/default-avatar.png")
                    {
                        var oldFilePath = Path.Combine(webRoot, user.Avatar.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Cập nhật đường dẫn avatar mới
                    user.Avatar = $"/uploads/avatars/{uniqueFileName}";
                }

                // Lưu thay đổi
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                // ⭐ QUAN TRỌNG: Cập nhật Session với avatar mới
                HttpContext.Session.SetString("Avatar", user.Avatar ?? "/images/default-avatar.png");

                TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        // DELETE PROJECT
        [HttpPost]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectProgresses)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null)
                return Json(new { success = false, message = "Đề tài không tồn tại." });

            _context.ProjectFiles.RemoveRange(project.ProjectFiles);
            _context.ProjectProgresses.RemoveRange(project.ProjectProgresses);
            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa đề tài thành công!" });
        }

        // HELPER: Create Notification
        private async Task CreateNotification(int userID, string title, string message, string type, string link = "")
        {
            var notification = new Notification
            {
                UserID = userID,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Đổi mật khẩu
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            try
            {
                var userId = GetCurrentUserID();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });

                // Kiểm tra mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự!" });

                // Kiểm tra xác nhận mật khẩu
                if (newPassword != confirmPassword)
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp!" });

                // Kiểm tra mật khẩu mới không trùng mật khẩu cũ
                if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
                    return Json(new { success = false, message = "Mật khẩu mới không được trùng với mật khẩu hiện tại!" });

                // Cập nhật mật khẩu
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                // Thông báo
                await CreateNotification(userId, "Đổi mật khẩu thành công",
                    "Mật khẩu của bạn đã được thay đổi thành công.", "Success");

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
