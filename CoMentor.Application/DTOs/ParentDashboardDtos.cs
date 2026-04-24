using System;
using System.Collections.Generic;

namespace CoMentor.Application.DTOs
{
    public class ParentDashboardDto
    {
        public string ParentName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AISummary { get; set; } = string.Empty;
        public List<TrialExamProgressDto> TrialExams { get; set; } = new();
        public List<UpcomingExamDto> UpcomingExams { get; set; } = new();
    }

    public class TrialExamProgressDto
    {
        public int Id { get; set; }
        public string DateLabel { get; set; } = string.Empty; // e.g., "D1", "D2" or "12 Oca"
        public string ExamType { get; set; } = string.Empty; 
        public int TotalScore { get; set; } 
    }

    public class UpcomingExamDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
    }
}
