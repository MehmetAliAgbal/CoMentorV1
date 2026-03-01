using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services;

public class TeacherAuthService : ITeacherAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public TeacherAuthService(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<TeacherAuthResponse?> RegisterAsync(TeacherRegisterRequest request)
    {
        if (await _db.Teachers.AnyAsync(t => t.Email == request.Email))
            return null; // Teacher already exists

        var teacher = new Teacher
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Name = request.Name,
            Surname = request.Surname,
            Branch = request.Branch,
            AvatarUrl = request.AvatarUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var (token, expires) = GenerateTeacherToken(teacher);
        
        return new TeacherAuthResponse 
        { 
            TeacherId = teacher.Id, 
            Token = token, 
            ExpiresAt = expires, 
            Teacher = new TeacherDto
            {
                Id = teacher.Id,
                Email = teacher.Email,
                Name = teacher.Name,
                Surname = teacher.Surname,
                Branch = teacher.Branch,
                AvatarUrl = teacher.AvatarUrl
            }
        };
    }

    public async Task<TeacherAuthResponse?> LoginAsync(TeacherLoginRequest request)
    {
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Email == request.Email);
        if (teacher == null) return null;
        if (!PasswordHasher.Verify(request.Password, teacher.PasswordHash)) return null;

        var (token, expires) = GenerateTeacherToken(teacher);
        
        return new TeacherAuthResponse 
        { 
            TeacherId = teacher.Id, 
            Token = token, 
            ExpiresAt = expires, 
            Teacher = new TeacherDto
            {
                Id = teacher.Id,
                Email = teacher.Email,
                Name = teacher.Name,
                Surname = teacher.Surname,
                Branch = teacher.Branch,
                AvatarUrl = teacher.AvatarUrl
            }
        };
    }

    private (string token, DateTime expiresAt) GenerateTeacherToken(Teacher teacher)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt.GetValue<string>("Key") ?? throw new InvalidOperationException("Jwt:Key not configured");
        var issuer = jwt.GetValue<string>("Issuer") ?? "CoMentor";
        var audience = jwt.GetValue<string>("Audience") ?? "CoMentorClients";
        var expiresMinutes = jwt.GetValue<int?>("ExpiresMinutes") ?? 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, teacher.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, teacher.Email),
            new Claim("name", teacher.Name),
            new Claim(ClaimTypes.Role, "Teacher") // Bu en önemli kısım: Yetkilendirme için Rol
        };

        var sym = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(sym, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: expires, signingCredentials: creds);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenStr, expires);
    }
}
