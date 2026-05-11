using System.Collections.Generic;
using System.Threading.Tasks;
using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface IAppointmentService
    {
        // Öğrenci/Veli tarafından randevu talep etme
        Task<AppointmentDto> RequestAppointmentAsync(int requesterId, string requesterType, RequestAppointmentDto request);
        
        // Öğretmen tarafından bir talebe tarih/saat atayıp randevulaştırma
        Task<AppointmentDto> ScheduleAppointmentAsync(int teacherId, int appointmentId, ScheduleAppointmentDto request);
        
        // Rol bazlı listelemeler
        Task<List<AppointmentDto>> GetTeacherAppointmentsAsync(int teacherId);
        Task<List<AppointmentDto>> GetStudentAppointmentsAsync(int studentId);
        
        // İptal İşlemi (Her iki taraf yapabilir, yetkiye göre controller kontrol eder)
        Task<bool> CancelAppointmentAsync(int appointmentId);
        
        // Öğretmenleri listeleme (Randevu almak için)
        Task<List<TeacherDto>> GetAvailableTeachersAsync();
    }
}
