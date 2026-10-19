# RaceDay API (Part 2 – RESTful API Development)

ASP.NET Core 8 Web API that powers the RaceDay system: authentication with role-based session access, event management, categories, enrolments and results. Data is stored in SQL Server through Entity Framework Core (Code-First).

[![CI](../../actions/workflows/ci.yml/badge.svg)](../../actions/workflows/ci.yml)

## CI/CD – green build

> **TODO before submitting:** push to GitHub, open the **Actions** tab, wait for the green build, take a screenshot and save it as `docs/green-build.png`.

![Successful GitHub Actions build](docs/green-build.png)

## Demo video

> **TODO before submitting:** replace with your unlisted YouTube link.

[RaceDay API walkthrough (YouTube, unlisted)](https://youtube.com/REPLACE_ME)

## Setup and run

**Prerequisites:** .NET 8 SDK, SQL Server / LocalDB, Visual Studio 2022 (or the `dotnet` CLI).

```bash
git clone <your-repo-url>
cd RaceDay

# 1. Set the connection string in src/RaceDay.Api/appsettings.json (LocalDB by default)

# 2. Install the EF tool once, then create the database from the code-first model
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/RaceDay.Api
dotnet ef database update --project src/RaceDay.Api

# 3. Run the API
dotnet run --project src/RaceDay.Api
```

Swagger UI opens at `https://localhost:7100/swagger`.
The schema must match your Part 1 ERD and SQL script (see "Data model" below).

### Run the tests

```bash
dotnet test
```

The tests boot the real API in memory with an in-memory database, so no SQL Server is needed.

## The two roles

| Role | What they can do |
|------|------------------|
| **Organiser** | Create, update and delete **their own** events; define age/distance categories; view all enrolments for their events; capture finish times and positions. |
| **Participant** | Browse events and categories; enrol in an event by choosing a category; view their own enrolments; view **their own** results. |

Both roles can register, log in, view events/categories, and view/update their own profile.
Organiser functionality is rejected for Participants (HTTP 403) and vice versa. Unauthenticated calls to non-public endpoints return HTTP 401.

## Authentication

- `POST /api/auth/register` – personal details plus a role (`Organiser` or `Participant`). Passwords are **hashed** (ASP.NET Core `PasswordHasher`, PBKDF2) – never stored in plain text.
- `POST /api/auth/login` – validates credentials and starts a **session** using a signed, HttpOnly cookie carrying the user id and role, so every subsequent request is authenticated and authorised by role.
- `POST /api/auth/logout` – ends the session.

## Endpoints

| Method | Route | Access |
|--------|-------|--------|
| POST | `/api/auth/register`, `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Authenticated |
| GET / PUT | `/api/profile` | Authenticated (own profile) |
| GET | `/api/events`, `/api/events/{id}` | Authenticated |
| POST / PUT / DELETE | `/api/events`, `/api/events/{id}` | Organiser (owner) |
| GET | `/api/events/{eventId}/categories` | Authenticated |
| POST / DELETE | `/api/events/{eventId}/categories[/{id}]` | Organiser (owner) |
| POST | `/api/events/{eventId}/enrolments` | Participant |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) |
| GET | `/api/enrolments/mine` | Participant |
| POST / GET | `/api/events/{eventId}/results` | Organiser (owner) |
| GET | `/api/results/mine` | Participant |

Every endpoint is documented in Swagger (purpose, expected data, responses).

## Data model

`User` (role) · `Event` (name, description, date, location, distance, type Run/Walk/Cycle, organiser) · `Category` (belongs to Event) · `Enrolment` (Participant + Event + Category, unique per participant/event) · `RaceResult` (finish time, position, one per enrolment).

## Project structure

```
src/RaceDay.Api   Controllers, Models, Data (DbContext), Dtos, Program.cs
tests/RaceDay.Tests   xUnit tests: auth, roles, events, enrolments, results
.github/workflows/ci.yml   Build + test on every push
```
