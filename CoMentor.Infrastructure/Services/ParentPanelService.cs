using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services
{
    public class ParentPanelService : IParentPanelService
    {
        private readonly AppDbContext _db;
        private readonly ChatClient _chatClient;

        public ParentPanelService(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            var apiKey = configuration["AIService:ApiKey"];
            var model = configuration["AIService:Model"] ?? "gpt-4o";

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenAI API Key is missing in configuration.");

            _chatClient = new ChatClient(model: model, apiKey: apiKey);
        }

        public async Task<ParentDashboardDto> GetDashboardAsync(int parentId)
        {
            var parent = await _db.Parents
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == parentId);

            if (parent == null)
                throw new Exception("Parent not found.");

            var dashboard = new ParentDashboardDto
            {
                ParentName = $"{parent.Name} {parent.Surname}",
                StudentName = $"{parent.Student.Name} {parent.Student.Surname}"
            };

            // Son 5 denemeyi al
            var recentTrials = await _db.TrialExams
                .Include(t => t.SubjectScores)
                .Where(t => t.UserId == parent.StudentId)
                .OrderBy(t => t.ExamDate)
                .Take(5)
                .ToListAsync();

            for (int i = 0; i < recentTrials.Count; i++)
            {
                dashboard.TrialExams.Add(new TrialExamProgressDto
                {
                    Id = recentTrials[i].Id,
                    DateLabel = $"D{i + 1}",
                    ExamType = recentTrials[i].ExamType,
                    TotalScore = recentTrials[i].TotalScore
                });
            }

            // AI Özet Oluşturma
            dashboard.AISummary = await GenerateParentSummaryAsync(parent.Student.Name, recentTrials);

            return dashboard;
        }

        public async Task<List<AnnouncementDto>> GetMessagesAsync(int parentId)
        {
            var parent = await _db.Parents
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == parentId);

            if (parent == null)
                return new List<AnnouncementDto>();

            return await _db.Announcements
                .Include(a => a.Teacher)
                .Include(a => a.Classroom)
                .Include(a => a.User)
                .Include(a => a.Parent)
                .Where(a => 
                    a.ParentId == parentId || 
                    (a.UserId == parent.StudentId && (a.TargetAudience == "Parents" || a.TargetAudience == "Both")) ||
                    (parent.Student.ClassroomId != null && a.ClassroomId == parent.Student.ClassroomId && (a.TargetAudience == "Parents" || a.TargetAudience == "Both"))
                )
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementDto
                {
                    Id = a.Id,
                    TeacherId = a.TeacherId,
                    TeacherName = $"{a.Teacher.Name} {a.Teacher.Surname}",
                    Title = a.Title,
                    Message = a.Message,
                    CreatedAt = a.CreatedAt,
                    ClassroomId = a.ClassroomId,
                    ClassroomName = a.Classroom != null ? a.Classroom.Name : null,
                    UserId = a.UserId,
                    UserName = a.User != null ? $"{a.User.Name} {a.User.Surname}" : null,
                    ParentId = a.ParentId,
                    ParentName = a.Parent != null ? $"{a.Parent.Name} {a.Parent.Surname}" : null,
                    TargetAudience = a.TargetAudience
                })
                .ToListAsync();
        }

        private async Task<string> GenerateParentSummaryAsync(string studentName, List<Domain.Entities.TrialExam> trials)
        {
            if (!trials.Any())
                return $"{studentName} henüz sisteme herhangi bir deneme sınavı sonucu girmedi. Hedef belirleyip deneme sınavlarına başlamasını sağlayabilirsiniz.";

            var summaryData = string.Join("\n", trials.Select(t =>
                $"- {t.ExamDate.ToShortDateString()} ({t.ExamType}): Toplam {t.TotalScore} Net."
            ));

            var prompt = $@"
            Sen uzman bir YKS (TYT/AYT) danışmanı ve koçusun. 
            Aşağıdaki veriler öğrencinin son girdiği deneme sınavlarının toplam netlerini göstermektedir.
            Öğrencinin adı: {studentName}
            Sonuçlar:
            {summaryData}

            Görev: Veliyi bilgilendirmek amacıyla yukarıdaki sonuçları analiz et ve 2-3 cümlelik çok kısa, teşvik edici bir özet yaz.
            Örnek format: '{studentName} bu hafta hedeflerine yaklaştı. Matematik ağırlıklı deneme netlerinde artış gözlemliyoruz. Gelecek hafta Fen konularına biraz daha ağırlık vermesi iyi olabilir.'
            Lütfen teknik detaylara (şu deneme şu net yaptı diye tek tek saymaya) fazla girmeden veliyi rahatlatıcı ve yönlendirici bir özet yaz.
            ";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("Sen öğrencinin durumunu veliye aktaran empatik bir eğitim koçusun. Yalnızca istenilen 2-3 cümlelik özeti döndür."),
                new UserChatMessage(prompt)
            };

            try
            {
                var result = await _chatClient.CompleteChatAsync(messages);
                return result.Value.Content[0].Text.Trim();
            }
            catch (Exception)
            {
                // API kota hatası veya bağlantı problemi olursa mock veya default mesaj
                return $"{studentName}'in deneme sınavı sonuçları giderek şekilleniyor. Genel gelişimi takip edebilmek için düzenli deneme çözmeye devam etmesi önemli.";
            }
        }
        public async Task<List<StudyScheduleDto>> GetStudentScheduleAsync(int parentId)
        {
            var parent = await _db.Parents.FirstOrDefaultAsync(p => p.Id == parentId);
            if (parent == null)
                return new List<StudyScheduleDto>();

            var schedules = await _db.StudySchedules
                .Include(s => s.Subject)
                .Where(s => s.UserId == parent.StudentId && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var dtos = schedules.Select(s => new StudyScheduleDto
            {
                Id = s.Id,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject?.Name ?? "",
                SubjectShortName = s.Subject?.ShortName ?? "",
                SubjectColorHex = s.Subject?.ColorHex ?? "#ccc",
                DayOfWeek = s.DayOfWeek,
                DayName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((DayOfWeek)s.DayOfWeek),
                StartTime = s.StartTime.ToString("HH:mm"),
                EndTime = s.EndTime.ToString("HH:mm"),
                DurationMinutes = (int)(s.EndTime - s.StartTime).TotalMinutes,
                Topic = s.Topic,
                IsActive = s.IsActive
            }).ToList();

            return dtos;
        }
    }
}
