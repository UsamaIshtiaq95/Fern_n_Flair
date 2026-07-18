# DecorAI Backend — Initial Context for AI Assistants

This document provides a complete architectural overview of the DecorAI backend (`Fern_n_Flair`). Any AI assistant should read this file FIRST to understand the project before making changes.

## 1. Project Overview

An **AI-Powered Interior Design Platform** built with a microservices architecture. Users describe rooms (dimensions, style, photos) and the system calls **Anthropic Claude** to generate design suggestions. The backend follows **Clean Architecture** with **CQRS via MediatR**.

**Tech Stack:** .NET 10, ASP.NET Core, Ocelot API Gateway, EF Core, SQL Server, MediatR, JWT Bearer, Anthropic Claude API

---

## 2. Solution Structure (6 Projects)

```
DecorAI.sln
├── GatewayApi/          — Ocelot API Gateway (reverse proxy)
├── UserAuthApi/         — Authentication service (register/login/refresh)
├── UserApi/             — Main business service (all CRUD + AI design)
├── Application/         — CQRS layer (MediatR handlers, DTOs, requests/responses)
├── Infrastructure/      — Data access (EF Core DbContext, repositories, external services)
└── UserDomain/          — Domain layer (entities, interfaces, exceptions)
```

**Dependency Chain:**
```
API Projects (GatewayApi, UserAuthApi, UserApi)
    └── Application (handlers, DTOs)
            └── Infrastructure (DbContext, repos, services)
                    └── UserDomain (entities, interfaces, exceptions)
```

---

## 3. Service Ports & Gateway Routing

| Service | Port(s) | Launch URL |
|---|---|---|
| **GatewayApi** | `http://localhost:5162` / `https://localhost:7009` | `/swagger` |
| **UserAuthApi** | `http://localhost:5210` / `https://localhost:7261` | `/swagger` |
| **UserApi** | `http://localhost:5262` | `/swagger` |

**IMPORTANT:** All public traffic goes through the Gateway (`:5162` / `:7009`). Downstream services (`:5210`, `:5262`) are internal.

### Ocelot Routes (ocelot.json)

| Upstream Path | Downstream | Methods |
|---|---|---|
| `/api/auth/{everything}` | `localhost:5210/api/auth/{everything}` | POST, GET |
| `/api/rooms/{everything}` | `localhost:5262/api/rooms/{everything}` | GET, POST, PUT, DELETE |
| `/api/chats/{everything}` | `localhost:5262/api/chats/{everything}` | GET, POST, PUT, DELETE |
| `/api/chatmessages/{everything}` | `localhost:5262/api/chatmessages/{everything}` | GET, POST, PUT, DELETE |
| `/api/design/{everything}` | `localhost:5262/api/design/{everything}` | POST, GET |
| `/api/images/{everything}` | `localhost:5262/api/images/{everything}` | GET, POST, PUT, DELETE |
| `/api/airesults/{everything}` | `localhost:5262/api/airesults/{everything}` | GET, POST, PUT, DELETE |
| `/api/contexts/{everything}` | `localhost:5262/api/contexts/{everything}` | GET, POST, PUT, DELETE |
| `/api/logs/{everything}` | `localhost:5262/api/logs/{everything}` | GET, POST, PUT, DELETE |
| `/api/apikeys/{everything}` | `localhost:5262/api/apikeys/{everything}` | GET, POST, PUT, DELETE |
| `/api/admins/{everything}` | `localhost:5262/api/admins/{everything}` | GET, POST, PUT, DELETE |

---

## 4. Authentication Flow

### Register & Login
```
POST /api/auth/register  →  Creates user, returns success message
POST /api/auth/login     →  Validates credentials, returns JWT + RefreshToken
```

### JWT Configuration
- **Expiry:** 20 minutes (configurable via `Jwt:ExpirationInMinutes`)
- **Issuer:** `AuthApi`
- **Audience:** `UserApi`
- **Algorithm:** HMAC-SHA256
- **Claims:** `sub` (UserId), `email`, `name`, `jti` (unique token ID)

### Token Refresh
```
POST /api/auth/refresh   →  Accepts { Token, RefreshToken }, returns new pair
POST /api/auth/revoke    →  Revokes all refresh tokens for authenticated user (JWT required)
```
- Refresh tokens expire after **7 days** (configurable via `Jwt:RefreshTokenExpirationDays`)
- Refresh tokens are **single-use** (marked `IsUsed = true` after consumption)
- Refresh tokens can be **revoked** (e.g., on logout/password change)
- Stored in `RefreshTokens` table with FK to `Users`

