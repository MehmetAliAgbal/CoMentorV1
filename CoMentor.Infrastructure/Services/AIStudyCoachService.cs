using System.Text.Json;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Domain.Entities;
using CoMentor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoMentor.Infrastructure.Services;

public class AIStudyCoachService : IAIStudyCoachService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ChatClient _chatClient;
    private readonly IMlPredictionService _mlPredictionService;
    private readonly IYouTubeService _youtubeService;

    public AIStudyCoachService(AppDbContext db, IConfiguration configuration, IMlPredictionService mlPredictionService, IYouTubeService youtubeService)
    {
        _db = db;
        _configuration = configuration;
        _mlPredictionService = mlPredictionService;
        _youtubeService = youtubeService;

        var apiKey = _configuration["AIService:ApiKey"];
        var model = _configuration["AIService:Model"] ?? "gpt-4o";

        if (string.IsNullOrEmpty(apiKey))
            // throw new InvalidOperationException("OpenAI API Key is missing."); 
            // Dev ortamında hata fırlatmayalım, mock veya boş dönebiliriz ama şimdilik hata fırlatıyoruz kullanıcı fark etsin diye
            throw new InvalidOperationException("OpenAI API Key is missing in configuration.");

        _chatClient = new ChatClient(model: model, apiKey: apiKey);
    }

    public async Task<List<StudyScheduleDto>> GenerateScheduleAsync(int userId, GenerateScheduleRequestDto request)
    {
        // 1. Kullanıcı verilerini topla
        var user = await _db.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        var recentTrials = await _db.TrialExams
            .Include(t => t.SubjectScores)
                .ThenInclude(s => s.Subject)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.ExamDate)
            .Take(5)
            .ToListAsync();

        var allSubjects = await _db.Subjects.Where(s => s.IsActive).ToListAsync();

        string mlPredictionResult = "";
        if (recentTrials.Any())
        {
            try
            {
                if (request.ExamType?.ToUpper() == "SAYISAL")
                {
                    var tahminReq = new SayisalTahminRequest();
                    for (int i = 0; i < recentTrials.Count; i++)
                    {
                        var t = recentTrials[i];
                        // Geçmişten günümüze doğru sıralamak için Count - i da yapılabilir, şimdilik index veriyoruz
                        tahminReq.Denemeler.Add(i + 1, new SayisalDenemeDto
                        {
                            Matematik = t.SubjectScores.Where(s => s.Subject.Name.Contains("Matematik") || s.Subject.Name.Contains("Geometri")).Sum(s => s.NetScore),
                            Fen = t.SubjectScores.Where(s => s.Subject.Name.Contains("Fizik") || s.Subject.Name.Contains("Kimya") || s.Subject.Name.Contains("Biyoloji") || s.Subject.Name.Contains("Fen")).Sum(s => s.NetScore)
                        });
                    }
                    mlPredictionResult = await _mlPredictionService.GetSayisalTahminAsync(tahminReq);
                }
                else
                {
                    var tahminReq = new TytTahminRequest();
                    for (int i = 0; i < recentTrials.Count; i++)
                    {
                        var t = recentTrials[i];
                        tahminReq.Denemeler.Add(i + 1, new TytDenemeDto
                        {
                            Turkce = t.SubjectScores.FirstOrDefault(s => s.Subject.Name.Contains("Türkçe"))?.NetScore ?? 0,
                            Matematik = t.SubjectScores.Where(s => s.Subject.Name.Contains("Matematik") || s.Subject.Name.Contains("Geometri")).Sum(s => s.NetScore),
                            Fen = t.SubjectScores.Where(s => s.Subject.Name.Contains("Fen") || s.Subject.Name.Contains("Fizik") || s.Subject.Name.Contains("Kimya") || s.Subject.Name.Contains("Biyoloji")).Sum(s => s.NetScore),
                            Sosyal = t.SubjectScores.Where(s => s.Subject.Name.Contains("Sosyal") || s.Subject.Name.Contains("Tarih") || s.Subject.Name.Contains("Coğrafya") || s.Subject.Name.Contains("Felsefe")).Sum(s => s.NetScore)
                        });
                    }
                    mlPredictionResult = await _mlPredictionService.GetTytTahminAsync(tahminReq);
                }
            }
            catch { /* ML servisi ayakta değilse failover, sessizce geç */ }
        }

        // 2. Prompt hazırla
        var prompt = BuildPrompt(user, recentTrials, allSubjects, request, mlPredictionResult);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Sen uzman bir YKS (TYT/AYT) öğrenci koçusun. Öğrencinin deneme sonuçlarına göre ona haftalık ders çalışma programı hazırlamalısın. " +
                                  "Önce <planlama> etiketleri arasında adım adım mantığını kur, ardından sadece JSON formatında programı döndür."),
            new UserChatMessage(prompt)
        };

        try 
        {
            // ClientResult<ChatCompletion> handled
            var result = await _chatClient.CompleteChatAsync(messages);
            ChatCompletion completion = result.Value;
            
            var responseText = completion.Content[0].Text;
            
            // AI <planlama> yapacağı için metinden sadece JSON dizisini ( [ ... ] ) ayıklamalıyız
            int startIndex = responseText.IndexOf('[');
            int endIndex = responseText.LastIndexOf(']');

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                responseText = responseText.Substring(startIndex, endIndex - startIndex + 1);
            }
            else
            {
                responseText = responseText.Replace("```json", "").Replace("```", "").Trim();
            }

            // Geçici DTO (AI Response yapısı)
            var aiOptions = new List<AIResponseScheduleDto>();
             try {
                aiOptions = JsonSerializer.Deserialize<List<AIResponseScheduleDto>>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            } catch {
                // Eğer AI JSON formatını bozduysa boş dön veya tekrar dene (basitlik için boş dönüyoruz)
                return new List<StudyScheduleDto>();
            }

            if (aiOptions == null || !aiOptions.Any())
                throw new Exception("AI failed to generate a valid schedule.");

            // 3. Veritabanına kaydet ve DTO'ya dönüştür
            var scheduleDtos = new List<StudyScheduleDto>();
            var newEntities = new List<StudySchedule>();
            var newVideoRecommendations = new List<VideoRecommendation>();
            var fetchedQueries = new HashSet<string>();
            
            // Eski aktif programları pasife çekiyoruz
            var oldSchedules = await _db.StudySchedules
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();
            
            foreach (var old in oldSchedules)
            {
                old.IsActive = false;
            }

            // AI'ın aynı gün ve aynı saate birden fazla kayıt atması durumunu engelliyoruz
            var uniqueAiOptions = aiOptions
                .GroupBy(x => new { x.DayOfWeek, x.StartTime })
                .Select(g => g.First())
                .ToList();

            foreach (var s in uniqueAiOptions)
            {
                var subject = allSubjects.FirstOrDefault(sub => sub.Name.Equals(s.SubjectName, StringComparison.OrdinalIgnoreCase));
                if (subject == null) continue; 

                TimeOnly start = TimeOnly.Parse(s.StartTime);
                TimeOnly end = TimeOnly.Parse(s.EndTime);
                
                var entity = new StudySchedule
                {
                    UserId = userId,
                    SubjectId = subject.Id,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = start,
                    EndTime = end,
                    Topic = s.Topic,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                newEntities.Add(entity);

                // WeakTopic Video Suggestion Resolution
                if (!string.IsNullOrWhiteSpace(s.VideoSuggestionKeyword) && fetchedQueries.Count < 3) 
                {
                    if (fetchedQueries.Add(s.VideoSuggestionKeyword))
                    {
                        var topVideo = await _youtubeService.GetTopVideoAsync(s.VideoSuggestionKeyword);
                        if (topVideo != null)
                        {
                            newVideoRecommendations.Add(new VideoRecommendation
                            {
                                UserId = userId,
                                SubjectId = subject.Id,
                                Title = topVideo.Value.Title,
                                Url = topVideo.Value.Url,
                                Priority = 1,
                                RecommendedAt = DateTime.UtcNow,
                                IsWatched = false
                            });
                        }
                    }
                }

                // DTO oluştur (Response için)
                scheduleDtos.Add(new StudyScheduleDto
                {
                    Id = 0, // DB'ye kaydettikten sonra güncellenebilir veya ID'siz dönebiliriz. Kayıttan sonra ID alacağız.
                    SubjectId = subject.Id,
                    SubjectName = subject.Name,
                    SubjectShortName = subject.ShortName,
                    SubjectColorHex = subject.ColorHex,
                    DayOfWeek = s.DayOfWeek,
                    DayName = s.DayName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationMinutes = (int)(end - start).TotalMinutes,
                    Topic = s.Topic,
                    IsActive = true
                });
            }

            _db.StudySchedules.AddRange(newEntities);
            if (newVideoRecommendations.Any())
            {
                _db.VideoRecommendations.AddRange(newVideoRecommendations);
            }
            await _db.SaveChangesAsync();

            // ID'leri DTO'lara geri yükle (sıralı eklendiği varsayımıyla veya tekrar query ile)
            // Basitlik için ID'leri set etmiyoruz veya entities'den alıyoruz
            for(int i=0; i < newEntities.Count && i < scheduleDtos.Count; i++)
            {
                scheduleDtos[i].Id = newEntities[i].Id;
            }
            
            return scheduleDtos;
        }
        catch (Exception ex)
        {
            throw new Exception($"AI generation failed: {ex.Message}", ex);
        }
    }

    private string BuildPrompt(User user, List<TrialExam> trials, List<Subject> subjects, GenerateScheduleRequestDto request, string mlPredictionResult)
    {
        // 1. DERS FİLTRELEME
        if (request.SubjectPreferences != null && request.SubjectPreferences.Any())
        {
            var preferredSubjectNames = request.SubjectPreferences.Select(p => p.SubjectName).ToList();
            subjects = subjects.Where(s => preferredSubjectNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        }
        var subjectList = string.Join(", ", subjects.Select(s => s.Name));

        // 2. MÜSAİTLİK ANALİZİ
        var availabilityInfo = "Öğrenci haftalık hedef saat: " + (request.TargetWeeklyHours ?? 20) + " saat.";
        var availableSlotsText = "";
        int totalAvailableHours = request.TargetWeeklyHours ?? 20;

        if (request.WeeklyAvailability != null && request.WeeklyAvailability.Any())
        {
            totalAvailableHours = 0; // Eğer detaylı takvim geldiyse üstüne yaz
            availableSlotsText = "\n\nÖğrencinin MÜSAİT OLDUĞU SPESİFİK ZAMAN DİLİMLERİ (AŞAĞIDAKİ LİSTEDEKİ HER BİR SLOT İÇİN JSON'DA TAM OLARAK 1 ADET KAYIT OLUŞTURULMALIDIR!):";
            foreach (var day in request.WeeklyAvailability)
            {
                var dayName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((DayOfWeek)day.DayOfWeek);
                availableSlotsText += $"\n- {dayName} (DayOfWeek: {day.DayOfWeek}): {string.Join(", ", day.TimeSlots)}";
                
                // Basitçe her slotu 1 saat sayıyoruz
                totalAvailableHours += day.TimeSlots.Count;
            }
        }

        // 3. KONU VE ZAMAN ANALİZİ (Dinamik Ağırlık Sistemi)
        var topicsInfo = "Öğrencinin SEÇTİĞİ DİZİNLER (Genel Havuz):";
        if (request.SubjectPreferences != null && request.SubjectPreferences.Any())
        {
            foreach(var pref in request.SubjectPreferences)
            {
                if (pref.SelectedTopics.Any())
                    topicsInfo += $"\n- {pref.SubjectName}: {string.Join(", ", pref.SelectedTopics)}";
            }
        }

        var weakTopicsText = "";
        if (request.WeakTopics != null && request.WeakTopics.Any())
        {
            weakTopicsText = $"\n\nÖğrencinin DİKKAT ÇEKTİĞİ / ZORLANDIĞI KONULAR (ÇOK YÜKSEK ÖNCELİK VER): \n- {string.Join("\n- ", request.WeakTopics)}";
        }

        var mlInsights = "";
        if (!string.IsNullOrEmpty(mlPredictionResult))
        {
            mlInsights = $"\n\nMAKİNE ÖĞRENMESİ (ML) ANALİZİ VE TAHMİNLERİ:\n{mlPredictionResult}\nBu JSON verisindeki tahminlere bakarak (eğer varsa), öğrencinin hangi derste potansiyel olarak düştüğünü veya gelişime açık olduğunu tespit et ve o derse daha fazla saat ayır.";
        }

        // 5. DENEME SONUÇLARI
        var trialSummary = "Henüz deneme sınavı girilmemiş.";
        var strategyHint = "Genel ve Dengeli Çalışma";
        
        if (trials.Any())
        {
            trialSummary = string.Join("\n", trials.Select(t => 
                $"- {t.ExamDate.ToShortDateString()} ({t.ExamType}): Toplam {t.TotalScore} net. " +
                $"Detaylar: {string.Join(", ", t.SubjectScores.Select(s => $"{s.Subject?.Name}: {s.NetScore} Net"))}"
            ));
            
            var lastTrialScore = trials.First().TotalScore;
            // TYT için ortalama 120, AYT sayısal için 80 soru.
            // Örnek basit bir strateji (Daha detaylısı ML veya net oranlarıyla yapılabilir)
            if (request.ExamType == "TYT") {
                if (lastTrialScore < 45) strategyHint = "Konu Odaklı (%70 Konu, %30 Soru)";
                else if (lastTrialScore < 80) strategyHint = "Dengeli Çalışma (%40 Konu, %60 Soru)";
                else strategyHint = "Pratik Odaklı (%90 Soru Çözümü ve Branş Denemesi)";
            } else {
                 if (lastTrialScore < 25) strategyHint = "Konu Odaklı (%70 Konu, %30 Soru)";
                else if (lastTrialScore < 50) strategyHint = "Dengeli Çalışma (%40 Konu, %60 Soru)";
                else strategyHint = "Pratik Odaklı (%90 Soru Çözümü ve Branş Denemesi)";
            }
        }

        // 6. ÖN KOŞUL VE MANTIK KONTROLLERİ
        var logicRules = @"
        - KRİTİK SIRALAMA YASASI (TÜM DERSLER İÇİN): Sana verdiğim 'Öğrencinin SEÇTİĞİ DİZİNLER' listesindeki konular, kendi içlerinde KRONOLOJİK bir sıradadır. ZAYIF KONU KRONOLOJİYİ BOZAMAZ: Bir konu 'Zayıf konu' olsa bile, sıralamadakilerden ÖNCE İŞLENEMEZ! Kronolojik sıralamayı ASLA bozma!
        - TİP KRONOLOJİSİ (ÖNCE KONU, SONRA SORU): Aynı konunun çalışma tipleri de kendi içinde sıralıdır. Bir konunun ÖNCE 'Konu Anlatımı' atanmalı, SONRA (daha geç bir saate/güne) 'Soru Çözümü' veya 'Deneme' atanmalıdır. Konu anlatımını görmeden soru çözümü veya deneme atamak KESİNLİKLE YASAKTIR.
        - YIĞILMA ÖNLEYİCİ: Aynı dersin ardışık DOĞASI FARKLI konularını (Örn: Matematikte Limit ve İntegral) AYNI GÜNE (aynı slotların içine) sıkıştırma. Gerekirse araya Fizik/Türkçe koy veya farklı günlere yay.
        - ZORLUK YÖNETİMİ: Zor dersleri akşam saatlerine (18:00 sonrası) koyma (Eğer sabah boşluk varsa).
        ";

        return $@"
        SEN UZMAN BİR YKS KOÇUSUN. Aşağıdaki kısıtlara göre gerçekçi ve kişiselleştirilmiş bir program hazırla.

        DURUM ANALİZİ:
        - Müsait Süre: TOPLAM {totalAvailableHours} adet slot (1 slot = ~50dk ders) var. Döneceğin JSON dizisi TAM OLARAK {totalAvailableHours} ELEMANLI OLMAK ZORUNDA!
        {availabilityInfo}
        {availableSlotsText}
        
        {topicsInfo}
        {weakTopicsText}
        {mlInsights}

        SON DENEME PERFORMANSI VE AI STRATEJİSİ:
        {trialSummary}
        UYGULANACAK ÇALIŞMA MODU: {strategyHint} (Bunu programı kurgularken göz önünde bulundur. Mesela pratik odaklıysa konudan ziyade soru çözümü veya deneme atamaları yap.)

        MEVCUT DERSLER: {subjectList}

        GÖREV VE KURALLAR:
        1. **Eksiksiz Program**: MÜSAİT OLAN TÜM SAATLERİ DOLDUR. Dizide tam {totalAvailableHours} kayıt bulunmalı. Hiçbir saati atlama, gerekirse 'Branş Denemesi' veya 'Genel Tekrar' yazarak doldur.
        
        2. **Zaman Uyumu**: 'DayOfWeek' ve 'StartTime' alanlarını yukarıda sana verdiğim spesifik slotlarla birebir eşleştir. Bitiş saatini (EndTime) StartTime'dan 50 dakika sonrası olarak hesapla (örn: StartTime: 19:00 ise EndTime: 19:50).
        
        3. **İLERLEME VE AĞIRLIK MANTIĞI**: 
           Zayıf konulara (weak topics) çok daha fazla saat ağırlığı (weight) ver.
           - EKSTRA GÖREV: Eğer atadığın konu öğrencinin 'Zayıf Konular (WeakTopics)' listesinde ise, bu konuyu en iyi açıklayan Türkçe bir YouTube arama kelimesi bul (örn: 'Rehber Matematik Limit') ve JSON içindeki 'VideoSuggestionKeyword' alanına yaz. Eğer bu konu zayıf konulardan biri DEĞİLSE, kesinlikle boş ('') bırak.
           {logicRules}

        4. **ADIM ADIM DÜŞÜNME (CHAIN OF THOUGHT)**:
           Hemen JSON yazmaya başlama! Önce `<planlama> ... </planlama>` etiketleri arasında hangi derse kaç saat vereceğini, limit vs türev konularını hangi sırayla hangi günlere yerleştireceğini adım adım mantıksal bir şekilde planla.
           Planlaman bittikten sonra EN SON adımda, plandaki dağılıma tam uyumlu JSON çıktısını ````json ... ```` bloğu içinde ekle.

        YANIT FORMATI:
        <planlama>
        (Düşünce süreçlerin...)
        </planlama>
        ```json
        [
          {{
            ""SubjectName"": ""Matematik"",
            ""DayOfWeek"": 1,
            ""DayName"": ""Pazartesi"",
            ""StartTime"": ""19:00"", 
            ""EndTime"": ""19:50"",
            ""Topic"": ""Limit (Konu Anlatımı)"",
            ""VideoSuggestionKeyword"": ""Rehber Matematik Limit""
          }}
        ]
        ```
        ";
    }

    // AI'dan gelen JSON'ı karşılamak için
    private class AIResponseScheduleDto
    {
        public string SubjectName { get; set; } = "";
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string? Topic { get; set; }
        public string? VideoSuggestionKeyword { get; set; }
    }
}
