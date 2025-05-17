using Microsoft.AspNetCore.Identity;

namespace PlannerApp.Additionally
{
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<string> hasher = new();

        public static string HashPassword(string password)
        {
            return hasher.HashPassword(null, password);
        }

        public static bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
