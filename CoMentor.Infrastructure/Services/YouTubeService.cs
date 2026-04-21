using CoMentor.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace CoMentor.Infrastructure.Services
{
    public class YouTubeService : IYouTubeService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public YouTubeService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["YouTube:ApiKey"];
        }

        public async Task<(string Title, string Url)?> GetTopVideoAsync(string searchQuery)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                // API key yoksa güvenli çıkış, test amaçlı fallback de dönebilir.
                return null;
            }

            try
            {
                // YouTube Data API v3 Search endpoint
                var url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(searchQuery)}&maxResults=1&type=video&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    var firstItem = items[0];
                    var videoId = firstItem.GetProperty("id").GetProperty("videoId").GetString();
                    var title = firstItem.GetProperty("snippet").GetProperty("title").GetString();

                    if (!string.IsNullOrEmpty(videoId) && !string.IsNullOrEmpty(title))
                    {
                        // Basit temizleme
                        title = System.Net.WebUtility.HtmlDecode(title);
                        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
                        return (title, videoUrl);
                    }
                }

                return null;
            }
            catch
            {
                // Herhangi bir hatada (örneğin quota aşımı), API çökmemesi için null dönüyoruz
                return null;
            }
        }
    }
}