### Frontend Token Flow
1. Login returns `{ token, refreshToken, message }`
2. Store both in localStorage (`auth_token` and `refresh_token`)
3. Attach `Authorization: Bearer {token}` header via HTTP interceptor
4. On **401 Unauthorized**: interceptor calls `/api/auth/refresh`, retries original request
5. On refresh failure: redirect to login

---

## 5. Database Schema (SQL Server)

### Entity Relationship Diagram
```
Users ──1:N──> Chats ──1:N──> ChatMessages ──1:1──> AIResults
  │              │  └──N:1──< Rooms
  │              └────N:1──< Contexts
  └──1:N──> Logs
  └──1:N──> RefreshTokens
Images ──N:1──> Rooms/Chats/ChatMessages
Admins (standalone)
ApiKeys (standalone)
```

### 10+1 Entities

| Entity | PK | Key Notes |
|---|---|---|
| **Users** | `UserId` | Unique Email, soft-delete via `IsActive` |
| **Rooms** | `RoomId` | `RoomType` (HomeSingle/HomeDouble/Marquee), `CeilingType` (nullable, for marquee), `RoomGroupId` (links rooms in HomeDouble), soft-delete via `IsDeleted` |
| **Contexts** | `ContextId` | Stores AI prompt templates. `Type` field: "home-single", "home-double", "marquee". `SourceAI`: "claude" |
| **Chats** | `ChatId` | FK → User, Room, Context. Soft-delete via `IsDeleted` |
| **ChatMessages** | `MessageId` | `Sender`: "user" or "ai". Soft-delete via `IsDeleted` |
| **AIResults** | `ResultId` | Stores full AI response, tracks tokens/usage/cost |
| **Images** | `ImageId` | FK → Room, Chat, Message (all nullable). Stores metadata only; actual files in `uploads/{chatId}/` |
| **Logs** | `LogId` | Audit trail, FK → User (nullable) |
| **ApiKeys** | `ApiKeyId` | External service keys, unique `ServiceName` |
| **Admins** | `AdminId` | Separate admin accounts |
| **RefreshToken** | `TokenId` | FK → User. Fields: Token, JwtId, IsUsed, IsRevoked, ExpiresAt |

### Soft Delete Pattern
Entities `Users`, `Rooms`, `Chats`, `ChatMessages` use **soft delete** via `IsDeleted` / `IsActive` with EF Core global query filters (`HasQueryFilter`). All queries automatically exclude deleted records.

---

## 6. Room Types & Design Generation Flow

### Room Types
| Type | Description | Behavior |
|---|---|---|
| **HomeSingle** | Single room in a home | One room record created |
| **HomeDouble** | Two rooms in a home | Two room records linked via `RoomGroupId` |
| **Marquee** | Event marquee | One room record with `CeilingType` set |

### Design Generation (`POST /api/design/generate`)
```
1. Extract userId from JWT
2. Determine room type from request
3. Load prompt from Contexts table by room type (e.g., "home-single")
4. Create Room(s) with dimensions, type, style
5. Create Chat linked to primary room
6. Save uploaded images to disk (uploads/{chatId}/) + DB metadata
7. Save user ChatMessage with room summary
8. Build AI context:
   - Fill prompt placeholders ({{RoomName}}, {{Length}}, {{Style}}, etc.)
   - Base64-encode uploaded images
9. Call Anthropic Claude Messages API (text + image content blocks)
10. Save AI response as ChatMessage + AIResult
11. Return { chatId, rooms: [...], message }
```

### Prompt Placeholder System
Prompts stored in `Contexts.ContextData` use these placeholders:

| Placeholder | Description | Used For |
|---|---|---|
| `{{RoomName}}` | Room name | All types |
| `{{Length}}` | Room length | All types |
| `{{Width}}` | Room width | All types |
| `{{Height}}` | Room height | All types |
| `{{Area}}` | Room area (optional) | All types |
| `{{Unit}}` | Measurement unit (ft/m) | All types |
| `{{Style}}` | Design style (modern, etc.) | All types |
| `{{RoomName2}}` | Second room name | HomeDouble only |
| `{{Length2}}`, `{{Width2}}`, `{{Height2}}`, `{{Area2}}` | Second room dimensions | HomeDouble only |
| `{{CeilingType}}` | Ceiling type | Marquee only |

### 3 Default Seed Prompts
Auto-seeded on first run via `DbSeeder.SeedAsync()`:
- **home-single**: Room design with color palette, furniture, lighting
- **home-double**: Cohesive design for two rooms
- **marquee**: Event layout with seating, lighting, ceiling decor

---

## 7. CQRS Pattern (MediatR)

Every API operation follows this pattern:
```
Controller → IMediator.Send(Request) → Handler.Handle(Request) → Repository → Response
```

