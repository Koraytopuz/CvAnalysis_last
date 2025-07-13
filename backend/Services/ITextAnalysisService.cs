using CvAnalysis.Server.Models;
using System.Threading.Tasks;

namespace CvAnalysis.Server.Services
{
    public interface ITextAnalysisService
    {
        [System.Obsolete("Lütfen AnalyzeTextAsync fonksiyonunu kullanın.")]
        AnalysisReport AnalyzeText(string cvText, string jobDescription);
        Task<AnalysisReport> AnalyzeTextAsync(string cvText, string jobDescription, string lang = "tr");
    }
}