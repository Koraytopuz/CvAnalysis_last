using CvAnalysis.Server.Services;
using Microsoft.AspNetCore.Mvc;
using CvAnalysis.Server.Models;

namespace CvAnalysis.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CvAnalysisController : ControllerBase
    {
        private readonly ICvAnalysisService _cvAnalysisService;
        private readonly ITextAnalysisService _textAnalysisService;
        private readonly ILogger<CvAnalysisController> _logger;

        public CvAnalysisController(
            ICvAnalysisService cvAnalysisService, 
            ITextAnalysisService textAnalysisService,
            ILogger<CvAnalysisController> logger)
        {
            _cvAnalysisService = cvAnalysisService;
            _textAnalysisService = textAnalysisService;
            _logger = logger;
        }

        [Consumes("multipart/form-data")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadCv([FromForm] CvUploadRequest request, [FromForm] string lang = "tr")
        {
            try
            {
                _logger.LogInformation("CV upload isteği alındı");
                
                var file = request.File;
                var jobDescription = request.JobDescription;
                
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Dosya seçilmedi veya boş");
                    return BadRequest(new { error = "Lütfen bir dosya seçin." });
                }

                // Dosya türü kontrolü
                var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                if (!allowedTypes.Contains(file.ContentType))
                {
                    _logger.LogWarning($"Desteklenmeyen dosya türü: {file.ContentType}");
                    return BadRequest(new { error = "Sadece PDF ve DOCX dosyaları desteklenmektedir." });
                }

                _logger.LogInformation($"Dosya işleniyor: {file.FileName}, Boyut: {file.Length} bytes");

                // CV metni çıkarma
                string extractedText;
                try
                {
                    extractedText = await _cvAnalysisService.AnalyzeCvAsync(file);
                    _logger.LogInformation($"CV metni çıkarıldı, uzunluk: {extractedText.Length} karakter");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CV metni çıkarılırken hata oluştu");
                    return StatusCode(500, new { error = "CV metni çıkarılırken hata oluştu", details = ex.Message });
                }

                // Metin analizi
                AnalysisReport analysisReport;
                try
                {
                    analysisReport = await _textAnalysisService.AnalyzeTextAsync(extractedText, jobDescription ?? "", lang);
                    _logger.LogInformation("Metin analizi tamamlandı");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Metin analizi sırasında hata oluştu");
                    return StatusCode(500, new { error = "Metin analizi sırasında hata oluştu", details = ex.Message });
                }

                return Ok(analysisReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CV analiz edilirken genel hata oluştu");
                return StatusCode(500, new { 
                    error = "CV analiz edilirken bir sunucu hatası oluştu.", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        // Test endpoint'i
        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Test endpoint çağrıldı");
            return Ok(new { message = "CV Analysis API çalışıyor!", timestamp = DateTime.Now });
        }

        // Konfigürasyon testi
        [HttpGet("test-config")]
        public IActionResult TestConfig()
        {
            try
            {
                var config = HttpContext.RequestServices.GetService<IConfiguration>();
                
                var azureEndpoint = config?["AzureAi:DocumentIntelligenceEndpoint"];
                var azureKey = config?["AzureAi:DocumentIntelligenceKey"];
                var openAiEndpoint = config?["AzureAi:OpenAiEndpoint"];
                var openAiKey = config?["AzureAi:OpenAiKey"];
                var openAiDeployment = config?["AzureAi:OpenAiDeployment"];

                return Ok(new
                {
                    DocumentIntelligenceEndpoint = string.IsNullOrEmpty(azureEndpoint) ? "EKSIK" : "MEVCUT",
                    DocumentIntelligenceKey = string.IsNullOrEmpty(azureKey) ? "EKSIK" : "MEVCUT",
                    OpenAiEndpoint = string.IsNullOrEmpty(openAiEndpoint) ? "EKSIK" : "MEVCUT",
                    OpenAiKey = string.IsNullOrEmpty(openAiKey) ? "EKSIK" : "MEVCUT",
                    OpenAiDeployment = string.IsNullOrEmpty(openAiDeployment) ? "EKSIK" : "MEVCUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Konfigürasyon testi sırasında hata");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // CORS preflight için OPTIONS endpoint
        [HttpOptions("upload")]
        public IActionResult PreflightUpload()
        {
            return Ok();
        }
    }
}