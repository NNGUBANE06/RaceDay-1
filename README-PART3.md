# RaceDay – Part 3 MVC

Part 3 adds an ASP.NET Core MVC frontend to the existing Part 2 REST API. The MVC application communicates with the API over HTTP and does not connect directly to the database.

## Projects
- `RaceDay/src/RaceDay.Api`: existing REST API and data layer (Part 2).
- `RaceDay/src/RaceDay.Web`: MVC frontend (Part 3).
- `RaceDay/src/RaceDay.Tests`: existing API tests.

## Run locally (Windows / Visual Studio)
1. Install the .NET 8 SDK and SQL Server/LocalDB.
2. Open `RaceDay/src/RaceDay.Api/appsettings.json` and check the database connection string.
3. Start the API: `dotnet run --project RaceDay/src/RaceDay.Api`.
4. The API launch profile uses `https://localhost:7100` and `http://localhost:5100`.
5. In a second terminal start MVC: `dotnet run --project RaceDay/src/RaceDay.Web`.
6. Open the MVC URL printed in the terminal. MVC is configured to call the API at `https://localhost:7100/`.

## Docker
Build from the repository root:
```bash
docker build -f RaceDay/src/RaceDay.Web/Dockerfile -t raceday-web .
docker run --rm -p 8080:8080 -e RaceDayApi__BaseUrl=http://host.docker.internal:5100/ raceday-web
```
Start the API separately and make sure it is reachable from the container. On Docker Desktop, `host.docker.internal` resolves to the host machine. For Linux Docker, configure the host gateway or use a reachable API service name. Do not use localhost inside the MVC container to refer to the host API.

## Implemented MVC features
- Landing page and responsive Bootstrap navigation.
- Account registration, login and logout using the API's cookie authentication.
- Session-backed role-aware navigation for Organisers and Participants.
- Event listing and details.
- Organiser event create, update and delete.
- Organiser event category add/remove.
- Organiser view of event enrolments and result capture.
- Participant event enrolment, personal enrolments and personal results.
- Profile view and update.
- Dockerfile and GitHub Actions build workflow.

## Remaining integration work / validation
- Azure Blob Storage image upload and display must be added to the Part 2 API first; MVC must only call API upload endpoints and must not access Azure directly. No Blob upload endpoint was present in the Part 2 API when this MVC starter was assembled.
- Verify the API cookie name and cookie forwarding in a running local environment.
- Run the API and MVC together, complete end-to-end tests, and update screenshots/demo video links before submission.
- The sample MVC project currently expects the API to be available at the configured URL. Set `RaceDayApi:BaseUrl` or the `RaceDayApi__BaseUrl` environment variable to match your environment.
