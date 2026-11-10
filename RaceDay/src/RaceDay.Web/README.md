# RaceDay MVC (Part 3 starter)

This ASP.NET Core 8 MVC application uses the existing RaceDay REST API for accounts and event data.

## Run locally
1. Install the .NET 8 SDK.
2. Start SQL Server and configure the API connection string in `src/RaceDay.Api/appsettings.json`.
3. Start the API from the repository's `RaceDay` folder:
   `dotnet run --project src/RaceDay.Api`
4. In another terminal run:
   `dotnet run --project src/RaceDay.Web`
5. Open the URL printed by the MVC project.

The MVC app expects the API at `https://localhost:7100/`. Change `RaceDayApi:BaseUrl` in appsettings.json if your API uses another URL.

## Docker
From the repository root:
`docker build -f RaceDay/src/RaceDay.Web/Dockerfile -t raceday-web .`
`docker run --rm -p 8080:8080 -e RaceDayApi__BaseUrl=http://host.docker.internal:7100/ raceday-web`

The API must be reachable from the container. Configure a suitable API URL for your environment; HTTPS development certificates are not automatically trusted inside the container.

## Current scope
Includes landing page, registration, login, logout, session cookie forwarding to the API, role-aware dashboard and event listing. Event CRUD, enrolment management, results, profile editing, Azure Blob Storage, automated tests and deployment still need implementing and validating.
