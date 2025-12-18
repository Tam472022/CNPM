using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Duan_CNPM.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string fullName, string verificationLink);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string fullName, string verificationLink)
        {
            var subject = "Xác thực tài khoản - Hệ thống Quản lý Đồ án";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #0d1b3d 0%, #1a3a6d 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #0d1b3d 0%, #1a3a6d 100%); color: white !important; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Xác thực tài khoản</h1>
                        </div>
                        <div class='content'>
                            <p>Xin chào <strong>{fullName}</strong>,</p>
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại Hệ thống Quản lý Đồ án.</p>
                            <p>Vui lòng nhấn vào nút bên dưới để xác thực địa chỉ email của bạn:</p>
                            <p style='text-align: center;'>
                                <a href='{verificationLink}' class='button'>Xác thực Email</a>
                            </p>
                            <p>Hoặc copy link sau vào trình duyệt:</p>
                            <p style='word-break: break-all; background: #fff; padding: 10px; border-radius: 5px;'>{verificationLink}</p>
                            <p><strong>Lưu ý:</strong> Link xác thực có hiệu lực trong 24 giờ.</p>
                            <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Hệ thống Quản lý Đồ án. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
        {
            var subject = "Đặt lại mật khẩu - Hệ thống Quản lý Đồ án";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white !important; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Đặt lại mật khẩu</h1>
                        </div>
                        <div class='content'>
                            <p>Xin chào <strong>{fullName}</strong>,</p>
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                            <p>Vui lòng nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
                            <p style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Đặt lại mật khẩu</a>
                            </p>
                            <p><strong>Lưu ý:</strong> Link có hiệu lực trong 1 giờ.</p>
                            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Hệ thống Quản lý Đồ án. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {toEmail}: {ex.Message}");
                throw;
            }
        }
    }
}