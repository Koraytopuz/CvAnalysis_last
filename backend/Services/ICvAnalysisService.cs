namespace CvAnalysis.Server.Services {
    public interface ICvAnalysisService {
        Task<string> AnalyzeCvAsync(IFormFile cvFile);
    }
}