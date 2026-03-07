using System.Collections.Generic;

namespace CoMentor.Application.DTOs;

public class SayisalDenemeDto
{
    public double Matematik { get; set; }
    public double Fen { get; set; }
}

public class SayisalTahminRequest
{
    public Dictionary<int, SayisalDenemeDto> Denemeler { get; set; } = new();
    public string Sinav_Tarihi { get; set; } = "21.06.2025";
}

public class TytDenemeDto
{
    public double Turkce { get; set; }
    public double Matematik { get; set; }
    public double Fen { get; set; }
    public double Sosyal { get; set; }
}

public class TytTahminRequest
{
    public Dictionary<int, TytDenemeDto> Denemeler { get; set; } = new();
    public string Sinav_Tarihi { get; set; } = "21.06.2025";
}
