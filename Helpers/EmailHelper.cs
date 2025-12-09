using System.Net;
using System.Net.Mail;
using System.Web;

namespace MyRazorApp.Helpers
{
    public static class EmailHelper
    {
        // Ideally, read these from appsettings.json for security
        private static readonly string FromEmail = "longaquit.davidjames@gmail.com";
        private static readonly string Password = "ntinygxjvpihsjem";  // App Password
        private static readonly string SmtpHost = "smtp.gmail.com";
        private static readonly int SmtpPort = 587;

        public static void SendVerificationEmail(string toEmail, string code)
        {
            var subject = "Verify Your Account";
            var verifyUrl = $"https://yourdomain.com/Auth/VerifyEmail?email={HttpUtility.UrlEncode(toEmail)}&code={HttpUtility.UrlEncode(code)}";

            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #333;'>Welcome to JOYSON!</h2>
                    <p>Thank you for registering. Please verify your account by clicking the button below:</p>
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='{verifyUrl}' style='display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; font-weight: bold; border-radius: 5px;'>Verify Account</a>
                    </p>
                    <p>Or, enter this verification code manually if needed:</p>
                    <p style='font-size: 24px; font-weight: bold; color: #007bff; text-align: center;'>{code}</p>
                    <p>This code will expire in 15 minutes.</p>
                    <p>If you did not create an account, please ignore this email.</p>
                    <hr>
                    <p style='font-size: 12px; color: #888;'>MyRazorApp &copy; {DateTime.UtcNow.Year}</p>
                </div>
            </body>
            </html>";

            SendEmail(toEmail, subject, body);
        }

        public static void SendPasswordResetEmail(string toEmail, string token)
        {
            var subject = "Reset Your Password";
            var resetUrl = $"https://yourdomain.com/Auth/ResetPassword?email={HttpUtility.UrlEncode(toEmail)}&token={HttpUtility.UrlEncode(token)}";

            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #333;'>Reset Your Password</h2>
                    <p>Click the button below to reset your password:</p>
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='{resetUrl}' style='display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; font-weight: bold; border-radius: 5px;'>Reset Password</a>
                    </p>
                    <p>If you did not request a password reset, please ignore this email.</p>
                    <hr>
                    <p style='font-size: 12px; color: #888;'>MyRazorApp &copy; {DateTime.UtcNow.Year}</p>
                </div>
            </body>
            </html>";

            SendEmail(toEmail, subject, body);
        }

        private static void SendEmail(string toEmail, string subject, string body)
        {
            using var client = new SmtpClient(SmtpHost, SmtpPort)
            {
                Credentials = new NetworkCredential(FromEmail, Password),
                EnableSsl = true
            };

            var mail = new MailMessage(FromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            client.Send(mail);
        }
        public static void SendOTPEmail(string email, string otp)
        {
            string subject = "Your OTP Code";
            string body = $"Your OTP for password reset is: {otp}. It expires in 10 minutes.";

            SendEmail(email, subject, body);
        }
    }
}
