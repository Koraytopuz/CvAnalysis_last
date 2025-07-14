using CvAnalysis.Server.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Azure.AI.OpenAI;
using Azure;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CvAnalysis.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly OpenAIClient _openAiClient;
        private readonly string _deployment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TextAnalysisService> _logger;

        public TextAnalysisService(IConfiguration configuration, ILogger<TextAnalysisService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var endpoint = configuration["AzureAi:OpenAiEndpoint"];
            var key = configuration["AzureAi:OpenAiKey"]!;
            _deployment = configuration["AzureAi:OpenAiDeployment"];
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("AzureAi:OpenAiEndpoint appsettings.json'da tanımlı değil!");
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("AzureAi:OpenAiKey appsettings.json'da tanımlı değil!");
            if (string.IsNullOrWhiteSpace(_deployment))
                throw new InvalidOperationException("AzureAi:OpenAiDeployment appsettings.json'da tanımlı değil!");
            _openAiClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _logger.LogInformation($"OpenAI servisi başlatıldı. Endpoint: {endpoint?.Substring(0, Math.Min(50, endpoint?.Length ?? 0))}...");
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
                // 1. ATS puanı ve ATS tavsiyeleri için prompt
                string atsPrompt = "Sen, bir CV ve kariyer danışmanı olarak hareket eden, işe alım süreçlerinde kullanılan Aday Takip Sistemleri (ATS) konusunda uzman bir yapay zeka asistanısın. Senin temel görevin, kullanıcı tarafından sunulan CV'yi analiz etmek, bir ATS puanı hesaplamak ve bu puanı artırmak için somut ve uygulanabilir öneriler sunmaktır. Aşağıda sana sunulan CV metnini ve iş ilanı metnini (eğer varsa) analiz et. Bu analize dayanarak CV'nin ATS uyumluluğunu değerlendir, bir ATS puanı hesapla ve kullanıcıya CV'sini nasıl daha etkili hale getirebileceği konusunda adım adım rehberlik et. Aşağıdaki kriterlere göre CV'yi değerlendir ve 100 üzerinden bir ATS puanı hesapla. Her kriter için bir puan ver ve toplam puanı gerekçelendirerek açıkla. * **Format ve Okunabilirlik (20 Puan):** * CV formatı basit ve temiz mi? (Karmaşık tablolar, sütunlar, grafikler ve resimler ATS tarafından okunamaz.) * Kullanılan yazı tipi standart mı? (Arial, Calibri, Times New Roman gibi) * Bölüm başlıkları standart ve net mi? (\"İş Deneyimi\", \"Eğitim\" gibi) * Dosya formatı .docx veya .pdf gibi yaygın bir format mı? * **Anahtar Kelime Optimizasyonu (35 Puan):** * İş ilanıyla ne kadar uyumlu? (Eğer iş ilanı varsa) * Pozisyonla ilgili genel anahtar kelimeler ve endüstri terimleri kullanılmış mı? * Beceriler bölümü, iş tanımındaki gerekliliklerle örtüşüyor mu? * **İçerik ve Yapı (30 Puan):** * Her iş deneyimi için somut başarılar ve ölçülebilir sonuçlar (rakamlar, yüzdeler) belirtilmiş mi? (Örn: \"Satışları %20 artırdı.\") * Görev tanımlarında aktif fiiller kullanılmış mı? (Örn: \"yönetildi\", \"geliştirildi\", \"oluşturuldu\") * İletişim bilgileri eksiksiz ve kolayca bulunabilir mi? * CV uzunluğu ideal mi? (Genellikle 1-2 sayfa) * **Genel Kalite ve Profesyonellik (15 Puan):** * Yazım ve dil bilgisi hataları var mı? * Tarih formatları tutarlı mı? * Genel olarak profesyonel bir izlenim bırakıyor mu? **Puanlama Sonucu:** \"**Hesaplanan ATS Puanınız: [Toplam Puan]/100** * **Format ve Okunabilirlik:** [Puan]/20 * **Anahtar Kelime Optimizasyonu:** [Puan]/35 * **İçerik ve Yapı:** [Puan]/30 * **Genel Kalite ve Profesyonellik:** [Puan]/15\" **ATS Puanını İyileştirmek İçin Tavsiyeler** Hesaplanan puana ve yapılan analize dayanarak, kullanıcıya CV'sini geliştirmesi için aşağıdaki başlıklar altında, öncelik sırasına göre somut ve eyleme geçirilebilir tavsiyeler sun: **1. Format ve Yapısal İyileştirmeler:** * **Örnek:** \"CV'nizde bulunan karmaşık bir tablo, ATS sistemleri tarafından doğru okunamayabilir. Bu bölümü standart madde işaretleri kullanarak yeniden düzenlemenizi öneririm.\" * **Örnek:** \"Başlık olarak 'Profesyonel Yeteneklerim' yerine daha standart olan 'Beceriler' başlığını kullanmanız, ATS uyumunu artıracaktır.\" **2. Anahtar Kelime Optimizasyonu:** * **Örnek (İş ilanı varsa):** \"Başvurduğunuz ilanda 'dijital pazarlama' ve 'SEO' anahtar kelimeleri sıkça geçiyor. Bu kelimeleri iş deneyimlerinizde ve beceriler bölümünüzde daha belirgin bir şekilde kullanmalısınız. Örneğin, 'Pazarlama kampanyaları yönetildi' yerine 'SEO odaklı dijital pazarlama kampanyaları yönetildi' yazabilirsiniz.\" * **Örnek (Genel):** \"Yazılım geliştirici pozisyonu için 'Git' ve 'Docker' gibi popüler teknolojileri beceriler bölümünüze eklemeniz, CV'nizin daha fazla işveren tarafından bulunmasını sağlayacaktır.\" **3. İçeriği Güçlendirme:** * **Örnek:** \"İş deneyimlerinizde sorumluluklarınızı belirtmişsiniz ancak başarılarınıza yeterince yer vermemişsiniz. Örneğin, 'Proje ekibini yönettim' demek yerine, '5 kişilik proje ekibini yöneterek projenin zamanında ve %10 bütçe altında tamamlanmasını sağladım' gibi ölçülebilir sonuçlar ekleyin.\" * **Örnek:** \"CV'nizin başında kariyer hedeflerinizi ve en güçlü yetkinliklerinizi özetleyen 2-3 cümlelik bir 'Profesyonel Özet' bölümü ekleyerek işe alım uzmanının dikkatini hemen çekebilirsiniz.\" **4. Dil ve Profesyonellik:** * **Örnek:** \"CV'nizde birkaç yazım hatası tespit ettim. Göndermeden önce bir yazım denetimi yapmanız profesyonel imajınızı güçlendirecektir.\"";
                // Zorunlu puanlama tablosu formatı talimatı
                atsPrompt += "\n\nYanıtının sonunda mutlaka aşağıdaki tabloyu gerçek sayısal değerlerle ve eksiksiz şekilde, her satırı yeni satırda olacak şekilde ekle. [Puan] veya [Toplam Puan] gibi örnek metinler asla yazma, gerçek rakam yaz!\n" +
                "Puanlama Sonucu:\n" +
                "Hesaplanan ATS Puanınız: [Toplam Puan]/100\n" +
                "Format ve Okunabilirlik: [Puan]/20\n" +
                "Anahtar Kelime Optimizasyonu: [Puan]/35\n" +
                "İçerik ve Yapı: [Puan]/30\n" +
                "Genel Kalite ve Profesyonellik: [Puan]/15" +
                "\n\nSadece özgün, uygulanabilir ve kişiye özel tavsiye üret. Profil linki, iletişim, sosyal medya, kişisel bilgi, özet, analiz, başlık, madde, vs. asla yazma!";
                var atsOptions = new ChatCompletionsOptions()
                {
                    Messages = { new Azure.AI.OpenAI.ChatMessage(Azure.AI.OpenAI.ChatRole.User, atsPrompt) },
                    MaxTokens = 1024,
                    Temperature = 0.7f
                };
                var atsResponse = await _openAiClient.GetChatCompletionsAsync(_deployment, atsOptions);
                var atsResult = atsResponse.Value.Choices[0].Message.Content;
                _logger.LogInformation($"OpenAI ATS yanıtı: {atsResult}");
                // Parse: puan ve atsImprovementTips
                int atsScore = 0;
                double formatScore = 0, keywordScore = 0, contentScore = 0, qualityScore = 0;
                var atsTips = new List<string>();
                var suggestions = new List<string>();
                // Regex ile puanları çek
                var atsScoreMatch = Regex.Match(atsResult, @"Hesaplanan ATS Puanınız:\s*(\d+)");
                if (atsScoreMatch.Success) int.TryParse(atsScoreMatch.Groups[1].Value, out atsScore);
                var formatScoreMatch = Regex.Match(atsResult, @"Format ve Okunabilirlik:\s*(\d+)");
                if (formatScoreMatch.Success) double.TryParse(formatScoreMatch.Groups[1].Value, out formatScore);
                var keywordScoreMatch = Regex.Match(atsResult, @"Anahtar Kelime Optimizasyonu:\s*(\d+)");
                if (keywordScoreMatch.Success) double.TryParse(keywordScoreMatch.Groups[1].Value, out keywordScore);
                var contentScoreMatch = Regex.Match(atsResult, @"İçerik ve Yapı:\s*(\d+)");
                if (contentScoreMatch.Success) double.TryParse(contentScoreMatch.Groups[1].Value, out contentScore);
                var qualityScoreMatch = Regex.Match(atsResult, @"Genel Kalite ve Profesyonellik:\s*(\d+)");
                if (qualityScoreMatch.Success) double.TryParse(qualityScoreMatch.Groups[1].Value, out qualityScore);
                // Tavsiyeleri çek (madde işaretli veya numaralı)
                var ignoreKeywords = new[] {
                    "Ad:", "Soyad:", "İletişim", "Kişisel Bilgi", "Kişisel Bilgiler", "Kişisel", "Pozisyon:", "Tarih:", "Başlangıç ve Bitiş Tarihleri:", "Programlama Dilleri:", "Backend ve Frontend Teknolojileri:", "Veritabanları:", "Diğer Yetenekler:", "Burdur Mehmet Akif Ersoy Üniversitesi", "Merinos Erdemoğlu Holding", "Görev Tanımları", "Full-stack", "DevOps", "Yapay zekâ", "Özet", "Eğitim Bilgileri", "Sertifikalar", "Projeler", "Bölüm:", "Mezuniyet Tarihi:", "GPA:", "Backend Teknolojileri:", "Frontend Teknolojileri:", "Yöntemler ve Uygulamalar:", "Versiyon Kontrol:", "Referanslar:", "Sertifikalar:", "Projeler:", "İletişim:", "Ad:", "Başlangıç ve Bitiş Tarihleri:", "full stack", "stajyer", "mezuniyet", "teknolojiler", "geliştirme deneyimi", "bilgi sahibi", "frontend", "backend", "API", "MVC", "Azure", "React.js", "HTML5", "CSS3", "MSSQL", "PostgreSQL", "SQLite", "Agile", "Scrum", "Temiz Kod", "Katmanlı Mimari", "TDD", "Git", "GitHub", "proje", "sertifika", "referans", "bölüm", "not ortalaması", "puan", "iletişim", "eğitim", "tarih", "pozisyon", "görev", "sorumluluk", "deneyim", "şirket", "üniversite", "bölüm", "başlangıç", "bitiş", "rol", "alanı", "alanında", "bilgisi", "bilgileri", "bilgisine", "bilgileriniz", "bilgiler", "bilgi", "analiz", "başlık", "madde", "LinkedIn", "Profil", "www.linkedin.com", "http", "https", "linkedin.com", "profil linki", "sosyal medya", "instagram", "twitter", "facebook"
                };
                var tipLines = atsResult.Split('\n').Where(l => l.Trim().StartsWith("- ") || l.Trim().StartsWith("• ") || l.Trim().StartsWith("1.") || l.Trim().StartsWith("2.") || l.Trim().StartsWith("3.") || l.Trim().StartsWith("4.") || l.Trim().StartsWith("5.")).ToList();
                string lastTip = null;
                foreach (var line in tipLines)
                {
                    var clean = line.TrimStart('-', '•', ' ', '\t', '1', '2', '3', '4', '5', '.').Trim();
                    // Başlık olan ve açıklaması olmayan satırı gösterme
                    bool isHeader = clean.EndsWith(":") || clean.EndsWith(":") || clean.EndsWith("İyileştirmeler") || clean.EndsWith("Optimizasyonu") || clean.EndsWith("Güçlendirme") || clean.EndsWith("Profesyonellik");
                    if (!string.IsNullOrWhiteSpace(clean) && !ignoreKeywords.Any(k => clean.StartsWith(k) || clean.Contains(k))) {
                        if (isHeader) {
                            lastTip = clean;
                            continue;
                        }
                        // Eğer önceki satır başlıksa ve bu satır açıklama ise, başlık + açıklama olarak ekle
                        if (lastTip != null) {
                            atsTips.Add(lastTip + " " + clean);
                            lastTip = null;
                        } else {
                            atsTips.Add(clean);
                        }
                    }
                }
                report.AtsScore = atsScore;
                report.ContentScore = contentScore;
                report.AtsImprovementTips = atsTips;
                // Puanlama detaylarını çek
                var detailsMatch = Regex.Match(atsResult, @"Puanlama Sonucu:(?:\r?\n)?([\s\S]+?Genel Kalite ve Profesyonellik: ?\d+/15)", RegexOptions.Multiline);
                if (detailsMatch.Success)
                {
                    var details = "Puanlama Sonucu:\n" + detailsMatch.Groups[1].Value.Trim();
                    // Satır sonu eksikse, puan satırlarını ayır
                    details = Regex.Replace(details, @"(\d+/100)(?=Format)", "$1\n");
                    details = Regex.Replace(details, @"(\d+/20)(?=Anahtar)", "$1\n");
                    details = Regex.Replace(details, @"(\d+/35)(?=İçerik)", "$1\n");
                    details = Regex.Replace(details, @"(\d+/30)(?=Genel)", "$1\n");
                    report.AtsScoreDetails = details;
                }
                else
                {
                    // Fallback: atsImprovementTips veya suggestions içinden 'puan' içeren satırları birleştir
                    var puanSatirlari = atsTips.Where(t => t.Contains("puan", StringComparison.OrdinalIgnoreCase) || t.Contains("Puan", StringComparison.OrdinalIgnoreCase));
                    if (puanSatirlari.Any())
                        report.AtsScoreDetails = string.Join("\n", puanSatirlari);
                    else if (suggestions.Any())
                    {
                        var puanSatirlari2 = suggestions.Where(t => t.Contains("puan", StringComparison.OrdinalIgnoreCase) || t.Contains("Puan", StringComparison.OrdinalIgnoreCase));
                        if (puanSatirlari2.Any())
                            report.AtsScoreDetails = string.Join("\n", puanSatirlari2);
                    }
                }
                // 2. Ekstra tavsiyeler için prompt
                string extraPrompt = @"Aşağıda bir özgeçmiş (CV) ve iş ilanı metni verilmiştir.
CV'nin genel kalitesini, sunumunu ve iş ilanına uygunluğunu artırmak için, yaratıcı, kişiye özel ve uygulanabilir 5 öneri verin. İş ilanındaki maddeleri veya gereksinimleri tekrar etme, kopyalama. Klasik/generik tavsiyeler verme, özgün ve uygulanabilir öneriler ver. Her öneri özgün, spesifik ve doğrudan CV ve iş ilanı içeriğine dayalı olmalı. Sadece CV'yi geliştirmek için özgün, uygulanabilir ve kişiye özel öneriler yaz. CV özetini, analizini, pozisyon, tarih, eğitim, teknoloji, GPA, görev, bölüm, iletişim, referans, sertifika, proje gibi bilgi tekrarlarını asla yazma. Sadece tavsiye ve öneri maddeleri üret! Her başlık için mutlaka özgün ve uygulanabilir açıklama/tavsiye ekle, başlıkları asla boş bırakma. Önerileri Türkçe yazın.
Yanıtı şu formatta ver:
Tavsiyeler:
- ...
- ...";
                extraPrompt += $"\n\nCV metni:\n{cvText}\n\nİş ilanı metni:\n{jobDescription}";
                var extraOptions = new ChatCompletionsOptions()
                {
                    Messages = { new Azure.AI.OpenAI.ChatMessage(Azure.AI.OpenAI.ChatRole.User, extraPrompt) },
                    MaxTokens = 1024,
                    Temperature = 0.7f
                };
                var extraResponse = await _openAiClient.GetChatCompletionsAsync(_deployment, extraOptions);
                var extraResult = extraResponse.Value.Choices[0].Message.Content;
                // Parse: extraAdvice
                var extraAdvice = new List<string>();
                foreach (var line in extraResult.Split('\n'))
                {
                    if (line.Trim().StartsWith("-"))
                        extraAdvice.Add(line.Trim().TrimStart('-').Trim());
                }
                report.ExtraAdvice = extraAdvice;
                // Tüm tavsiyeleri birleştir suggestions'a ata
                suggestions.AddRange(atsTips);
                suggestions.AddRange(extraAdvice);
                report.Suggestions = suggestions;
                // Anahtar kelime analizi: iş ilanı ve CV metni boş değilse çalıştır
                if (!string.IsNullOrWhiteSpace(jobDescription) && !string.IsNullOrWhiteSpace(cvText))
                {
                    string jobKeywordPrompt = $@"Aşağıdaki iş ilanı metninden, pozisyon için en önemli 10 anahtar kelimeyi veya anahtar kelime öbeklerini (tercihen 1-2 kelimelik) çıkar. Sadece anahtar kelimeleri virgül ile ayırarak sırala. Açıklama veya başka bir şey ekleme.\n\nİş ilanı metni:\n{jobDescription}\n";
                    var jobKeywordOptions = new ChatCompletionsOptions()
                    {
                        Messages = { new Azure.AI.OpenAI.ChatMessage(Azure.AI.OpenAI.ChatRole.User, jobKeywordPrompt) },
                        MaxTokens = 128,
                        Temperature = 0.2f
                    };
                    var jobKeywordResponse = await _openAiClient.GetChatCompletionsAsync(_deployment, jobKeywordOptions);
                    var jobKeywordResult = jobKeywordResponse.Value.Choices[0].Message.Content;
                    var jobKeywords = jobKeywordResult.Split(',')
                        .Select(k => k.Trim().ToLowerInvariant())
                        .Where(k => k.Length > 2)
                        .Distinct()
                        .ToList();
                    string cvKeywordPrompt = $@"Aşağıdaki CV metninden, pozisyon için en önemli 10 anahtar kelimeyi veya anahtar kelime öbeklerini (tercihen 1-2 kelimelik) çıkar. Sadece anahtar kelimeleri virgül ile ayırarak sırala. Açıklama veya başka bir şey ekleme.\n\nCV metni:\n{cvText}\n";
                    var cvKeywordOptions = new ChatCompletionsOptions()
                    {
                        Messages = { new Azure.AI.OpenAI.ChatMessage(Azure.AI.OpenAI.ChatRole.User, cvKeywordPrompt) },
                        MaxTokens = 128,
                        Temperature = 0.2f
                    };
                    var cvKeywordResponse = await _openAiClient.GetChatCompletionsAsync(_deployment, cvKeywordOptions);
                    var cvKeywordResult = cvKeywordResponse.Value.Choices[0].Message.Content;
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
                _logger.LogInformation($"Analiz tamamlandı. ATS Puanı: {atsScore}, ATS öneri sayısı: {atsTips.Count}, Ekstra tavsiye sayısı: {extraAdvice.Count}");
                return report;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"Azure OpenAI API hatası: {ex.Message}, Status: {ex.Status}, ErrorCode: {ex.ErrorCode}");
                string errorMessage;
                if (ex.Status == 404)
                {
                    errorMessage = lang == "en" 
                        ? "Azure OpenAI service not found. Please check if the service is active in Azure portal."
                        : "Azure OpenAI servisi bulunamadı. Lütfen Azure portal'da servisin aktif olduğundan emin olun.";
                }
                else if (ex.Status == 401)
                {
                    errorMessage = lang == "en"
                        ? "Azure OpenAI API key is invalid. Please check your API key."
                        : "Azure OpenAI API anahtarı geçersiz. Lütfen API anahtarını kontrol edin.";
                }
                else
                {
                    errorMessage = lang == "en"
                        ? $"Azure OpenAI API error: {ex.Message} (Status: {ex.Status})"
                        : $"Azure OpenAI API hatası: {ex.Message} (Status: {ex.Status})";
                }
                report.AtsScore = 0;
                report.Suggestions.Add(errorMessage);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metin analizi sırasında hata oluştu");
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