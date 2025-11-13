using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Duan_CNPM.Data;
using Duan_CNPM.Models;

namespace Duan_CNPM.Controllers {
    public class StudentController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentController(ApplicationDbContext context, IWebHostEnvironment environment) {
            _context = context;
            _environment = environment;
        }

        private int GetCurrentUserID() {
            var userID = HttpContext.Session.GetString("UserID");
            return string.IsNullOrEmpty(userID) ? 0 : int.Parse(userID);
        }

        private bool CheckAuth() {
            return HttpContext.Session.GetString("Role") == "Student";
        }

        public async Task<IActionResult> Dashboard(string status = "All") {
            if (!CheckAuth()) return RedirectToAction("Login", "Home"); 
            var userID = GetCurrentUserID();
            ViewBag.UserName = HttpContext.Session.GetString("FullName");
            ViewBag.CurrentStatus = status; 
            // Lấy danh sách dự án của sinh viên
            var projectsQuery = _context.Projects
                .Include(p => p.Professor)
                .Where(p => p.StudentID == userID)
                .AsQueryable(); 
            // Nếu status khác "All", lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "All") {
                projectsQuery = projectsQuery.Where(p => p.Status == status);
            } 
            var projects = await projectsQuery
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync(); 
            // Đếm số lượng dự án
            ViewBag.TotalProjects = projects.Count - projects.Count(p => p.Status == "Rejected");
            ViewBag.PendingProjects = projects.Count(p => p.Status == "Pending");
            ViewBag.CompletedProjects = projects.Count(p => p.Status == "Completed");
            ViewBag.InProgressProjects = projects.Count(p => p.Status == "Approved" || p.Status == "InProgress")
                - projects.Count(p => p.Status == "Completed"); 
            // Lấy 5 thông báo chưa đọc gần nhất
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();
            ViewBag.Notifications = notifications; 
            return View(projects);
        }


        // Project Management
        [HttpGet]
        public IActionResult CreateProject() {
            // Không đúng Student thì về trang chủ (dùng cho đăng xuất)
            if (!CheckAuth()) return RedirectToAction("Login", "Home");
            // Xóa thông báo TempData cũ để không hiển thị lại
            TempData.Remove("Success");
            TempData.Remove("Error");
            // Kiểm tra có giảng viên nào:
            ViewBag.Professors = _context.Users
                .Where(u => u.Role == "Professor" && u.IsActive)
                .Select(u => new { u.UserID, u.FullName })
                .ToList();
            // Trả về View:
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject(Project project) {
            // Không đúng Student thì về trang chủ (dùng cho đăng xuất)
            if (!CheckAuth()) return RedirectToAction("Login", "Home");
            // Xử lí bất đồng bộ:
            try {
                // Lây mã sinh viên:
                project.StudentID = GetCurrentUserID();
                // Vừa tạo là phải chờ duyệt:
                project.Status = "Pending";
                // Lấy thời gian thực:
                project.CreatedDate = DateTime.Now;
                var config = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "CurrentYear");
                project.Year = config != null ? int.Parse(config.ConfigValue) : DateTime.Now.Year;
                // Học kì:
                var semesterConfig = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "CurrentSemester");
                project.Semester = semesterConfig != null ? int.Parse(semesterConfig.ConfigValue) : 1;
                // Thêm dự án và chờ duyệt:
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                // Thông báo cho giảng viên:
                if (project.ProfessorID.HasValue) {
                    await CreateNotification(project.ProfessorID.Value, "Đề tài mới",
                        $"Sinh viên đã đăng ký đề tài: {project.Title}", "Info", $"/Professor/ProjectDetail/{project.ProjectID}");
                }
                // Thông báo tạo đề tài thành công:
                TempData["Success"] = "Tạo đề tài thành công!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex) {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                // Trả về View với số lượng project của sinh viên:
                return View(project);
            }
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
                .FirstOrDefaultAsync(p => p.ProjectID == id && p.StudentID == GetCurrentUserID());

            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgress(int projectId, string title, string description, int percentage)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectProgresses)
                    .FirstOrDefaultAsync(p => p.ProjectID == projectId);

                if (project == null || project.StudentID != GetCurrentUserID())
                    return Json(new { success = false, message = "Project not found" });

                // Validate percentage
                if (percentage < 0 || percentage > 100)
                {
                    return Json(new { success = false, message = "Tiến độ phải trong khoảng 0-100%" });
                }

                // Lấy tiến độ hiện tại (cao nhất)
                var currentProgress = project.ProjectProgresses
                    .OrderByDescending(p => p.Percentage)
                    .FirstOrDefault();

                int currentPercentage = currentProgress?.Percentage ?? 0;

                // Kiểm tra không được giảm tiến độ
                if (percentage < currentPercentage)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Tiến độ không thể giảm xuống! Tiến độ hiện tại là {currentPercentage}%, bạn chỉ có thể cập nhật từ {currentPercentage}% trở lên."
                    });
                }

                // Kiểm tra không được trùng tiến độ
                if (percentage == currentPercentage)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Tiến độ hiện tại đã là {currentPercentage}%. Vui lòng cập nhật tiến độ cao hơn."
                    });
                }

                // Thêm tiến độ mới
                var progress = new ProjectProgress
                {
                    ProjectID = projectId,
                    Title = title,
                    Description = description,
                    Percentage = percentage,
                    UpdateDate = DateTime.Now,
                    UpdatedBy = GetCurrentUserID()
                };

                _context.ProjectProgresses.Add(progress);
                project.UpdatedDate = DateTime.Now;

                // Tự động chuyển trạng thái khi đạt 100%
                if (percentage == 100 && project.Status != "Completed")
                {
                    project.Status = "InProgress"; // Vẫn để InProgress, chờ giảng viên đánh giá
                }

                await _context.SaveChangesAsync();

                // Thông báo cho giảng viên
                if (project.ProfessorID.HasValue)
                {
                    string progressInfo = percentage == 100
                        ? $"Sinh viên đã hoàn thành 100% tiến độ đề tài: {project.Title}"
                        : $"Sinh viên đã cập nhật tiến độ ({currentPercentage}% → {percentage}%) đề tài: {project.Title}";

                    await CreateNotification(project.ProfessorID.Value, "Cập nhật tiến độ",
                        progressInfo, "Info",
                        $"/Professor/ProjectDetail/{projectId}");
                }

                return Json(new { success = true, message = "Cập nhật tiến độ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(int projectId, IFormFile file, string fileType) {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            try {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null || project.StudentID != GetCurrentUserID())
                    return Json(new { success = false, message = "Project not found" });
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "No file uploaded" });
                // Check file size
                var maxSize = await _context.SystemConfigs
                    .FirstOrDefaultAsync(c => c.ConfigKey == "MaxFileSize");
                var maxFileSize = maxSize != null ? long.Parse(maxSize.ConfigValue) : 10485760;
                if (file.Length > maxFileSize)
                    return Json(new { success = false, message = "File quá lớn!" });
                // Save file
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "projects");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) {
                    await file.CopyToAsync(stream);
                }
                // Save to database
                var projectFile = new ProjectFile {
                    ProjectID = projectId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/projects/{uniqueFileName}",
                    FileType = fileType,
                    FileSize = file.Length,
                    UploadDate = DateTime.Now,
                    UploadedBy = GetCurrentUserID()
                };
                _context.ProjectFiles.Add(projectFile);
                await _context.SaveChangesAsync();
                // Thông báo:
                return Json(new { success = true, message = "Upload thành công!" });
            }
            catch (Exception ex) {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Xóa file
        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var file = await _context.ProjectFiles
                    .Include(f => f.Project)
                    .FirstOrDefaultAsync(f => f.FileID == fileId);

                if (file == null)
                    return Json(new { success = false, message = "File không tồn tại" });

                // Kiểm tra quyền sở hữu
                if (file.Project.StudentID != GetCurrentUserID())
                    return Json(new { success = false, message = "Bạn không có quyền xóa file này" });

                // Kiểm tra trạng thái project
                var status = file.Project.Status?.Trim() ?? "";
                if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Không thể xóa file của đề tài đã hoàn thành" });

                // Xóa file vật lý trên server
                if (!string.IsNullOrWhiteSpace(file.FilePath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath,
                        file.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Xóa record trong database
                _context.ProjectFiles.Remove(file);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa file thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Cập nhật (thay thế) file
        [HttpPost]
        public async Task<IActionResult> UpdateFile(int fileId, IFormFile newFile)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var file = await _context.ProjectFiles
                    .Include(f => f.Project)
                    .FirstOrDefaultAsync(f => f.FileID == fileId);

                if (file == null)
                    return Json(new { success = false, message = "File không tồn tại" });

                // Kiểm tra quyền sở hữu
                if (file.Project.StudentID != GetCurrentUserID())
                    return Json(new { success = false, message = "Bạn không có quyền cập nhật file này" });

                // Kiểm tra trạng thái project
                var status = file.Project.Status?.Trim() ?? "";
                if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Không thể cập nhật file của đề tài đã hoàn thành" });

                if (newFile == null || newFile.Length == 0)
                    return Json(new { success = false, message = "Vui lòng chọn file mới" });

                // Check file size
                var maxSize = await _context.SystemConfigs
                    .FirstOrDefaultAsync(c => c.ConfigKey == "MaxFileSize");
                var maxFileSize = maxSize != null ? long.Parse(maxSize.ConfigValue) : 10485760;

                if (newFile.Length > maxFileSize)
                    return Json(new { success = false, message = "File quá lớn! Tối đa 10MB" });

                // Xóa file cũ trên server
                if (!string.IsNullOrWhiteSpace(file.FilePath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath,
                        file.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Lưu file mới
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "projects");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{newFile.FileName}";
                var newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await newFile.CopyToAsync(stream);
                }

                // Cập nhật thông tin file trong database
                file.FileName = newFile.FileName;
                file.FilePath = $"/uploads/projects/{uniqueFileName}";
                file.FileSize = newFile.Length;
                file.UploadDate = DateTime.Now;

                _context.ProjectFiles.Update(file);
                await _context.SaveChangesAsync();

                // Thông báo cho giảng viên
                if (file.Project.ProfessorID.HasValue)
                {
                    await CreateNotification(file.Project.ProfessorID.Value, "Cập nhật tài liệu",
                        $"Sinh viên đã cập nhật tài liệu cho đề tài: {file.Project.Title}", "Info",
                        $"/Professor/ProjectDetail/{file.Project.ProjectID}");
                }

                return Json(new
                {
                    success = true,
                    message = "Đã cập nhật file thành công!",
                    newFileName = file.FileName,
                    newFileSize = (file.FileSize / 1024.0).ToString("F2")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Messages
        public async Task<IActionResult> Messages() {
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

        [HttpPost]
        public async Task<IActionResult> SendMessage(int receiverId, string content) {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            try {
                var message = new Message {
                    SenderID = GetCurrentUserID(),
                    ReceiverID = receiverId,
                    Content = content,
                    SentDate = DateTime.Now,
                    IsRead = false
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                // Create notification
                await CreateNotification(receiverId, "Tin nhắn mới",
                    "Bạn có tin nhắn mới", "Info", "/Student/Messages");
                return Json(new { success = true, message = "Gửi tin nhắn thành công!" });
            }
            catch (Exception ex) {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Notifications
        public async Task<IActionResult> Notifications() {
            if (!CheckAuth()) return RedirectToAction("Login", "Home");
            var notifications = await _context.Notifications
                .Where(n => n.UserID == GetCurrentUserID())
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id) {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && notification.UserID == GetCurrentUserID()) {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // Profile
        public async Task<IActionResult> Profile() {
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
                    var webRoot = _environment.WebRootPath;
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

                TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        // Hoạt động xóa dự án:
        [HttpPost]
        public async Task<IActionResult> DeleteProject(int projectId) {
            // Kiểm tra quyền
            if (!CheckAuth())
                return Json(new { success = false, message = "Unauthorized" });
            int studentId = GetCurrentUserID();
            Console.WriteLine($"Current UserID={studentId}, Trying to delete ProjectID={projectId}");
            // Lấy project kèm files và progress
            var project = await _context.Projects
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectProgresses)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && p.StudentID == studentId);
            if (project == null) {
                Console.WriteLine($"ProjectID={projectId} not found or not owned by UserID={studentId}");
                return Json(new { success = false, message = "Đề tài không tồn tại hoặc bạn không có quyền xóa." });
            }
            // Chuẩn hóa status trước khi check
            var status = project.Status?.Trim() ?? "";
            Console.WriteLine($"ProjectID={projectId} Status='{project.Status}' TrimmedStatus='{status}'");
            if (!status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                !status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) {
                return Json(new { success = false, message = "Chỉ có thể xóa đề tài đang chờ duyệt hoặc bị từ chối." });
            }
            try {
                // Xóa file trên server
                if (project.ProjectFiles != null) {
                    foreach (var file in project.ProjectFiles) {
                        if (string.IsNullOrWhiteSpace(file.FilePath))
                            continue;
                        var filePath = Path.Combine(_environment.WebRootPath,
                            file.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (System.IO.File.Exists(filePath)) {
                            System.IO.File.Delete(filePath);
                            Console.WriteLine($"Deleted file: {filePath}");
                        } else {
                            Console.WriteLine($"File not found: {filePath}");
                        }
                    }
                }
                // Xóa các bản ghi liên quan trong DB
                if (project.ProjectFiles != null && project.ProjectFiles.Any())
                    _context.ProjectFiles.RemoveRange(project.ProjectFiles);
                if (project.ProjectProgresses != null && project.ProjectProgresses.Any())
                    _context.ProjectProgresses.RemoveRange(project.ProjectProgresses);
                // Xóa project
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                Console.WriteLine($"ProjectID={projectId} deleted successfully.");
                return Json(new { success = true, message = "Xóa đề tài thành công!" });
            } catch (Exception ex) {
                Console.WriteLine($"Error deleting ProjectID={projectId}: {ex}");
                return Json(new { success = false, message = "Xóa thất bại: " + ex.Message });
            }
        }

        // Helper method
        private async Task CreateNotification(int userID, string title, string message, string type, string link = "") {
            var notification = new Notification {
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