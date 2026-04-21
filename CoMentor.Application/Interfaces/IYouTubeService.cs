using System.Threading.Tasks;

namespace CoMentor.Application.Interfaces
{
    public interface IYouTubeService
    {
        Task<(string Title, string Url)?> GetTopVideoAsync(string searchQuery);
    }
}
