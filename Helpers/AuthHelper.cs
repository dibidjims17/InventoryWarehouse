using BCrypt.Net;

namespace MyRazorApp.Helpers
{
    public static class AuthHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        // Token generator stays the same
        public static string GenerateToken(string userId)
        {
            string random = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return $"{random}.{userId}";
        }
    }
}
