namespace CvAnalysis.Server.Models
{
    public class User {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }
}