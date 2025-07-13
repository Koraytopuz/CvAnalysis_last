using CvAnalysis.Server.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CvAnalysis.Server.Services
{
    // NOT: Şu anda kullanılmıyor. Azure OpenAI servisine geçildi.
    public class GeminiTextAnalysisService : ITextAnalysisService
    {
        private readonly string? _apiKey;
        private readonly HttpClient _httpClient;
        private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=";

        public GeminiTextAnalysisService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey ayarı bulunamadı.");
            _httpClient = new HttpClient();
        }

        public async Task<AnalysisReport> AnalyzeTextAsync(string cvText, string jobDescription, string lang = "tr")
        {
            var report = new AnalysisReport { ExtractedCvText = cvText };
            if (string.IsNullOrWhiteSpace(cvText))
            {
                report.Suggestions.Add(lang == "en" ? "No text could be read from the CV or the CV is empty." : "CV'den metin okunamadı veya CV boş.");
                return report;
            }

            List<string> targetKeywords = string.IsNullOrWhiteSpace(jobDescription)
                ? new List<string> { "C#", ".NET", "React", "SQL", "Azure" }
                : ExtractKeywordsFromJobDescription(jobDescription);

            var foundKeywords = new List<string>();
            var lowerCvText = cvText.ToLowerInvariant();

            foreach (var keyword in targetKeywords)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(lowerCvText, $@"\b{System.Text.RegularExpressions.Regex.Escape(keyword.ToLowerInvariant())}\b"))
                {
                    foundKeywords.Add(keyword);
                }
            }

            report.FoundKeywords = foundKeywords;
            report.MissingKeywords = targetKeywords.Except(foundKeywords).ToList();

            if (targetKeywords.Any())
            {
                report.AtsScore = Math.Round((double)foundKeywords.Count / targetKeywords.Count * 100);
            }

            if (report.AtsScore < 50)
            {
                if(report.MissingKeywords.Any())
                    report.Suggestions.Add(lang == "en"
                        ? $"Consider adding these keywords to improve your ATS compatibility: {string.Join(", ", report.MissingKeywords)}"
                        : $"ATS uyumluluğunuzu artırmak için şu anahtar kelimeleri eklemeyi düşünün: {string.Join(", ", report.MissingKeywords)}");
            }
            else
            {
                report.PositiveFeedback.Add(lang == "en"
                    ? "You have successfully included important keywords from the job description in your CV!"
                    : "İş ilanıyla ilgili önemli anahtar kelimeleri başarıyla CV'nize eklemişsiniz!");
            }

            // Dinamik öneri için Gemini prompt
            string prompt = lang == "en"
                ? $@"Below is a resume (CV) text and a job description. Generate 5 personalized, bullet-pointed suggestions in English to improve the CV's ATS compatibility and overall quality. Give advice on missing keywords, format, content, language, detail, development, and general job application success. Only output the suggestions as a list, no explanations.\n\nCV text:\n{cvText}\n\nJob description:\n{jobDescription}\n"
                : $@"Aşağıda bir özgeçmiş (CV) metni ve iş ilanı metni verilmiştir. CV'nin ATS uyumluluğunu ve genel kalitesini artırmak için kişiye özel, Türkçe ve maddeler halinde 5 öneri/tavsiye üret. Eksik anahtar kelimeleri, format, içerik, dil, detay, gelişim ve genel iş başvurusu başarısı açısından öneriler ver. Sadece öneri maddelerini üret, açıklama ekleme.\n\nCV metni:\n{cvText}\n\nİş ilanı metni:\n{jobDescription}\n";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(GeminiEndpoint + _apiKey, content);
            var responseString = await response.Content.ReadAsStringAsync();

            // DEBUG: Yanıtı dosyaya kaydet (hem köke hem backend/ klasörüne)
            try
            {
                System.IO.File.WriteAllText("gemini_api_response.json", responseString);
                System.IO.File.WriteAllText("backend/gemini_api_response.json", responseString);
            }
            catch {}

            // Yanıtı işle
            var suggestions = new List<string>();
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(responseString))
            {
                suggestions.Add($"Gemini API HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                suggestions.Add($"Yanıt: {responseString}");
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var candidates = doc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() > 0)
                    {
                        var parts = candidates[0].GetProperty("content").GetProperty("parts");
                        foreach (var part in parts.EnumerateArray())
                        {
                            var text = part.GetProperty("text").GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                suggestions.AddRange(text.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    suggestions.Add(lang == "en" ? $"No suggestions could be generated by Gemini API. Error: {ex.Message}" : $"Gemini API'den öneri alınamadı. Hata: {ex.Message}");
                    suggestions.Add($"Yanıt: {responseString}");
                }
            }
            report.ExtraAdvice = suggestions;
            report.Suggestions.AddRange(suggestions);

            report.AtsImprovementTips = lang == "en"
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

            return report;
        }

        public AnalysisReport AnalyzeText(string cvText, string jobDescription)
        {
            throw new NotImplementedException("Lütfen AnalyzeTextAsync fonksiyonunu kullanın.");
        }

        private List<string> ExtractKeywordsFromJobDescription(string jobDescription)
        {
            var potentialSkills = new List<string>
            {
                "C#", "ASP.NET", ".NET Core", ".NET", "Python", "Java", "Go", "Ruby", "PHP",
                "SQL", "NoSQL", "PostgreSQL", "MySQL", "MongoDB", "Redis", "Elasticsearch",
                "Azure", "AWS", "Google Cloud", "GCP", "Docker", "Kubernetes", "Terraform", "CI/CD",
                "React", "Angular", "Vue", "JavaScript", "TypeScript", "HTML", "CSS", "Node.js", "jQuery",
                "Microservices", "API", "REST", "Agile", "Scrum", "Git", "Jira", "TDD",
                "Machine Learning", "Data Science", "AI", "Artificial Intelligence", "Deep Learning", "NLP"
            };

            var foundSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var skill in potentialSkills)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(jobDescription, $@"\b{System.Text.RegularExpressions.Regex.Escape(skill)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    foundSkills.Add(skill);
                }
            }
            return foundSkills.Any() ? foundSkills.ToList() : new List<string> { "C#", ".NET", "React", "SQL", "Azure" };
        }
    }
} 