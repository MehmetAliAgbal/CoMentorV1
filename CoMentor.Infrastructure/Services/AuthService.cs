using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;
using CoMentor.Application.Mappers;
using CoMentor.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CoMentor.Infrastructure.Services;
public class AuthService : IAuthService
{
    private readonly CoMentor.Infrastructure.Persistence.AppDbContext _db;
    private readonly IConfiguration _cfg;

    public AuthService(CoMentor.Infrastructure.Persistence.AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Name = request.Name,
            Surname = request.Surname,
            AvatarUrl = request.AvatarUrl,
            SchoolName = request.SchoolName,
            GradeLevel = request.GradeLevel,
            TargetExam = request.TargetExam,
            DailyGoalMinutes = request.DailyGoalMinutes ?? 120,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Defensive debug: ensure Surname is present on both request and entity before saving
        if (string.IsNullOrWhiteSpace(request.Surname) || string.IsNullOrWhiteSpace(user.Surname))
        {
            // Log diagnostic info to console (visible in terminal)
            Console.WriteLine("[AuthService] RegisterAsync: request.Surname='{0}'", request.Surname ?? "<null>");
            Console.WriteLine("[AuthService] RegisterAsync: user.Surname='{0}'", user.Surname ?? "<null>");
            throw new InvalidOperationException("Surname is missing on request or user object before saving to DB.");
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var (token, expires) = GenerateToken(user);
        return new AuthResponse { UserId = user.Id, Token = token, ExpiresAt = expires, User = user.ToDto() };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return null;
        if (!PasswordHasher.Verify(request.Password, user.PasswordHash)) return null;

        var (token, expires) = GenerateToken(user);
        return new AuthResponse { UserId = user.Id, Token = token, ExpiresAt = expires, User = user.ToDto() };
    }

    private (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt.GetValue<string>("Key") ?? throw new InvalidOperationException("Jwt:Key not configured");
        var issuer = jwt.GetValue<string>("Issuer") ?? "CoMentor";
        var audience = jwt.GetValue<string>("Audience") ?? "CoMentorClients";
        var expiresMinutes = jwt.GetValue<int?>("ExpiresMinutes") ?? 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name)
        };

        var sym = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(sym, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: expires, signingCredentials: creds);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenStr, expires);
    }
}