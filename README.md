# DecorAI - AI-Powered Interior Design Platform

A modern microservices architecture built with **ASP.NET Core 10**, demonstrating service decomposition, Ocelot API Gateway, JWT authentication with refresh tokens, CQRS pattern with MediatR, and Anthropic Claude AI integration.

## Architecture Overview

```
                         ┌─────────────────────┐
                         │   Client / Angular   │
                         └──────────┬──────────┘
                                    │
                         ┌──────────▼──────────┐
                         │   Ocelot API Gateway │  (localhost:5162 / :7009)
                         │     (GatewayApi)     │
                         └──────┬───────┬───────┘
                                │       │
                ┌───────────────▼─┐   ┌─▼──────────────┐
                │   UserAuthApi   │   │    UserApi       │
                │  (JWT Auth)     │   │  (Business CRUD  │
                │  :5210          │   │   + AI Design)   │
                └───────┬─────────┘   │  :5262           │
                        │             └──────┬──────────┘
                ┌───────▼────────────────────▼──────────┐
                │         Infrastructure / Domain        │
                │     (Clean Architecture Layers)        │
                └───────────────────────────────────────┘
```

## Services

| Service | Responsibility | Port |
|---|---|---|
| `GatewayApi` | Ocelot API Gateway — routes all requests to downstream services | 5162 / 7009 |
| `UserAuthApi` | Handles registration, login, JWT token generation & refresh | 5210 |
| `UserApi` | User management, room CRUD, chat, AI design generation | 5262 |
| `UserDomain` | Shared domain entities, interfaces, exceptions | — |
| `Infrastructure` | EF Core DbContext (SQL Server), repositories, JWT & Anthropic services | — |
| `Application` | CQRS layer with MediatR handlers, DTOs, requests/responses | — |

## Tech Stack

- **Framework:** ASP.NET Core (.NET 10)
- **API Gateway:** Ocelot 24.1.0
- **Authentication:** JWT Bearer (20min expiry) + Refresh Tokens (7 days, DB-stored, revocable)
- **Database:** SQL Server via Entity Framework Core 10
- **ORM:** Entity Framework Core 10
- **API Design:** RESTful with MediatR CQRS pattern
- **AI:** Anthropic Claude API (Claude Sonnet 4-6) with image content block support
- **Caching:** Redis (optional, configured per service)

## Key Features

- ✅ Ocelot API Gateway — centralized routing for all microservices
- ✅ JWT Authentication + Refresh Token rotation (single-use, revocable)
- ✅ Clean Architecture — domain, infrastructure, and application layers separated
- ✅ CQRS Pattern — MediatR for command/query separation
- ✅ Generic Repository — reusable CRUD operations for all entities
- ✅ Full CRUD Operations — for all 10 domain entities
- ✅ AI Design Generation — Claude API with prompt templates from DB
- ✅ Image Processing — base64 image support for Claude Messages API
- ✅ 3 Room Types — HomeSingle, HomeDouble (2 rooms), Event Marquee (with ceiling type)
- ✅ Exception Handling — custom middleware with typed HTTP exceptions
- ✅ DB Auto Migration + Seed — runs on startup, seeds 3 default prompts
- ✅ Redis Caching — middleware available (disabled by default)

## Room Types

| Type | Description | Extra Fields |
|---|---|---|
| **HomeSingle** | Single room in a home | RoomName, Length, Width, Height, Area (optional) |
| **HomeDouble** | Two rooms in a home (linked via RoomGroupId) | Room1 + Room2 fields |
| **Marquee** | Event marquee space | CeilingType (Flat, Vaulted, Dome, etc.) |

## Authentication Flow

```
1. POST /api/auth/register    → Create new user
2. POST /api/auth/login       → Returns JWT (20min) + RefreshToken (7 days)
3. POST /api/auth/refresh     → Exchange expired JWT + refresh token for new pair
4. POST /api/auth/revoke      → Revoke all refresh tokens (requires JWT)
```

**Authorization Header:** `Authorization: Bearer <jwt_token>`

## API Endpoints

All endpoints accessible through the API Gateway at `http://localhost:5162` or `https://localhost:7009`

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | No | Register new user |
| POST | `/api/auth/login` | No | Login, returns JWT + refresh token |
| POST | `/api/auth/refresh` | No | Refresh JWT using refresh token |
| POST | `/api/auth/revoke` | JWT | Revoke all refresh tokens |

### User
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/user/profile` | JWT | Get user profile from JWT claims |
| PATCH | `/api/user/profile` | JWT | Update user profile |

### Design (Core AI Feature)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/design/generate` | JWT | Generate AI design (multipart, ~15MB limit) |

