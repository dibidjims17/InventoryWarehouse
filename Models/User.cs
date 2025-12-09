using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyRazorApp.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;


        [BsonElement("role")]
        public string Role { get; set; } = "client";

        [BsonElement("status")]
        public string Status { get; set; } = "Inactive";

        [BsonElement("verified")]
        public bool Verified { get; set; } = false;

        [BsonElement("verification_code")]
        public int VerificationCode { get; set; } = 0;

        [BsonElement("last_login")]
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;

        [BsonElement("session_token")]
        public string? SessionToken { get; set; }

        [BsonElement("profile_picture")]
        public string? ProfilePicture { get; set; } = null;

        [BsonIgnore]
        public string ProfilePictureUrl => string.IsNullOrEmpty(ProfilePicture) ? "/images/default-avatar.png" : $"/uploads/{ProfilePicture}";

        [BsonElement("isEmailVerified")]
        public bool IsEmailVerified { get; set; } = false;

        [BsonElement("emailVerificationCode")]
        public string? EmailVerificationCode { get; set; }

        [BsonElement("emailVerificationExpiry")]
        public DateTime? EmailVerificationExpiry { get; set; }

        [BsonElement("lastVerificationEmailSent")]
        public DateTime? LastVerificationEmailSent { get; set; }

        [BsonElement("passwordResetToken")]
        public string? PasswordResetToken { get; set; }

        [BsonElement("passwordResetExpiry")]
        public DateTime? PasswordResetExpiry { get; set; }

        public string OTP { get; set; } = string.Empty;
        public DateTime? OTPExpiry { get; set; }
        
    }

}
