using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Duan_CNPM.Data;
using Duan_CNPM.Models;
using ClosedXML.Excel;

namespace Duan_CNPM.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserID()
        {
            var userID = HttpContext.Session.GetString("UserID");
            return string.IsNullOrEmpty(userID) ? 0 : int.Parse(userID);
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            ViewBag.UserName = HttpContext.Session.GetString("FullName");

            // Statistics
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
            ViewBag.TotalProfessors = await _context.Users.CountAsync(u => u.Role == "Professor");
            ViewBag.TotalProjects = await _context.Projects.CountAsync();
            ViewBag.PendingProjects = await _context.Projects.CountAsync(p => p.Status == "Pending");
            ViewBag.InProgressProjects = await _context.Projects.CountAsync(p => p.Status == "InProgress");
            ViewBag.CompletedProjects = await _context.Projects.CountAsync(p => p.Status == "Completed");
            ViewBag.TotalCouncils = await _context.Councils.CountAsync();

            // Recent activities
            var recentProjects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Professor)
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentProjects = recentProjects;

            // Projects by status
            // Controller C#
            var allStatus = new[] { "Pending", "Approved", "InProgress", "Completed", "Rejected" };

            var projectsByStatus = allStatus
                .GroupJoin(
                    _context.Projects,
                    status => status,
                    project => project.Status,
                    (status, projects) => new {
                        label = status switch
                        {
                            "Pending" => "Chờ duyệt",
                            "Approved" => "Đã duyệt",
                            "InProgress" => "Đang thực hiện",
                            "Completed" => "Hoàn thành",
                            "Rejected" => "Bị từ chối",
                            _ => status
                        },
                        count = projects.Count()
                    }
                )
                .ToList();

            ViewBag.ProjectsByStatus = projectsByStatus;


            return View();
        }

        // User Management
        public async Task<IActionResult> Users(string role = "", string search = "")
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.Username.Contains(search) || 
                                       u.FullName.Contains(search) ||
                                       u.Email.Contains(search));

            var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();

            ViewBag.Role = role;
            ViewBag.Search = search;

            return View(users);
        }

        // Thêm người dùng mới:
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user, string password)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            try
            {
                // Kiểm tra username
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ViewBag.Message = "Tên đăng nhập đã tồn tại!";
                    return View(user);
                }

                // Hash password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.CreatedDate = DateTime.Now;
                user.IsActive = true;

                // Nếu User có Email/Phone hoặc chuyên ngành, map từ form nếu muốn
                if (!string.IsNullOrEmpty(Request.Form["StudentEmail"]))
                    user.Email = Request.Form["StudentEmail"];
                else if (!string.IsNullOrEmpty(Request.Form["ProfessorEmail"]))
                    user.Email = Request.Form["ProfessorEmail"];

                if (!string.IsNullOrEmpty(Request.Form["StudentPhone"]))
                    user.Phone = Request.Form["StudentPhone"];
                else if (!string.IsNullOrEmpty(Request.Form["ProfessorPhone"]))
                    user.Phone = Request.Form["ProfessorPhone"];

                if (!string.IsNullOrEmpty(Request.Form["StudentMajor"]))
                    user.Major = Request.Form["StudentMajor"];
                else if (!string.IsNullOrEmpty(Request.Form["ProfessorMajor"]))
                    user.Major = Request.Form["ProfessorMajor"];

                // Nếu User có MSSV, lưu:
                if (!string.IsNullOrEmpty(Request.Form["StudentCode"]))
                    user.StudentCode = Request.Form["StudentCode"];

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await CreateAuditLog("Create User", "Users", user.UserID);

                ViewBag.Message = "Tạo người dùng thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Có lỗi xảy ra: " + ex.Message;
                return View(user);
            }
        }

        // Thêm action này vào AdminController
        [HttpGet]
        public async Task<IActionResult> GetProfessors()
        {
            if (!CheckAuth()) return Json(new { success = false });

            var professors = await _context.Users
                .Where(u => u.Role == "Professor" && u.IsActive)
                .Select(u => new { u.UserID, u.FullName })
                .ToListAsync();

            return Json(professors);
        }
        
        // Thay đổi thông tin người dùng:
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            try
            {
                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null) return NotFound();
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Phone = user.Phone;
                existingUser.Role = user.Role;
                existingUser.Major = user.Major;
                existingUser.StudentCode = user.StudentCode;
                existingUser.IsActive = user.IsActive;
                await _context.SaveChangesAsync();
                await CreateAuditLog("Update User", "Users", user.UserID);
                TempData["Success"] = "Cập nhật người dùng thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(user);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditUserField(int id, string field, string value)
        {
            if (!CheckAuth())
                return Json(new { success = false, message = "Unauthorized" });
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            try
            {
                switch (field)
                {
                    case "FullName":
                        user.FullName = value;
                        break;
                    case "Email":
                        user.Email = value;
                        break;
                    case "PhoneNumber":
                        user.Phone = value;
                        break;
                    case "Major":
                        user.Major = value;
                        break;
                    default:
                        return Json(new { success = false, message = "Mục không hợp lệ" });
                }
                await _context.SaveChangesAsync();
                await CreateAuditLog($"Cập nhật {field}", "Users", id);
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });

                if (user.Role == "Admin")
                    return Json(new { success = false, message = "Không thể xóa Admin!!" });

                if (user.Role == "Professor")
                {
                    bool hasProject = await _context.Projects.AnyAsync(p => p.ProfessorID == id);
                    if (hasProject)
                        return Json(new { success = false, message = "Không thể xóa giảng viên đang được phân công đề tài!" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await CreateAuditLog("Delete User", "Users", id);

                return Json(new { success = true, message = "Đã xóa người dùng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;

                await _context.SaveChangesAsync();
                await CreateAuditLog("Reset Password", "Users", id);

                return Json(new { success = true, message = "Đã đặt lại mật khẩu!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Project Management
        public async Task<IActionResult> Projects(string status = "", int year = 0, string search = "")
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var query = _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Professor)
                .Include(p => p.Council)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (year > 0)
                query = query.Where(p => p.Year == year);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search) || 
                                       p.Student.FullName.Contains(search));

            var projects = await query.OrderByDescending(p => p.CreatedDate).ToListAsync();

            ViewBag.Status = status;
            ViewBag.Year = year;
            ViewBag.Search = search;

            return View(projects);
        }

        [HttpPost]
        public async Task<IActionResult> AssignProfessor(int projectId, int professorId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                    return Json(new { success = false, message = "Project not found" });

                project.ProfessorID = professorId;
                project.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // Notify professor
                await CreateNotification(professorId, "Phân công hướng dẫn",
                    $"Bạn được phân công hướng dẫn đề tài: {project.Title}", "Info",
                    $"/Professor/ProjectDetail/{projectId}");

                // Notify student
                await CreateNotification(project.StudentID, "Giảng viên hướng dẫn",
                    $"Đề tài của bạn đã được phân công giảng viên hướng dẫn", "Success",
                    $"/Student/ProjectDetail/{projectId}");

                await CreateAuditLog("Assign Professor", "Projects", projectId);

                return Json(new { success = true, message = "Đã phân công giảng viên!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Council Management
        // Thay thế action Councils trong AdminController.cs
        public async Task<IActionResult> Councils()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var councils = await _context.Councils
                .Include(c => c.CouncilMembers)
                    .ThenInclude(cm => cm.Professor)
                .Include(c => c.Projects)
                .OrderByDescending(c => c.DefenseDate)
                .ToListAsync();

            // ✅ Lấy danh sách đề tài được đề xuất bảo vệ
            var proposedProjects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Professor)
                .Where(p => p.Status == "Completed"
                         && p.CouncilID == null
                         && !string.IsNullOrEmpty(p.ProfessorComment)
                         && p.ProfessorComment.Contains("[ĐỀ XUẤT HỘI ĐỒNG]"))
                .OrderByDescending(p => p.UpdatedDate)
                .ToListAsync();

            ViewBag.ProposedProjects = proposedProjects;

            return View(councils);
        }

        // Thay thế cả 2 action CreateCouncil trong AdminController.cs

        // Thay thế cả 2 action CreateCouncil trong AdminController.cs

        [HttpGet]
        public async Task<IActionResult> CreateCouncil(int? projectId)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            ViewBag.Professors = await _context.Users
                .Where(u => u.Role == "Professor" && u.IsActive)
                .Select(u => new { u.UserID, u.FullName })
                .ToListAsync();

            // ✅ CHỈ LẤY CÁC ĐỀ TÀI ĐÃ ĐƯỢC ĐỀ XUẤT
            var proposedProjects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Professor)
                .Where(p => p.Status == "Completed"
                         && p.CouncilID == null
                         && p.ProfessorComment != null
                         && p.ProfessorComment.Contains("[ĐỀ XUẤT HỘI ĐỒNG]"))
                .OrderByDescending(p => p.UpdatedDate)
                .ToListAsync();

            ViewBag.ProposedProjects = proposedProjects;

            // Nếu có projectId được chọn từ trang Councils
            if (projectId.HasValue)
            {
                ViewBag.PreSelectedProjectId = projectId.Value;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCouncil(
    Council council,
    List<int> memberIds,
    string memberRoles,
    List<int> projectIds)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            try
            {
                // Validate: Phải có ít nhất 1 đề tài
                if (projectIds == null || !projectIds.Any())
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một đề tài!";
                    return RedirectToAction("CreateCouncil");
                }

                // Validate: Phải có thành viên hội đồng
                if (memberIds == null || !memberIds.Any())
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một giảng viên làm thành viên hội đồng!";
                    return RedirectToAction("CreateCouncil");
                }

                // ✅ SỬA LẠI PHẦN NÀY - Lấy từng project riêng lẻ thay vì dùng Contains
                var validProjects = new List<Project>();

                foreach (var projectId in projectIds)
                {
                    var project = await _context.Projects
                        .Include(p => p.Student)
                        .FirstOrDefaultAsync(p => p.ProjectID == projectId);

                    if (project != null
                        && project.Status == "Completed"
                        && project.CouncilID == null
                        && !string.IsNullOrEmpty(project.ProfessorComment)
                        && project.ProfessorComment.Contains("[ĐỀ XUẤT HỘI ĐỒNG]"))
                    {
                        validProjects.Add(project);
                    }
                }

                if (validProjects.Count != projectIds.Count)
                {
                    TempData["Error"] = "Chỉ có thể tạo hội đồng với các đề tài đã được giảng viên đề xuất!";
                    return RedirectToAction("CreateCouncil");
                }

                // 1. Gán các giá trị mặc định
                if (string.IsNullOrWhiteSpace(council.Description))
                    council.Description = "Không có mô tả";

                if (string.IsNullOrWhiteSpace(council.Location))
                    council.Location = "Chưa xác định";

                if (council.DefenseDate == default)
                    council.DefenseDate = DateTime.Now.AddDays(7);

                council.CreatedDate = DateTime.Now;

                // 2. Thêm hội đồng vào DB
                _context.Councils.Add(council);
                await _context.SaveChangesAsync();

                // 3. Thêm các thành viên hội đồng
                if (!string.IsNullOrWhiteSpace(memberRoles))
                {
                    var roles = memberRoles.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(r => r.Trim())
                                          .Where(r => !string.IsNullOrEmpty(r))
                                          .ToList();

                    if (memberIds.Count != roles.Count)
                    {
                        _context.Councils.Remove(council);
                        await _context.SaveChangesAsync();

                        TempData["Error"] = $"Số lượng vai trò ({roles.Count}) phải khớp với số lượng giảng viên ({memberIds.Count})!";
                        return RedirectToAction("CreateCouncil");
                    }

                    for (int i = 0; i < memberIds.Count; i++)
                    {
                        var member = new CouncilMember
                        {
                            CouncilID = council.CouncilID,
                            ProfessorID = memberIds[i],
                            Role = roles[i],
                            AssignedDate = DateTime.Now
                        };
                        _context.CouncilMembers.Add(member);
                    }

                    await _context.SaveChangesAsync();

                    foreach (var professorId in memberIds)
                    {
                        await CreateNotification(professorId,
                            "Phân công hội đồng",
                            $"Bạn được phân công vào hội đồng: {council.Name}",
                            "Info",
                            $"/Professor/CouncilDetail/{council.CouncilID}");
                    }
                }

                // 4. Gán đề tài vào hội đồng - ✅ SỬA LẠI: Dùng validProjects đã lấy sẵn
                foreach (var project in validProjects)
                {
                    project.CouncilID = council.CouncilID;
                    project.DefenseDate = council.DefenseDate;

                    if (!string.IsNullOrEmpty(project.ProfessorComment))
                    {
                        project.ProfessorComment = project.ProfessorComment
                            .Replace("[ĐỀ XUẤT HỘI ĐỒNG]", "[ĐÃ XẾP VÀO HỘI ĐỒNG]");
                    }

                    await CreateNotification(project.StudentID,
                        "Lịch bảo vệ",
                        $"Đề tài của bạn được xếp lịch bảo vệ: {council.DefenseDate:dd/MM/yyyy HH:mm} tại {council.Location}",
                        "Success",
                        $"/Student/ProjectDetail/{project.ProjectID}");

                    if (project.ProfessorID.HasValue)
                    {
                        await CreateNotification(project.ProfessorID.Value,
                            "Đề tài được xếp vào hội đồng",
                            $"Đề tài '{project.Title}' đã được xếp vào hội đồng bảo vệ",
                            "Success",
                            $"/Professor/ProjectDetail/{project.ProjectID}");
                    }
                }

                // 5. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // 6. Ghi log
                await CreateAuditLog("Create Council", "Councils", council.CouncilID);

                TempData["Success"] = $"Tạo hội đồng thành công với {validProjects.Count} đề tài!";
                return RedirectToAction("Councils");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("CreateCouncil");
            }
        }

        // Thêm action này vào AdminController
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
                .Include(p => p.ProfessorComments)
                    .ThenInclude(pc => pc.Professor)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null) return NotFound();

            return View(project);
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
                    .ThenInclude(p => p.ProjectFiles) // <-- thêm dòng này
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Scores)
                        .ThenInclude(s => s.CouncilMember)
                            .ThenInclude(cm => cm.Professor)
                .FirstOrDefaultAsync(c => c.CouncilID == id);

            if (council == null) return NotFound();

            return View(council);
        }

        // Statistics & Reports (giao diện thống kê)
        public async Task<IActionResult> Statistics()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var currentYear = DateTime.Now.Year;

            // Projects by year
            var projectsByYear = await _context.Projects
                .GroupBy(p => p.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // Projects by status
            var allStatus = new[] { "Pending", "Approved", "InProgress", "Completed", "Rejected" };
            var projectsByStatus = allStatus
                .GroupJoin(
                    _context.Projects,
                    status => status,
                    project => project.Status,
                    (status, projects) => new { 
                        // Pair gồm label và count
                        label = status switch
                        {
                            "Pending" => "Chờ duyệt",
                            "Approved" => "Đã duyệt",
                            "InProgress" => "Đang thực hiện",
                            "Completed" => "Hoàn thành",
                            "Rejected" => "Bị từ chối",
                            _ => status
                        },
                        count = projects.Count()
                    }
                )
                .ToList();

            // Top professors
            var topProfessors = await _context.Projects
                .Include(p => p.Professor)
                .Where(p => p.ProfessorID.HasValue && p.Year == currentYear)
                .GroupBy(p => p.Professor)
                .Select(g => new { 
                    Professor = g.Key.FullName, 
                    Count = g.Count(),
                    AvgScore = g.Where(p => p.FinalScore.HasValue).Average(p => p.FinalScore)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Score distribution
            var scoreDistribution = await _context.Projects
                .Where(p => p.FinalScore.HasValue && p.Year == currentYear)
                .GroupBy(p => Math.Floor((decimal)p.FinalScore))
                .Select(g => new { Score = g.Key, Count = g.Count() })
                .OrderBy(x => x.Score)
                .ToListAsync();

            ViewBag.ProjectsByStatus = projectsByStatus;
            ViewBag.ProjectsByYear = projectsByYear;
            ViewBag.TopProfessors = topProfessors;
            ViewBag.ScoreDistribution = scoreDistribution;

            return View();
        }

        public async Task<IActionResult> ExportProjects(int year = 0)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var query = _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Professor)
                .Include(p => p.Council)
                .AsQueryable();

            if (year > 0)
                query = query.Where(p => p.Year == year);

            var projects = await query.OrderBy(p => p.Student.FullName).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Projects");

            // Headers
            worksheet.Cell(1, 1).Value = "STT";
            worksheet.Cell(1, 2).Value = "Mã SV";
            worksheet.Cell(1, 3).Value = "Họ tên SV";
            worksheet.Cell(1, 4).Value = "Tên đề tài";
            worksheet.Cell(1, 5).Value = "Giảng viên HD";
            worksheet.Cell(1, 6).Value = "Trạng thái";
            worksheet.Cell(1, 7).Value = "Điểm";
            worksheet.Cell(1, 8).Value = "Năm";
            worksheet.Cell(1, 9).Value = "Học kỳ";

            // Data
            for (int i = 0; i < projects.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = i + 1;
                worksheet.Cell(row, 2).Value = projects[i].Student.StudentCode ?? "";
                worksheet.Cell(row, 3).Value = projects[i].Student.FullName;
                worksheet.Cell(row, 4).Value = projects[i].Title;
                worksheet.Cell(row, 5).Value = projects[i].Professor?.FullName ?? "";
                worksheet.Cell(row, 6).Value = projects[i].Status;
                worksheet.Cell(row, 7).Value = projects[i].FinalScore?.ToString("F2") ?? "";
                worksheet.Cell(row, 8).Value = projects[i].Year;
                worksheet.Cell(row, 9).Value = projects[i].Semester;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachDoAn_{year}_{DateTime.Now:yyyyMMdd}.xlsx");
        }


        // System Config
        public async Task<IActionResult> SystemConfig()
        {
            if (!CheckAuth())
                return RedirectToAction("Login", "Home");

            var configs = await _context.SystemConfigs.ToListAsync();
            return View(configs);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConfig(int id, string value)
        {
            if (!CheckAuth())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(value))
                    return Json(new { success = false, message = "Giá trị không được để trống!" });

                var config = await _context.SystemConfigs.FindAsync(id);
                if (config == null)
                    return Json(new { success = false, message = "Config not found" });

                // Validate SEMESTER
                if (config.ConfigKey.Equals("SEMESTER", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(value, out int semester))
                        return Json(new { success = false, message = "Học kỳ phải là số!" });

                    if (semester != 1 && semester != 2)
                        return Json(new { success = false, message = "Học kỳ chỉ được nhập 1 hoặc 2!" });

                    value = semester.ToString(); // chuẩn hóa
                }

                config.ConfigValue = value;
                config.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                await CreateAuditLog("Update Config", "SystemConfigs", id);

                return Json(new { success = true, message = "Đã cập nhật cấu hình!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // Audit Logs
        public async Task<IActionResult> AuditLogs(int page = 1, int pageSize = 50)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");

            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalLogs = await _context.AuditLogs.CountAsync();

            return View(logs);
        }

        // Helper methods
        private async Task CreateAuditLog(string action, string tableName, int? recordID)
        {
            var log = new AuditLog
            {
                UserID = GetCurrentUserID(),
                Action = action,
                TableName = tableName,
                RecordID = recordID,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedDate = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

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
    }
}