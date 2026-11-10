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

## Azure Blob Storage setup (image uploads)
Image uploads are implemented in the Part 2 API. MVC sends multipart uploads to the API; only the API communicates with Azure. Event banners and profile pictures use separate private containers, and the API returns short-lived read-only SAS URLs for display.

1. Create an Azure Storage Account in Azure Portal.
2. Open **Storage account → Security + networking → Access keys** and copy a connection string.
3. Set the connection string as an environment variable for the API (do not commit it):
   - PowerShell: `$env:AzureBlobStorage__ConnectionString="YOUR_CONNECTION_STRING"`
   - Bash: `export AzureBlobStorage__ConnectionString="YOUR_CONNECTION_STRING"`
4. Optional container names: `AzureBlobStorage__EventContainer` (default `event-banners`) and `AzureBlobStorage__ProfileContainer` (default `profile-images`). The API creates the containers as private containers on first upload.
5. Apply `RaceDay/src/RaceDay.Api/Database/20261110_AddBlobImageNames.sql` to the RaceDay database before using uploads. It adds nullable blob-name columns to Events and Users.
6. Start the API with the environment variable set, then start MVC. Organisers can upload an event banner on create/edit; users can upload a profile picture on the profile edit page.

Allowed image formats are JPEG, PNG and WebP; maximum file size is 5 MB. Keep the connection string out of GitHub, screenshots and chat. An example non-secret configuration shape is in `RaceDay/src/RaceDay.Api/appsettings.Azure.example.json`.

## Remaining work
- Run the API and MVC together and complete end-to-end validation locally.
- Record the demonstration video and add its link before submission.
