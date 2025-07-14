using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CvAnalysis.Server.Services
{
    public class AzureCvAnalysisService : ICvAnalysisService
    {
        private readonly string _endpoint = null!;
        private readonly string _apiKey = null!;
        private readonly ILogger<AzureCvAnalysisService> _logger;

        public AzureCvAnalysisService(IConfiguration configuration, ILogger<AzureCvAnalysisService> logger)
        {
            _logger = logger;
            _endpoint = configuration["AzureAi:DocumentIntelligenceEndpoint"] ?? throw new ArgumentNullException("AzureAi:DocumentIntelligenceEndpoint");
            _apiKey = configuration["AzureAi:DocumentIntelligenceKey"] ?? throw new ArgumentNullException("AzureAi:DocumentIntelligenceKey");
            
            _logger.LogInformation($"Azure Document Intelligence servisi başlatıldı. Endpoint: {_endpoint?.Substring(0, Math.Min(50, _endpoint.Length))}...");
        }

        public async Task<string> AnalyzeCvAsync(IFormFile cvFile)
        {
            try
            {
                _logger.LogInformation("Azure Document Intelligence analizi başlatılıyor...");
                
                var credential = new AzureKeyCredential(_apiKey);
                var client = new DocumentAnalysisClient(new Uri(_endpoint), credential);

                await using var stream = cvFile.OpenReadStream();
                
                _logger.LogInformation("Dosya Azure'a gönderiliyor...");
                
                AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(
                    WaitUntil.Completed, 
                    "prebuilt-read", 
                    stream);
                
                AnalyzeResult result = operation.Value;
                
                _logger.LogInformation($"Azure analizi tamamlandı. Sayfa sayısı: {result.Pages.Count}");

                var cvText = new StringBuilder();
                
                foreach (var page in result.Pages)
                {
                    _logger.LogInformation($"Sayfa {page.PageNumber} işleniyor, satır sayısı: {page.Lines.Count}");
                    
                    foreach (var line in page.Lines)
                    {
                        cvText.AppendLine(line.Content);
                    }
                }
                
                var extractedText = cvText.ToString();
                _logger.LogInformation($"Toplam çıkarılan metin uzunluğu: {extractedText.Length} karakter");
                
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("Azure'dan boş metin döndü");
                    throw new InvalidOperationException("CV'den metin çıkarılamadı. Dosya bozuk olabilir veya metin içermiyor olabilir.");
                }
                
                return extractedText;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"Azure Document Intelligence API hatası: {ex.Message}, Status: {ex.Status}, ErrorCode: {ex.ErrorCode}");
                
                if (ex.Status == 404)
                {
                    throw new InvalidOperationException($"Azure Document Intelligence servisi bulunamadı. Endpoint: {_endpoint}. Lütfen Azure portal'da servisin aktif olduğundan emin olun.", ex);
                }
                else if (ex.Status == 401)
                {
                    throw new InvalidOperationException("Azure Document Intelligence API anahtarı geçersiz. Lütfen API anahtarını kontrol edin.", ex);
                }
                else
                {
                    throw new InvalidOperationException($"Azure Document Intelligence API hatası: {ex.Message} (Status: {ex.Status})", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CV analizi sırasında beklenmedik hata");
                throw new InvalidOperationException($"CV analizi sırasında hata oluştu: {ex.Message}", ex);
            }
        }
    }
}