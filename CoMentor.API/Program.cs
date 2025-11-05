using CoMentor.Infrastructure.Persistence;
using CoMentor.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using CoMentor.Infrastructure.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Connection string al (appsettings.json i�inden)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// DbContext'i ekle - PostgreSQL kullanımı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// register auth service (IAuthService implemented in Infrastructure)
builder.Services.AddScoped<IAuthService, AuthService>();

// CORS - allow frontend dev origins (adjust as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT konfigürasyonu (appsettings.json'da "Jwt" bölümü olmalı)
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
}

// Di�er servis kay�tlar� (�rnek)
builder.Services.AddScoped<IUserStatsService, UserStatsService>();

// Controller ve Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // var existing config...
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CoMentor API", Version = "v1" });

    // JWT (Bearer) auth için Swagger tanımı
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoMentor API v1");
    c.RoutePrefix = string.Empty; // Swagger ana sayfa olarak açılması için (opsiyonel)
});

// use CORS (must be before endpoints)
app.UseCors("DevCors");

app.UseHttpsRedirection();

// authentication middleware must run before Authorization
app.UseAuthentication();
app.UseAuthorization();

// healthchecks
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
