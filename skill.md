# Project Overview – DecorAI Fern_n_Flair

## Purpose
This repository implements **DecorAI**, an AI‑assisted interior‑design assistant. The application provides:
- A **frontend** UI for users to upload room photos, select style preferences, and receive design suggestions.
- A set of **backend APIs** handling user authentication, design generation, and data persistence.
- An **infrastructure layer** containing Docker compose files, CI/CD pipelines, and deployment scripts.

## High‑Level Architecture
```
DecorAI
├─ Application/                # Solution files, shared utilities
├─ frontend/                  # React/Angular (UI) – static assets, components
├─ GatewayApi/                # API gateway – routing, aggregation
├─ Infrastructure/            # Docker, Kubernetes, CI workflows
├─ UserApi/                   # CRUD for user profiles & preferences
├─ UserAuthApi/               # Authentication (JWT, OAuth)
└─ UserDomain/                # Domain models, business logic
```

## How the AI Model Should Interact
1. **Read the repository** – Use the provided tools (`glob`, `read_file`, `grep_search`) to explore the code.
2. **Follow project conventions** – Follow the existing naming, testing, and linting setup.
3. **Stay within scope** – Only modify files that are part of the requested task. Do not delete or rename existing files unless explicitly asked.
4. **Create documentation** – When a new feature or internal tool is added, create a markdown file (e.g., `skill.md`, `README.md`) that explains:
   - What the component does.
   - How to run/tests it locally.
   - Any required environment variables or external services.
5. **Track work** – Use the `todo_write` tool to generate a todo list for multi‑step changes and mark tasks as `in_progress`/`completed`.

## Development Workflow (Typical)
- **Install dependencies** – `npm install` (frontend) / `dotnet restore` (backend).
- **Run the backend** – `dotnet run` inside each API project.
- **Run the frontend** – `npm start` from `frontend/`.
- **Run tests** – Follow the test command described in each sub‑project’s README (e.g., `npm test`, `dotnet test`).

## Useful Commands (for the AI)
```
# List all API projects
rg --files "*Api.csproj" -g "**/*.csproj"

# Run backend API (example)
run_shell_command: "dotnet run --project UserApi/UserApi.csproj" (foreground)

# Start frontend
run_shell_command: "npm start" (foreground, in ./frontend)
```

---
*This file serves as the primary entry point for any AI‑assisted development or automation tasks within the DecorAI Fern_n_Flair project.*