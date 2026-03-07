using System.Threading.Tasks;
using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces;

public interface IMlPredictionService
{
    Task<string> GetSayisalTahminAsync(SayisalTahminRequest requestData);
    Task<string> GetTytTahminAsync(TytTahminRequest requestData);
}
