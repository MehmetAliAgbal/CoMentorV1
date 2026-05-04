using System.Threading.Tasks;
using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface IParentPanelService
    {
        Task<ParentDashboardDto> GetDashboardAsync(int parentId);
        Task<List<AnnouncementDto>> GetMessagesAsync(int parentId);
        Task<List<StudyScheduleDto>> GetStudentScheduleAsync(int parentId);
    }
}
