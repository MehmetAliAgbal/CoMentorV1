using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Domain.Entities;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _db;

        public AppointmentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AppointmentDto> RequestAppointmentAsync(int requesterId, string requesterType, RequestAppointmentDto request)
        {
            int studentId = requesterId;

            if (requesterType == "Parent")
            {
                var parent = await _db.Parents.FindAsync(requesterId);
                if (parent == null) throw new Exception("Parent not found");
                studentId = parent.StudentId;
            }

            var appointment = new Appointment
            {
                TeacherId = request.TeacherId,
                StudentId = studentId,
                RequesterType = requesterType,
                Status = "Pending",
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            return await GetAppointmentDtoAsync(appointment.Id);
        }

        public async Task<AppointmentDto> ScheduleAppointmentAsync(int teacherId, int appointmentId, ScheduleAppointmentDto request)
        {
            var appointment = await _db.Appointments.FindAsync(appointmentId);
            
            if (appointment == null)
                throw new Exception("Randevu bulunamadı.");
                
            if (appointment.TeacherId != teacherId)
                throw new Exception("Bu randevuyu planlama yetkiniz yok.");

            appointment.AppointmentDate = request.AppointmentDate;
            appointment.StartTime = request.StartTime;
            appointment.EndTime = request.EndTime;
            appointment.TeacherNotes = request.TeacherNotes;
            appointment.MeetingUrl = request.MeetingUrl;
            appointment.Status = "Scheduled";
            appointment.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return await GetAppointmentDtoAsync(appointment.Id);
        }

        public async Task<List<AppointmentDto>> GetTeacherAppointmentsAsync(int teacherId)
        {
            return await _db.Appointments
                .Include(a => a.Teacher)
                .Include(a => a.Student)
                .Where(a => a.TeacherId == teacherId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        public async Task<List<AppointmentDto>> GetStudentAppointmentsAsync(int studentId)
        {
            return await _db.Appointments
                .Include(a => a.Teacher)
                .Include(a => a.Student)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId)
        {
            var appointment = await _db.Appointments.FindAsync(appointmentId);
            if (appointment == null) return false;

            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<TeacherDto>> GetAvailableTeachersAsync()
        {
            return await _db.Teachers
                .Select(t => new TeacherDto
                {
                    Id = t.Id,
                    Email = t.Email,
                    Name = t.Name,
                    Surname = t.Surname,
                    Branch = t.Branch,
                    AvatarUrl = t.AvatarUrl
                })
                .ToListAsync();
        }

        // Helper Map Method
        private async Task<AppointmentDto> GetAppointmentDtoAsync(int appointmentId)
        {
            var appointment = await _db.Appointments
                .Include(a => a.Teacher)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
            return MapToDto(appointment!);
        }

        private static AppointmentDto MapToDto(Appointment a)
        {
            return new AppointmentDto
            {
                Id = a.Id,
                TeacherId = a.TeacherId,
                TeacherName = $"{a.Teacher.Name} {a.Teacher.Surname}",
                StudentId = a.StudentId,
                StudentName = $"{a.Student.Name} {a.Student.Surname}",
                RequesterType = a.RequesterType,
                Status = a.Status,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Notes = a.Notes,
                TeacherNotes = a.TeacherNotes,
                MeetingUrl = a.MeetingUrl,
                CreatedAt = a.CreatedAt
            };
        }
    }
}
