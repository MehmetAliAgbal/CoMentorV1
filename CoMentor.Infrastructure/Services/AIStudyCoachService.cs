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

    public AIStudyCoachService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;

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

        // 2. Prompt hazırla
        var prompt = BuildPrompt(user, recentTrials, allSubjects, request);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Sen uzman bir YKS (TYT/AYT) öğrenci koçusun. Öğrencinin deneme sonuçlarına göre ona haftalık ders çalışma programı hazırlamalısın. " +
                                  "Programı JSON formatında döndür. Sadece JSON döndür, açıklama yapma."),
            new UserChatMessage(prompt)
        };

        try 
        {
            // ClientResult<ChatCompletion> handled
            var result = await _chatClient.CompleteChatAsync(messages);
            ChatCompletion completion = result.Value;
            
            var responseText = completion.Content[0].Text;
            responseText = responseText.Replace("```json", "").Replace("```", "").Trim();

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
            
            // Eski aktif programları pasife çekelim (opsiyonel, şimdilik yapmıyoruz)

            foreach (var s in aiOptions)
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

    private string BuildPrompt(User user, List<TrialExam> trials, List<Subject> subjects, GenerateScheduleRequestDto request)
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
        int totalAvailableHours = 0;

        if (request.WeeklyAvailability != null && request.WeeklyAvailability.Any())
        {
            availableSlotsText = "\n\nÖğrencinin MÜSAİT OLDUĞU ZAMAN DİLİMLERİ:";
            foreach (var day in request.WeeklyAvailability)
            {
                var dayName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((DayOfWeek)day.DayOfWeek);
                availableSlotsText += $"\n- {dayName} (Day {day.DayOfWeek}): {string.Join(", ", day.TimeSlots)}";
                
                // Basitçe her slotu 1 saat sayıyoruz (daha hassas hesap için slot aralığına bakılabilir)
                totalAvailableHours += day.TimeSlots.Count;
            }
        }

        // 3. KONU VE ZAMAN ANALİZİ (Metadata Kullanımı)
        var topicsInfo = "Öğrencinin SEÇTİĞİ KONULAR ve TAHMİNİ SÜRELERİ:";
        int totalRequiredHours = 0;
        var strictModeInstruction = "";

        if (request.SubjectPreferences != null && request.SubjectPreferences.Any())
        {
            foreach(var pref in request.SubjectPreferences)
            {
                foreach(var topic in pref.SelectedTopics)
                {
                    // Metadata'dan süre ve zorluk al (yoksa varsayılan 2 saat)
                    var meta = CoMentor.Domain.Constants.TopicMetadata.TopicDetails.ContainsKey(topic) 
                        ? CoMentor.Domain.Constants.TopicMetadata.TopicDetails[topic] 
                        : (Hours: 2, Difficulty: 3);
                    
                    totalRequiredHours += meta.Hours;
                    topicsInfo += $"\n- {pref.SubjectName} / {topic}: {meta.Hours} saat (Zorluk: {meta.Difficulty}/5)";
                }
            }
            strictModeInstruction = "DİKKAT: Sadece seçilen bu konuları kullan. Başka ders ekleme.";
        }
        else 
        {
             topicsInfo += " (Genel tekrar ve eksik kapatma)";
        }

        // 4. KAPASİTE KONTROLÜ MESAJI
        var capacityWarning = "";
        if (totalRequiredHours > totalAvailableHours && totalAvailableHours > 0)
        {
            capacityWarning = $"\nUYARI: Öğrenci {totalRequiredHours} saatlik konu seçti ama sadece {totalAvailableHours} saat müsaitliği var! " +
                              $"Bu durumda EN ÖNEMLİ konuları programa yerleştir, sığmayanları dışarıda bırak ve not olarak belirt.";
        }

        // 5. DENEME SONUÇLARI
        var trialSummary = "Henüz deneme sınavı girilmemiş.";
        if (trials.Any())
        {
            trialSummary = string.Join("\n", trials.Select(t => 
                $"- {t.ExamDate.ToShortDateString()} ({t.ExamType}): Toplam {t.TotalScore} puan. " +
                $"Detaylar: {string.Join(", ", t.SubjectScores.Select(s => $"{s.Subject?.Name}: {s.CorrectAnswers}D/{s.WrongAnswers}Y"))}"
            ));
        }

        // 6. ÖN KOŞUL VE MANTIK KONTROLLERİ
        var logicRules = @"
        - MATEMATİK SIRALAMASI: Temel Kavramlar -> Fonksiyonlar -> Limit -> Türev -> İntegral. ASLA bu sırayı bozma. (Önce Limit bitmeli, sonra Türev başlamalı).
        - GEOMETRİ SIRALAMASI: Üçgenler -> Çokgenler -> Analitik Geometri.
        - FİZİK SIRALAMASI: Hareket -> Dinamik -> Enerji.
        - ZORLUK YÖNETİMİ: Zorluk seviyesi 4 ve 5 olan dersleri (Türev, İntegral, Fizik) ASLA akşam saatlerine (18:00 sonrası) koyma (Eğer sabah boşluk varsa).
        ";

        return $@"
        SEN UZMAN BİR YKS KOÇUSUN. Aşağıdaki kısıtlara göre gerçekçi bir program hazırla.

        DURUM ANALİZİ:
        - Müsait Süre: {totalAvailableHours} saat
        - Gereken Süre: {totalRequiredHours} saat
        {capacityWarning}

        {availabilityInfo}
        {availableSlotsText}
        
        {topicsInfo}
        
        MEVCUT DERSLER: {subjectList}

        SON DENEME PERFORMANSI:
        {trialSummary}

        GÖREV VE KURALLAR:
        1. **Sadece Müsait Saatleri Kullan**: Belirtilen gün ve saatler dışına asla çıkma.
        
        2. **Pomodoro Tekniği**: Uzun blokları (örn: 2 saat) tek parça yazma. Mümkünse '50 dk Ders + 10 dk Mola' mantığını gözet. 
           (Ancak çıktı JSON'da sadece ders saatini yaz, mola detayını karıştırma).
        
        3. **İLERLEME MANTIĞI VE SIRALAMA (ÇOK ÖNEMLİ)**: 
           {logicRules}
           - Eğer listede hem Limit hem Türev varsa, haftanın erken günlerine/saatlerine ÖNCE Limit'i koy.
           - Limit bitmeden Türev'e geçme.

        4. **Zorluk Dengesi**: 
           - Zor dersleri (Zorluk 4-5) **Salı 09:00** gibi zihnin açık olduğu saatlere koy.
           - Akşam saatlerine (18:00 sonrası) Paragraf, Sosyal veya daha hafif/tekrar konularını koy.
        
        5. **Süre Yönetimi**: 
           - Eğer [Gereken Süre > Müsait Süre] ise: En yüksek zorluk veya temel eksiklik olan konulara öncelik ver. Sığmayanları es geç.

        YANIT FORMATI (JSON Listesi):
        [
          {{
            ""SubjectName"": ""Matematik"",
            ""DayOfWeek"": 1,
            ""DayName"": ""Pazartesi"",
            ""StartTime"": ""09:00"", 
            ""EndTime"": ""09:50"", // Pomodoro
            ""Topic"": ""Limit (Konu Anlatımı)"" 
          }}
        ]
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
    }
}
