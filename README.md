# IT Asset Tracker API

![CI](https://github.com/Frans345/it-asset-tracker-api/actions/workflows/ci.yml/badge.svg)

A REST API for tracking company IT hardware: laptops, desktops, servers and switches, who they are assigned to, and their current status.

I built this after an IT infrastructure internship where hardware was tracked by hand. It is a small project, built the way production back-end services are built: authenticated, tested, containerised and checked automatically on every push.

## Features

- Create, read, update and delete hardware assets
- JWT login with two roles: **Admin** (full access) and **Viewer** (read-only)
- Passwords stored as salted hashes, never as plain text
- Secrets kept out of the repository with .NET user-secrets
- 10 integration tests covering login, permissions and every asset endpoint
- Dockerfile with a multi-stage build
- GitHub Actions pipeline that runs the tests and builds the Docker image on every push

## Tech stack

| Area | Technology |
|---|---|
| Language and framework | C#, ASP.NET Core (.NET 10) minimal APIs |
| Database | SQLite with Entity Framework Core |
| Authentication | JWT bearer tokens, role-based authorisation |
| Testing | xUnit, WebApplicationFactory |
| Containers | Docker |
| CI | GitHub Actions |

## Endpoints

| Method | Route | Access | Description |
|---|---|---|---|
| POST | `/api/auth/login` | Anyone | Log in and receive a token |
| GET | `/api/assets` | Any logged-in user | List all assets |
| GET | `/api/assets/{id}` | Any logged-in user | Get one asset |
| POST | `/api/assets` | Admin | Add an asset |
| PUT | `/api/assets/{id}` | Admin | Update an asset |
| DELETE | `/api/assets/{id}` | Admin | Delete an asset |

Send the token with each request in the `Authorization: Bearer <token>` header.

## Run it locally

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
git clone https://github.com/Frans345/it-asset-tracker-api.git
cd it-asset-tracker-api

dotnet user-secrets set "Jwt:Key" "a-random-string-of-at-least-32-characters" --project src/AssetTracker.Api
dotnet user-secrets set "Seed:AdminPassword" "your-admin-password" --project src/AssetTracker.Api
dotnet user-secrets set "Seed:ViewerPassword" "your-viewer-password" --project src/AssetTracker.Api

dotnet run --project src/AssetTracker.Api
```

The API starts on `http://localhost:5112`. Browse the endpoints at `http://localhost:5112/swagger`.

On first run the app creates the database and two demo users, `admin` and `viewer`, with the passwords you set above.

## Run the tests

```
dotnet test
```

The tests start the API in memory with a temporary database, so they never touch real data.

## Run with Docker

```
docker build -t it-asset-tracker-api .
docker run -p 8080:8080 -e Jwt__Key="a-random-string-of-at-least-32-characters" -e Seed__AdminPassword="your-admin-password" -e Seed__ViewerPassword="your-viewer-password" it-asset-tracker-api
```

The API is then available on `http://localhost:8080`.

## Possible next steps

- Maintenance history for each asset
- Database migrations in place of automatic database creation
- Filtering and searching assets by type, status or assignee