### Contexts (Prompts)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/contexts` | No | List all prompt contexts |
| GET | `/api/contexts/{id}` | No | Get context by ID |
| GET | `/api/contexts/type/{type}` | No | Get context by room type (home-single, home-double, marquee) |
| POST | `/api/contexts` | No | Create context |
| PUT | `/api/contexts/{id}` | No | Update context |
| DELETE | `/api/contexts/{id}` | No | Delete context |
| POST | `/api/contexts/ask` | No | Send raw context to Claude AI |

### CRUD Endpoints (All Entities)
Rooms, Chats, ChatMessages, Images, AIResults, Logs, ApiKeys, Admins all have standard CRUD:
- `GET /api/{entity}` — List all
- `GET /api/{entity}/{id}` — Get by ID
- `GET /api/{entity}/user/{userId}` or `/room/{roomId}` or `/chat/{chatId}` — Filtered lookups
- `POST /api/{entity}` — Create
- `PUT /api/{entity}/{id}` — Update
- `DELETE /api/{entity}/{id}` — Delete

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────┐
│              Presentation Layer                  │
│  UserApi / UserAuthApi / GatewayApi (Web APIs)   │
│  Controllers, Middleware, Program.cs             │
├─────────────────────────────────────────────────┤
│              Application Layer                   │
│  Application (MediatR CQRS pattern)              │
│  Request/Response DTOs, Handlers                 │
├─────────────────────────────────────────────────┤
│              Domain Layer                        │
│  UserDomain (Entities, Interfaces, Exceptions)   │
│  Pure domain logic, no external dependencies      │
├─────────────────────────────────────────────────┤
│              Infrastructure Layer                │
│  Infrastructure (EF Core, Repositories, JWT)     │
│  Data access, external service implementations   │
└─────────────────────────────────────────────────┘
```

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB works for development)
- Anthropic Claude API key

### Run the Solution
```bash
# Open solution
start DecorAI.sln

# Set multiple startup projects:
#   1. GatewayApi (https profile)
#   2. UserAuthApi (http profile)
#   3. UserApi (http profile)

# Or run from CLI (3 terminals):
dotnet run --project GatewayApi
dotnet run --project UserAuthApi
dotnet run --project UserApi
```

### Configuration
Set these in `appsettings.Development.json` for each project:
- `ConnectionStrings:UserDbConnection` — SQL Server connection string
- `Jwt:Secret` — At least 32 characters
- `Anthropic:ApiKey` — Your Claude API key (in UserApi only)

Database auto-creates and seeds on first startup. No manual migration steps needed.

## Project Structure

```
DecorAI/
├── GatewayApi/              # Ocelot gateway configuration & routing
│   ├── ocelot.json          # Route definitions
│   └── Program.cs           # Startup with Redis (optional)
├── UserAuthApi/             # Authentication service
│   ├── Controllers/         # AuthApiController (register, login, refresh, revoke)
│   └── Middleware/          # ExceptionMiddleware, RedisCacheMiddleware (unused)
├── UserApi/                 # Main business service
│   ├── Controllers/         # All CRUD controllers + DesignController
│   └── Middleware/          # ExceptionMiddleware, RedisCacheMiddleware (unused)
├── UserDomain/              # Domain entities, interfaces, exceptions
├── Infrastructure/          # EF Core DbContext, repositories, services
│   ├── Repositories/        # Generic + entity-specific repositories
│   ├── Services/            # JWTTokenProvider, AnthropicService
│   └── Migrations/          # EF Core migrations
├── Application/             # CQRS handlers, DTOs, requests/responses
│   ├── Handler/             # MediatR command/query handlers
│   ├── Request/             # IRequest<TResponse> classes
│   ├── Response/            # Standalone response DTOs
│   └── DTO/                 # Data transfer objects
├── DecorAI.sln              # Solution file
└── Test-Updated.postman_collection.json  # Postman test collection
```

## Testing with Postman

A Postman collection is included at `Test-Updated.postman_collection.json`.
Import it into Postman to test all endpoints.

## Database Migration

Migrations auto-apply on startup. To manually manage:
```bash
cd Infrastructure
dotnet ef migrations add MigrationName --startup-project ../UserApi
dotnet ef database update --startup-project ../UserApi
```

## Seed Data

On first startup, the following prompt contexts are auto-seeded:

| Type | Purpose |
|---|---|
| `home-single` | Design a single room (color, furniture, lighting) |
| `home-double` | Design two rooms with cohesive style |
| `marquee` | Design event marquee with ceiling decor |

## Future Improvements

- [ ] Docker & docker-compose support
- [ ] Message bus for async service communication
- [ ] Health check endpoints
- [ ] Unit tests with xUnit
- [ ] Rate limiting via Ocelot
- [ ] Gateway-level JWT validation
- [ ] Enable Redis caching

## Author

**Usama Ishtiaq** — Backend .NET Engineer
- [LinkedIn](https://www.linkedin.com/in/usama-ishtiaq-094881156/)
- [GitHub](https://github.com/UsamaIshtiaq95)
