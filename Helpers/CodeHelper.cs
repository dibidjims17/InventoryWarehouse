using System;

namespace MyRazorApp.Helpers
{
    public static class CodeHelper
    {
        // Generate a 6-digit numeric verification code as string
        public static string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
