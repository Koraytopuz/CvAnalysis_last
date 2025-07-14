namespace CvAnalysis.Server.Models
{
    public class AnalysisReport {
        public double AtsScore { get; set; }
        public double ContentScore { get; set; }
        public List<string> FoundKeywords { get; set; } = new();
        public List<string> MissingKeywords { get; set; } = new();
        public List<string> PositiveFeedback { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public string ExtractedCvText { get; set; } = string.Empty;
        public List<string> AtsImprovementTips { get; set; } = new();
        public List<string> ExtraAdvice { get; set; } = new();
        public string AtsScoreDetails { get; set; } = string.Empty;
    }
}