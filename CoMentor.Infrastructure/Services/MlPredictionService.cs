using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;

namespace CoMentor.Infrastructure.Services;

public class MlPredictionService : IMlPredictionService
{
    private readonly HttpClient _httpClient;
    private const string PythonApiBaseUrl = "http://localhost:8000";

    public MlPredictionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetSayisalTahminAsync(SayisalTahminRequest requestData)
    {
        try
        {
            string url = $"{PythonApiBaseUrl}/api/v1/tahmin/ayt-sayisal";

            var jsonContent = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sayısal Tahmin servisi hatası: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GetTytTahminAsync(TytTahminRequest requestData)
    {
        try
        {
            string url = $"{PythonApiBaseUrl}/api/v1/tahmin/tyt";

            var jsonContent = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TYT Tahmin servisi hatası: {ex.Message}");
            return null;
        }
    }
}
