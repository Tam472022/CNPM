using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Duan_CNPM.Data;
using Duan_CNPM.Models;
using System.Diagnostics;

namespace Duan_CNPM.Controllers {
    public class HomeController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger) {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index() {
            var userID = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userID)) {
                var role = HttpContext.Session.GetString("Role");
                return role switch {
                    "Admin" => RedirectToAction("Index", "Admin"),
                    "Professor" => RedirectToAction("Dashboard", "Professor"),
                    "Student" => RedirectToAction("Dashboard", "Student"),
                    _ => View()
                };
            }
            return View();
        }

        // ---------------- Login ----------------
        [HttpGet]
        public async Task<IActionResult> Login() {
            try {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserID")))
                    return RedirectToAction("Index"); 
                var lastUsername = HttpContext.Session.GetString("LastTriedUsername");
                if (!string.IsNullOrEmpty(lastUsername)) {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == lastUsername);
                    if (user != null && user.FailedLoginAttempts > 0) {
                        user.FailedLoginAttempts = 0;
                        try { await _context.SaveChangesAsync(); }
                        catch (Exception ex) { _logger.LogError(ex, "Failed to reset FailedLoginAttempts"); }
                    }
                } 
                HttpContext.Session.Remove("LastTriedUsername");
                return View();
            } catch (Exception e) {
                _logger.LogError(e, "GET Login error");
                ViewBag.Message = "Có lỗi xảy ra, vui lòng thử lại!";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password) {
            HttpContext.Session.SetString("LastTriedUsername", username);
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
                ViewBag.Message = "Tên đăng nhập và mật khẩu không được để trống!";
                return View();
            }
            try {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username); 
                if (user == null) {
                    ViewBag.Message = "Tên đăng nhập không tồn tại!";
                    return View();
                } 
                if (!user.IsActive) {
                    ViewBag.Message = "Tài khoản đã bị vô hiệu hóa!";
                    return View();
                }
                // Thời gian khỏa đã hết thì reset:
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.Now) {
                    // Chỉ reset nếu thực sự có thay đổi
                    if (user.FailedLoginAttempts != 0 || user.LockoutEnd != null) {
                        user.FailedLoginAttempts = 0;
                        user.LockoutEnd = null;
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now) {
                    ViewBag.Message = $"Tài khoản bị khóa đến {user.LockoutEnd.Value:dd/MM/yyyy HH:mm}";
                    return View();
                }
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 3) {
                        user.LockoutEnd = DateTime.Now.AddMinutes(5);
                        await _context.SaveChangesAsync();
                        await CreateAuditLog(user.UserID, "Login Failed - Account Locked", "Users", user.UserID);
                        return RedirectToAction("LoginFailed");
                    } 
                    await _context.SaveChangesAsync();
                    ViewBag.Message = $"Mật khẩu sai! Còn {3 - user.FailedLoginAttempts} lần thử.";
                    return View();
                }
                // Login thành công
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync(); 
                HttpContext.Session.SetString("UserID", user.UserID.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role); 
                await CreateAuditLog(user.UserID, "Login Success", "Users", user.UserID);
                await CreateNotification(user.UserID, "Đăng nhập thành công", $"Chào mừng {user.FullName} quay trở lại!", "Success"); 
                return RedirectToAction("Index");
            } catch (Exception ex) {
                _logger.LogError(ex, "Login error");
                ViewBag.Message = "Có lỗi xảy ra, vui lòng thử lại!";
                return View();
            }
        }

        public IActionResult LoginFailed() => View();

        // ---------------- Register ----------------
        [HttpGet]
        public IActionResult Register() {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserID")))
                return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult Register (
            string Username, string FullName, string Password, string ConfirmPassword, string Role,
            string StudentCode, string StudentMajor, string StudentPhone, string StudentEmail,
            string ProfessorMajor, string ProfessorPhone, string ProfessorEmail
        ) {
            try {
                // Không được để trống:
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)) {
                    ViewBag.Message = "Tên đăng nhập và mật khẩu không được để trống!";
                    return View();
                }
                if (Password != ConfirmPassword) {
                    ViewBag.Message = "Mật khẩu xác nhận không khớp!";
                    return View();
                }
                if (string.IsNullOrEmpty(Role)) {
                    ViewBag.Message = "Vui lòng chọn vai trò!";
                    return View();
                }
                // Không được đăng ký trùng tên với bất cứ ai (username phải khác biệt)
                if (_context.Users.Any(u => u.Username == Username)) {
                    ViewBag.Message = "Tên đăng nhập đã tồn tại!";
                    return View();
                }
                // Khởi tạo người dùng:
                var user = new User {
                    // Non-nullable:
                    Username = Username.Trim(),
                    FullName = FullName.Trim(),
                    Role = Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                    IsActive = true,
                    FailedLoginAttempts = 0
                };
                // Vai trò là student hay Professor:
                if (Role == "Student") {
                    user.StudentCode = StudentCode?.Trim();
                    user.Major = StudentMajor?.Trim();
                    user.Phone = StudentPhone?.Trim();
                    user.Email = StudentEmail?.Trim();
                } else if (Role == "Professor") {
                    user.Major = ProfessorMajor?.Trim();
                    user.Phone = ProfessorPhone?.Trim();
                    user.Email = ProfessorEmail?.Trim();
                }
                _context.Users.Add(user);
                _context.SaveChanges();
                ViewBag.Message = "Đăng ký thành công! Vui lòng đăng nhập.";
                return View();
            } catch (Exception ex) {
                _logger.LogError(ex, "Register error");
                ViewBag.Message = "Có lỗi xảy ra, vui lòng thử lại!";
                return View();
            }
        }

        // ---------------- Logout ----------------
        [HttpPost]
        public async Task<IActionResult> Logout() {
            var userID = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userID))
                await CreateAuditLog(int.Parse(userID), "Logout", "Users", int.Parse(userID));
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        } 
        public IActionResult Privacy() => View(); 
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); 

        // ---------------- Helper Methods ----------------
        private async Task CreateAuditLog(int? userID, string action, string tableName, int? recordID) {
            var log = new AuditLog {
                UserID = userID,
                Action = action,
                TableName = tableName ?? "",
                RecordID = recordID,
                OldValue = "",
                NewValue = "",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                CreatedDate = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        } 
        private async Task CreateNotification(int userID, string title, string message, string type) {
            var notification = new Notification {
                UserID = userID,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedDate = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}