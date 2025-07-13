using CvAnalysis.Server.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CvAnalysis.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly ChatClient _chatClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TextAnalysisService> _logger;

        public TextAnalysisService(IConfiguration configuration, ILogger<TextAnalysisService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            try
            {
                var endpoint = configuration["AzureAi:OpenAiEndpoint"];
                var key = configuration["AzureAi:OpenAiKey"];
                var deployment = configuration["AzureAi:OpenAiDeployment"];

                _logger.LogInformation($"OpenAI servisi başlatılıyor. Endpoint: {endpoint?.Substring(0, Math.Min(50, endpoint?.Length ?? 0))}...");

                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new InvalidOperationException("AzureAi:OpenAiEndpoint appsettings.json'da tanımlı değil!");
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidOperationException("AzureAi:OpenAiKey appsettings.json'da tanımlı değil!");
                if (string.IsNullOrWhiteSpace(deployment))
                    throw new InvalidOperationException("AzureAi:OpenAiDeployment appsettings.json'da tanımlı değil!");

                var clientOptions = new OpenAIClientOptions 
                { 
                    Endpoint = new Uri(endpoint) 
                };

                _chatClient = new ChatClient(deployment, new ApiKeyCredential(key), clientOptions);
                
                _logger.LogInformation("OpenAI ChatClient başarıyla başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI ChatClient başlatılırken hata oluştu");
                throw;
            }
        }

        public async Task<AnalysisReport> AnalyzeTextAsync(string cvText, string jobDescription, string lang = "tr")
        {
            var report = new AnalysisReport { ExtractedCvText = cvText };
            
            try
            {
                _logger.LogInformation($"Metin analizi başlatılıyor. CV uzunluğu: {cvText.Length}, Dil: {lang}");

                if (string.IsNullOrWhiteSpace(cvText))
                {
                    _logger.LogWarning("CV metni boş");
                    report.Suggestions.Add(lang == "en" ? "No text could be read from the CV or the CV is empty." : "CV'den metin okunamadı veya CV boş.");
                    return report;
                }

                // Environment variable veya appsettings'ten endpoint al
                var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? _configuration["AzureAi:OpenAiEndpoint"];
                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new InvalidOperationException("Azure OpenAI endpoint environment variable veya appsettings'te tanımlı değil!");

                // Entra ID authentication (DefaultAzureCredential)
                var credential = new DefaultAzureCredential();
                var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                var deploymentName = _configuration["AzureAi:OpenAiDeployment"] ?? "model-router";
                var chatClient = azureClient.GetChatClient(deploymentName);

                string prompt = lang == "en"
                    ? @"Below is a resume (CV) text and a job description.
1- Score the CV's ATS compatibility between 0-100 and return only the numeric score.
2- Then, generate 5 creative, personalized, and non-generic suggestions in English to improve the CV's ATS compatibility and overall quality. Do NOT repeat generic tips like 'Add keywords', 'Use a simple format', 'Standardize section titles', 'Save as PDF or DOCX', 'Be concise', 'Pay attention to spelling', 'Include your contact information', 'Avoid unnecessary personal information', 'Specify dates and positions', or 'List education and certificates in order'. Focus on unique, actionable, and CV-specific advice based on the actual content.
Respond in this format:
Score: <number>
Suggestions:
- ...
- ..."
                    : @"Aşağıda bir özgeçmiş (CV) ve iş ilanı metni verilmiştir.
1- CV'nin ATS uyumluluğunu 0-100 arasında puanla ve sadece sayısal puanı döndür.
2- Ayrıca, CV'nin ATS uyumluluğunu ve genel kalitesini artırmak için yaratıcı, kişiye özel ve sabit/generik olmayan 5 öneri üret. 'Anahtar kelime ekleyin', 'Sade format kullanın', 'Başlıkları standartlaştırın', 'PDF veya DOCX formatında kaydedin', 'Kısa ve öz yazın', 'İmla ve dil bilgisine dikkat edin', 'İletişim bilgilerinizi eksiksiz yazın', 'Gereksiz kişisel bilgilerden kaçının', 'Her iş deneyimi için tarih ve pozisyon belirtin', 'Eğitim ve sertifikaları kronolojik sırayla yazın' gibi klasik tavsiyeleri tekrar etme. Daha özgün ve CV'ye özel, uygulanabilir öneriler ver, gerçek içerikten yola çık.
Yanıtı şu formatta ver:
Puan: <sayı>
Öneriler:
- ...
- ...";

                prompt += $"\n\nCV metni:\n{cvText}\n\nİş ilanı metni:\n{jobDescription}";

                _logger.LogInformation("OpenAI API'sine istek gönderiliyor...");

                var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
                var options = new ChatCompletionOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 1024,
                    TopP = 0.95f,
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f
                };

                var completion = await chatClient.CompleteChatAsync(messages, options);
                var result = completion.Value.Content[0].Text;

                _logger.LogInformation($"OpenAI API'den yanıt alındı. Yanıt uzunluğu: {result.Length}");

                // Yanıtı parse et: puan ve öneriler
                int puan = 0;
                var oneriler = new List<string>();
                var lines = result.Split('\n');
                
                foreach (var line in lines)
                {
                    if (lang == "en" && line.Trim().StartsWith("Score:"))
                    {
                        int.TryParse(line.Replace("Score:", "").Trim(), out puan);
                    }
                    else if (lang != "en" && line.Trim().StartsWith("Puan:"))
                    {
                        int.TryParse(line.Replace("Puan:", "").Trim(), out puan);
                    }
                    else if (line.Trim().StartsWith("-"))
                    {
                        oneriler.Add(line.Trim().TrimStart('-').Trim());
                    }
                }

                report.AtsScore = puan;
                report.Suggestions = oneriler;

                if (puan >= 70)
                {
                    report.PositiveFeedback.Add(lang == "en"
                        ? "Your CV's ATS compatibility is high!"
                        : "CV'nizin ATS uyumluluğu yüksek!");
                }
                else
                {
                    report.Suggestions.Add(lang == "en"
                        ? "Consider improving your CV based on the suggestions above."
                        : "Yukarıdaki önerilere göre CV'nizi geliştirmeyi düşünün.");
                }

                // Farklı içerik ve dil için:
                report.AtsImprovementTips = GetAtsImprovementTips(lang);
                report.ExtraAdvice = report.Suggestions.ToList();

                // Anahtar kelime analizi: iş ilanı ve CV metni boş değilse çalıştır
                if (!string.IsNullOrWhiteSpace(jobDescription) && !string.IsNullOrWhiteSpace(cvText))
                {
                    // OpenAI ile iş ilanı anahtar kelimeleri çıkarımı
                    string jobKeywordPrompt = $@"
Aşağıdaki iş ilanı metninden, pozisyon için en önemli 10 anahtar kelimeyi veya anahtar kelime öbeklerini (tercihen 1-2 kelimelik) çıkar. 
Sadece anahtar kelimeleri virgül ile ayırarak sırala. Açıklama veya başka bir şey ekleme.

İş ilanı metni:
{jobDescription}
";
                    var jobKeywordMessages = new List<ChatMessage> { new UserChatMessage(jobKeywordPrompt) };
                    var keywordOptions = new ChatCompletionOptions
                    {
                        Temperature = 0.2f,
                        MaxOutputTokenCount = 128,
                        TopP = 0.95f
                    };
                    var jobKeywordCompletion = await chatClient.CompleteChatAsync(jobKeywordMessages, keywordOptions);
                    var jobKeywordResult = jobKeywordCompletion.Value.Content[0].Text;
                    var jobKeywords = jobKeywordResult.Split(',')
                        .Select(k => k.Trim().ToLowerInvariant())
                        .Where(k => k.Length > 2)
                        .Distinct()
                        .ToList();

                    // OpenAI ile CV anahtar kelimeleri çıkarımı
                    string cvKeywordPrompt = $@"
Aşağıdaki CV metninden, pozisyon için en önemli 10 anahtar kelimeyi veya anahtar kelime öbeklerini (tercihen 1-2 kelimelik) çıkar. 
Sadece anahtar kelimeleri virgül ile ayırarak sırala. Açıklama veya başka bir şey ekleme.

CV metni:
{cvText}
";
                    var cvKeywordMessages = new List<ChatMessage> { new UserChatMessage(cvKeywordPrompt) };
                    var cvKeywordCompletion = await chatClient.CompleteChatAsync(cvKeywordMessages, keywordOptions);
                    var cvKeywordResult = cvKeywordCompletion.Value.Content[0].Text;
                    var cvKeywords = cvKeywordResult.Split(',')
                        .Select(k => k.Trim().ToLowerInvariant())
                        .Where(k => k.Length > 2)
                        .Distinct()
                        .ToList();

                    var foundKeywords = jobKeywords
                        .Where(jk => cvKeywords.Any(ck => ck.Contains(jk, StringComparison.OrdinalIgnoreCase) || jk.Contains(ck, StringComparison.OrdinalIgnoreCase)))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var missingKeywords = jobKeywords
                        .Where(jk => !foundKeywords.Contains(jk, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    report.FoundKeywords = foundKeywords;
                    report.MissingKeywords = missingKeywords;
                }

                _logger.LogInformation($"Analiz tamamlandı. Puan: {puan}, Öneri sayısı: {oneriler.Count}");
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metin analizi sırasında hata oluştu");
                
                // Hata durumunda varsayılan yanıt döndür
                report.AtsScore = 0;
                report.Suggestions.Add(lang == "en" 
                    ? $"Analysis failed due to technical error: {ex.Message}"
                    : $"Teknik hata nedeniyle analiz başarısız oldu: {ex.Message}");
                
                return report;
            }
        }

        private List<string> GetAtsImprovementTips(string lang)
        {
            return lang == "en"
                ? new List<string>
                {
                    "Add keywords from the job description to your CV.",
                    "Use a simple and clean format, avoid tables and shapes.",
                    "Standardize section titles (Education, Experience, Skills, etc.).",
                    "Save as PDF or DOCX format.",
                    "Be concise, avoid unnecessarily long sentences.",
                    "Pay attention to spelling and grammar.",
                    "Include your contact information completely.",
                    "Avoid unnecessary personal information (ID, marital status, religion, etc.).",
                    "Specify dates and positions for each job experience.",
                    "List education and certificates in chronological order."
                }
                : new List<string>
                {
                    "İş ilanındaki anahtar kelimeleri CV'nize ekleyin.",
                    "Sade ve düz bir format kullanın, tablo ve şekillerden kaçının.",
                    "Başlıkları standartlaştırın (Eğitim, Deneyim, Yetenekler vb.).",
                    "PDF veya DOCX formatında kaydedin.",
                    "Kısa ve öz yazın, gereksiz uzun cümlelerden kaçının.",
                    "İmla ve dil bilgisine dikkat edin.",
                    "İletişim bilgilerinizi eksiksiz yazın.",
                    "Gereksiz kişisel bilgilerden kaçının (TC kimlik, medeni durum, din vb.).",
                    "Her iş deneyimi için tarih ve pozisyon belirtin.",
                    "Eğitim ve sertifikaları kronolojik sırayla yazın."
                };
        }

        public AnalysisReport AnalyzeText(string cvText, string jobDescription)
        {
            throw new NotImplementedException("Lütfen AnalyzeTextAsync fonksiyonunu kullanın.");
        }

        // Basit anahtar kelime çıkarıcı (kelime köklerine bakmaz, sadece kelime bazlı)
        private List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            // Noktalama işaretlerini kaldır, küçük harfe çevir, kelimelere ayır
            var words = Regex.Matches(text.ToLowerInvariant(), @"[\p{L}0-9_]+")
                .Select(m => m.Value)
                .Where(w => w.Length > 2) // çok kısa kelimeleri alma
                .Distinct()
                .ToList();
            return words;
        }
    }
}