**Handler conventions:**
- Each entity has ~5-7 handlers (Create, GetAll, GetById, Update, Delete + specialized lookups)
- Handlers are in `Application.Handler` namespace (EXCEPT `LoginHandler`, `RegisterHandler`, `UpdateUserDetailHandler` which are in global namespace — legacy inconsistency)
- Handlers inject repository interfaces from `UserDomain.Interface`
- Handlers throw custom exceptions (`NotFoundException`, `BadRequestException`, etc.) caught by global `ExceptionMiddleware`

**Auth-specific handlers:**
- `LoginHandler`: Verifies password → creates JWT + RefreshToken → returns both
- `RegisterHandler`: Checks email uniqueness → hashes password → creates user
- `RefreshTokenHandler`: Validates stored token → marks used → creates new JWT + refresh token
- `RevokeTokenHandler`: Revokes all active tokens for user

---

## 8. AI Integration (Anthropic Claude)

### Service: `AnthropicService`
- **Endpoint:** `https://api.anthropic.com/v1/messages`
- **Model:** Configurable via `Anthropic:Model` (default: `claude-sonnet-4-6`)
- **Version:** `2023-06-01`
- **Max tokens:** 2048

### Image Support
Claude's Messages API supports content blocks. The `AnthropicService` builds structured messages:
```json
{
  "role": "user",
  "content": [
    {"type": "text", "text": "Design a room..."},
    {"type": "image", "source": {"type": "base64", "media_type": "image/jpeg", "data": "..."}}
  ]
}
```
- Images are loaded from disk, base64-encoded
- Supported formats: JPEG, PNG, WEBP, GIF
- `ImageContent` class carries `FileName`, `Base64Data`, `MediaType`

**Configuration needed:** Set `Anthropic:ApiKey` in `appsettings.Development.json`

---

## 9. Exception Handling

Custom exception hierarchy in `UserDomain`:
```
AppException (base) ── StatusCode property
├── NotFoundException         → 404
├── UnauthorizedException     → 401
├── BadRequestException       → 400
├── InternalServerException   → 500
└── DatabaseUnavailableException → 503
```

Caught by `ExceptionMiddleware` (registered in both `UserAuthApi` and `UserApi`):
- AppException: returns `{ error: message }` with the exception's `StatusCode`
- All other exceptions: returns `{ error: "An unexpected error occurred." }` with 500

---

## 10. Configuration (appsettings.json)

### Required Settings (Development)
```json
{
  "ConnectionStrings": {
    "UserDbConnection": "Server=(localdb)\\MSSQLLocalDB;Database=DecorAI;..."
  },
  "Jwt": {
    "Secret": "at-least-32-characters-long-secret-key!!",
    "Issuer": "AuthApi",
    "Audience": "UserApi",
    "ExpirationInMinutes": 20,
    "RefreshTokenExpirationDays": 7
  },
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-sonnet-4-6"
  }
}
```

### Gateway-Specific
```json
{
  "Redis": {
    "ConnectionString": "" // Optional, leave empty to disable caching
  }
}
```

---

## 11. Running the Project

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB works for development)
- Visual Studio 2022 / VS Code
- Anthropic Claude API key

### Steps
```bash
# 1. Navigate to solution
cd Fern_n_Flair

# 2. Set Anthropic API key in UserApi/appsettings.Development.json

# 3. Run with multiple startup projects:
#    - GatewayApi (https profile: :7009 / :5162)
#    - UserAuthApi (http profile: :5210)
#    - UserApi (http profile: :5262)

# OR from CLI (in separate terminals):
dotnet run --project GatewayApi
dotnet run --project UserAuthApi
dotnet run --project UserApi

# 4. Database auto-creates + seeds on first startup
#    (EF Core MigrateAsync + DbSeeder.SeedAsync run automatically)
```

### Adding a Migration
```bash
dotnet ef migrations add MigrationName --startup-project ../UserApi
```

---

## 12. Known Issues & Technical Debt

### Critical
- **Redis cache middleware** exists (commented out) in all services — not active
- Most CRUD endpoints have **no JWT authorization** (only `/api/user/profile`, `/api/user/profile/update`, `/api/design/generate` require auth)
- **`auth.interceptor.ts`** in frontend exists but is **not registered** in `app.module.ts` providers

### Minor
- `UserRepository` does NOT extend generic `Repository<T>` (unlike all other repositories)
- `LoginHandler`, `RegisterHandler`, `UpdateUserDetailHandler` are in global namespace (not `Application.Handler`)
- `ExceptionMiddlware.cs` filename has a typo ("Middlware") in both UserAuthApi and UserApi
- No EF Core decimal precision configured for `Rooms` entity (Length, Width, Height, Area)
- `WeatherForecast.cs` and `.http` test files are unused boilerplate remnants

---

## 13. Key Interface Contracts

