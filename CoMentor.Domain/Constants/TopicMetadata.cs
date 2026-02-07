using System.Collections.Generic;

namespace CoMentor.Domain.Constants;

public static class TopicMetadata
{
    // Konu Adı -> (Tahmini Saat, Zorluk 1-5)
    public static readonly Dictionary<string, (int Hours, int Difficulty)> TopicDetails = new()
    {
        // Türkçe
        { "Sözcükte Anlam", (2, 1) },
        { "Cümlede Anlam", (3, 2) },
        { "Paragraf", (5, 3) },
        { "Ses Bilgisi", (3, 2) },
        { "Yazım Kuralları", (4, 3) },
        { "Noktalama İşaretleri", (3, 2) },
        { "Sözcük Türleri", (8, 3) },
        
        // Matematik
        { "Temel Kavramlar", (8, 2) },
        { "Rasyonel Sayılar", (5, 2) },
        { "Üslü Sayılar", (5, 3) },
        { "Köklü Sayılar", (5, 3) },
        { "Problemler", (30, 4) },
        { "Fonksiyonlar", (12, 4) },
        { "Polinomlar", (5, 3) },
        { "Limit", (4, 4) },
        { "Türev", (8, 5) },
        { "İntegral", (10, 5) },
        { "Trigonometri", (8, 4) },
        { "Analitik Geometri", (10, 4) },

        // Fen
        { "Fizik Bilimine Giriş", (4, 1) },
        { "Kuvvet ve Hareket", (8, 3) },
        { "Elektrik", (8, 4) },
        { "Atom ve Periyodik Sistem", (6, 2) },
        { "Hücre", (8, 3) },
        { "Kalıtım", (8, 4) }
    };

    public static int GetDuration(string topic) 
    {
        // Eşleşme yoksa varsayılan 3 saat
        return TopicDetails.ContainsKey(topic) ? TopicDetails[topic].Hours : 3;
    }
}
