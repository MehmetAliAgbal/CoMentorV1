using System.Threading.Tasks;
using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface IParentPanelService
    {
        Task<ParentDashboardDto> GetDashboardAsync(int parentId);
        Task<List<AnnouncementDto>> GetMessagesAsync(int parentId);
        Task<List<StudyScheduleDto>> GetStudentScheduleAsync(int parentId);
        Task<TrialExamDto?> GetStudentTrialExamDetailAsync(int parentId, int trialId);
    }
}
