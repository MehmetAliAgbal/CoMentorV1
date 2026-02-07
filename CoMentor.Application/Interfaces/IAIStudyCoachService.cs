using CoMentor.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoMentor.Application.Interfaces;

public interface IAIStudyCoachService
{
    Task<List<StudyScheduleDto>> GenerateScheduleAsync(int userId, GenerateScheduleRequestDto request);
}
