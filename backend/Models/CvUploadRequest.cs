using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CvAnalysis.Server.Models
{
    public class CvUploadRequest
    {
        [Required]
        public IFormFile? File { get; set; }
        public string? JobDescription { get; set; }
    }
} 