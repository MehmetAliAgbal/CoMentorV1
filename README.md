# CoMentor — Backend

This README contains quick developer setup for the backend so frontend devs can run and test the API locally.

## Prerequisites
- .NET 8 SDK
- PostgreSQL (or use the provided Docker Compose)
- dotnet-ef tool (for migrations): `dotnet tool install --global dotnet-ef`

## Quick start (local development)
1. Open a PowerShell terminal.

2. Go to the API project folder:
```powershell
cd C:\NewC\CoMentor\CoMentor.API
```

3. (Optional) Initialize user-secrets (only once per project):
```powershell
dotnet user-secrets init
```

4. Add your development secrets (do NOT commit these):
```powershell
# example (replace values)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=CoMentorDB;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Jwt:Key" "your_long_jwt_secret_here"
```

5. Run EF migrations (from solution root):
```powershell
cd C:\NewC\CoMentor
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef database update --project CoMentor.Infrastructure --startup-project CoMentor.API --context AppDbContext
```

6. Run the API
```powershell
cd C:\NewC\CoMentor\CoMentor.API
dotnet run
```

7. Open Swagger UI: `http://localhost:5107` (root set to swagger UI)

## Endpoints to test
- POST `/api/Auth/register` — register new user (see example JSON in Swagger)
- POST `/api/Auth/login` — login and receive JWT
- Use "Authorize" in Swagger UI (Bearer {token}) to test protected endpoints.

## CORS & Frontend
For local frontend dev, the API allows origins:
- http://localhost:3000 (React)
- http://localhost:5173 (Vite)
- http://localhost:4200 (Angular)

If your frontend runs on another origin, add it to `Program.cs` CORS policy.

## Quick curl example (register)
```bash
curl -X POST "http://localhost:5107/api/Auth/register" -H "Content-Type: application/json" -d '{
  "email":"test@example.com",
  "password":"P@ssw0rd",
  "name":"Test",
  "surname":"User"
}'
```

## Notes
- Do not commit secrets to git. Use user-secrets for development and environment variables / secret manager (Key Vault) in production.
- Swagger provides the OpenAPI document at `/swagger/v1/swagger.json`.

If you want, I can add a `docker-compose.yml` for Postgres and an example frontend stub next.

## If you prefer NOT to use dotnet user-secrets

You can avoid user-secrets by creating a local `appsettings.Development.json` file that contains your development connection string and JWT key. Do NOT commit this file.

1. Copy the example file shipped with the repo and edit it with your real values:

```powershell
cd C:\NewC\CoMentor\CoMentor.API
Copy-Item -Path .\appsettings.Development.json.example -Destination .\appsettings.Development.json
# then open and edit the new file to fill your DB and Jwt values
```

2. The repo includes `CoMentor.API/appsettings.Development.json.example` (placeholders). The real `appsettings.Development.json` is ignored by `.gitignore` so it won't be committed.

3. After creating that file, run the migrations and start the API as in the Quick start section.

Security reminder: committing real secrets to the repo is dangerous. Using a local `appsettings.Development.json` that is in `.gitignore` is more convenient than user-secrets but still keep the file private and do not share it in public channels.
