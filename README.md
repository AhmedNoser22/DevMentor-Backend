# DevMentor — Backend

An AI-driven platform for technical interview practice, timed exams, and verifiable
certificates in .NET, Angular, SQL, and System Design.

## Architecture

Clean Architecture, four layers, dependencies point inward only:

```
DevMentor.Domain          → Entities, enums, domain exceptions. No dependencies.
DevMentor.Application     → Use cases (Services), DTOs, validators, abstractions
                            (interfaces only: repositories, AI client, email, PDF).
DevMentor.Infrastructure  → Implements Application's interfaces: EF Core, Identity,
                            JWT, Gemini/Groq clients, QuestPDF, SMTP, OAuth.
DevMentor.API             → Controllers, middleware, DI wiring, Swagger.
```

`Application` never references `Infrastructure`. All repository interfaces
(`IQuestionRepository`, `IExamAttemptRepository`, `IInterviewSessionRepository`,
`ICertificateRepository`, `IUnitOfWork`) live in `Application/Abstractions/Persistence`
and are implemented in `Infrastructure/Persistence`.

Business rules live inside the entities themselves (`ExamAttempt.Submit()`,
`Question.Approve()`, `InterviewSession.ApplyEvaluationAndAdvance()`), not in the
services. Services only orchestrate: call repositories, call the AI client, wrap
transactions.

Shared AI response parsing (letter-to-index, lettered options, JSON options) lives in
`Infrastructure/Ai/AiResponseParser.cs`, so `GeminiAiClient` and `GroqAiClient` only
contain provider-specific connection details.

## Tech stack

- .NET 10 / ASP.NET Core
- EF Core + SQL Server
- ASP.NET Identity + JWT (access + refresh tokens)
- Google Gemini (primary) + Groq (fallback) for AI question generation and interview grading
- QuestPDF + QRCoder for certificate PDFs
- Serilog for logging
- FluentValidation for request validation
- xUnit + Moq for testing (in `/tests`, outside the four layers)

## Prerequisites

- .NET 10 SDK
- SQL Server (local or remote)
- A Gemini API key and a Groq API key
- A Google OAuth client and a GitHub OAuth app (see "External login" below)

## Setup

1. Clone the repo and restore:

   ```bash
   dotnet restore
   ```

2. Set secrets (never commit real secrets to `appsettings.json`):

   ```bash
   cd DevMentor.API
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:SigningKey" "a-long-random-string"
   dotnet user-secrets set "Ai:GeminiApiKey" "..."
   dotnet user-secrets set "Ai:GroqApiKey" "..."
   dotnet user-secrets set "OAuth:Google:ClientId" "..."
   dotnet user-secrets set "OAuth:Google:ClientSecret" "..."
   dotnet user-secrets set "OAuth:GitHub:ClientId" "..."
   dotnet user-secrets set "OAuth:GitHub:ClientSecret" "..."
   dotnet user-secrets set "Email:Username" "..."
   dotnet user-secrets set "Email:Password" "..."
   ```

3. Set the connection string in `appsettings.json` or via user-secrets, under
   `ConnectionStrings:Default`.

4. Run migrations:

   ```bash
   dotnet ef migrations add InitialCreate --project DevMentor.Infrastructure --startup-project DevMentor.API
   dotnet ef database update --project DevMentor.Infrastructure --startup-project DevMentor.API
   ```

5. Run the API:

   ```bash
   dotnet run --project DevMentor.API
   ```

   Swagger UI: `http://localhost:5102/swagger` (the port may differ, check `launchSettings.json`).

6. **Important: no HTTPS redirect in development.** The API does not force HTTPS
   redirection locally (only in production), because it broke CORS preflight
   requests from the Angular dev server. Do not re-enable
   `app.UseHttpsRedirection()` unconditionally.

## Seeding the question bank

Exams need `Approved` questions before anyone can start one. Generate them via
Swagger or curl, authenticated:

```http
POST /api/question-bank/generate
{
  "domain": 1,   // 1=DotNet, 2=Angular, 3=Sql, 4=SystemDesign
  "level": 1,    // 1=Beginner, 2=Intermediate, 3=Advanced
  "count": 20
}
```

The response `{ "generated": 20, "approved": 14 }` tells you how many actually
passed AI self-validation and became usable. Repeat per domain/level combination
you want to support. Check `logs/devmentor-*.log` if `approved` stays low: it logs
the raw AI response when parsing or validation fails.

## External login (Google / GitHub)

Redirect URIs registered with each provider must match exactly:

```
http://localhost:5102/api/auth/external/google/callback
http://localhost:5102/api/auth/external/github/callback
```

Swap in your production API domain when deploying. See the `OAuth` section in
`appsettings.json` for where credentials go.

## Key endpoints

| Area | Endpoint |
|---|---|
| Auth | `POST /api/auth/register`, `/login`, `/refresh`, `/confirm-email`, `/forgot-password`, `/reset-password` |
| External auth | `GET /api/auth/external/{google\|github}/login` |
| Question bank | `POST /api/question-bank/generate`, `GET /api/question-bank/by-status/{status}` |
| Exams | `POST /api/exams/start`, `POST /api/exams/answer`, `POST /api/exams/{id}/submit` |
| Interviews | `POST /api/interviews/start`, `POST /api/interviews/answer` |
| Certificates | `GET /api/certificates/mine`, `GET /api/certificates/{id}/pdf`, `GET /api/certificates/verify/{code}` |
| Profile | `GET /api/profile` |

## Testing

Tests live outside the four layers, in `/tests`, split by what they exercise:

```
tests/DevMentor.Domain.Tests        → entity rules, no mocking
tests/DevMentor.Application.Tests   → services, with Moq for repositories and the AI client
```

Run everything from the repo root:

```bash
dotnet test
```

Run a single project:

```bash
dotnet test tests/DevMentor.Domain.Tests/DevMentor.Domain.Tests.csproj
```

## Configuration reference

See `appsettings.json` for the full shape. Notable settings:

- `ExamSettings.PassPercentage`: score needed to earn a certificate (default 85).
- `ExamSettings.Levels`: question count and duration per level.
- `InterviewSettings.MaxTurns` / `DailyLimitPerUser`: interview session limits.
- `Ai.MaxRetries`: retries on the primary AI provider before falling back.