### Generic Repository (`IRepository<T>`)
```csharp
Task<T> GetByIdAsync(int id)
Task<IEnumerable<T>> GetAllAsync()
Task<IEnumerable<T>> GetAllAsync(int skip, int take)
Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
Task<T> AddAsync(T entity)
Task UpdateAsync(T entity)
Task DeleteAsync(int id)
Task<bool> ExistsAsync(int id)
Task<int> CountAsync()
IQueryable<T> Query()
Task<int> SaveChangesAsync(CancellationToken ct = default)
```

### ITokenService
```csharp
string CreateToken(Users user, out string jwtId)
string GenerateRefreshToken()
int GetJwtExpirationMinutes()
int GetRefreshTokenExpirationDays()
```

### IAnthropicService
```csharp
Task<string> SendContextAndGetRawResponseAsync(IList<MessageDto> context, CancellationToken ct = default)
```

---

## 14. File Map (All Source Files)

```
UserDomain/
├── Entities/        Admins.cs, AIResults.cs, ApiKeys.cs, ChatMessages.cs,
│                    Chats.cs, Contexts.cs, Images.cs, Logs.cs,
│                    RefreshToken.cs, Rooms.cs, Users.cs
├── Interface/       IAdminRepository.cs, IAIResultRepository.cs,
│                    IAnthropicService.cs, IApiKeyRepository.cs,
│                    IChatMessageRepository.cs, IChatRepository.cs,
│                    IContextRepository.cs, IImageRepository.cs,
│                    ILogRepository.cs, IRefreshTokenRepository.cs,
│                    IRepository.cs, IRoomRepository.cs,
│                    ITokenService.cs, IUserRepository.cs
├── Class1.cs        MessageDto + ImageContent
├── CustomExceptions.cs
├── Exceptions.cs
└── UserDomain.csproj

Application/
├── DTO/             AdminDtos.cs, AIResultDtos.cs, ApiKeyDtos.cs,
│                    AuthDtos.cs, ChatDtos.cs, ChatMessageDtos.cs,
│                    ContextDtos.cs, ImageDtos.cs, Login.cs,
│                    LogDtos.cs, Register.cs, RoomDtos.cs, UpdateDetails.cs
├── Handler/         AdminHandlers.cs, AIResultHandlers.cs,
│                    ApiKeyHandlers.cs, AuthHandlers.cs,
│                    ChatHandlers.cs, ChatMessageHandlers.cs,
│                    ContextHandlers.cs, ImageHandlers.cs,
│                    LogHandlers.cs, LoginHandler.cs,
│                    RegisterHandler.cs, RoomHandlers.cs,
│                    UpdateUserDetailhandler.cs
├── Request/         AdminRequests.cs, AIResultRequests.cs,
│                    ApiKeyRequests.cs, AuthRequests.cs,
│                    ChatMessageRequests.cs, ChatRequests.cs,
│                    ContextRequests.cs, ImageRequests.cs,
│                    LoginRequest.cs, LogRequests.cs,
│                    RegisterRequest.cs, RoomRequests.cs,
│                    UpdateUserDetailRequest.cs
├── Response/        LoginResponse.cs, RegisterResponse.cs,
│                    UpdateUserDetailResponse.cs
└── Application.csproj

Infrastructure/
├── Migrations/
├── Repositories/    AdminRepository.cs, AIResultRepository.cs,
│                    ApiKeyRepository.cs, ChatMessageRepository.cs,
│                    ChatRepository.cs, ContextRepository.cs,
│                    ImageRepository.cs, LogRepository.cs,
│                    RefreshTokenRepository.cs, Repository.cs,
│                    RoomRepository.cs, UserRepository.cs
├── Services/        AnthropicService.cs, JWTTokenProvider.cs
├── AppDbContext.cs
├── DbSeeder.cs
└── Infrastructure.csproj

GatewayApi/
├── Middleware/       RedisCacheMiddleware.cs (commented out)
├── Program.cs
├── ocelot.json
└── GatewayApi.csproj

UserAuthApi/
├── Controllers/     AuthApiController.cs
├── Middleware/       ExceptionMiddlware.cs, RedisCacheMiddleware.cs (unused)
├── Program.cs
└── UserAuthApi.csproj

UserApi/
├── Controllers/     AdminsController.cs, AIResultsController.cs,
│                    ApiKeysController.cs, BaseController.cs,
│                    ChatMessagesController.cs, ChatsController.cs,
│                    ContextsController.cs, DesignController.cs,
│                    ImagesController.cs, LogsController.cs,
│                    RoomsController.cs, UserApiController.cs
├── Middleware/       ExceptionMiddlware.cs, RedisCacheMiddleware.cs (unused)
├── Program.cs
└── UserApi.csproj
```

**End of context document.** Any AI assistant should now have sufficient understanding to make informed changes to this project.
