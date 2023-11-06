# dotnet-practice-starter

This repository contains a Docker Compose setup and application skeleton to practice modern Cloud DOTNET application development, specifically ASPNET.  

This starter is made with 3 goals in mind:

- Provide an ASPNET application skeleton with typical enterprise **infrastructure**  already in place. This includes Identity Server for user authentication, logs storage setup via [OpenTelemetry](https://opentelemetry.io/), various databases, queue and file storage.
- Showcase a Docker Compose setup that allows you to run a **mini-Cloud** on your personal machine. With a single `compose.yaml` file you can selectively spin-up everything you need.
- Showcase [devcontainers](https://code.visualstudio.com/docs/devcontainers/containers) - as soon as base devcontainer for DOTNET 8 is released.

## Intended audience

This starter is made for DOTNET students practicing ASPNET and Cloud development, for Enterprise Developers looking for complete Docker Compose setup to try-out and for Hackathon participants needing a quick DOTNET starter with infrastructure already setup. 

## Required software and hardware

It's expected that you are running a machine with [VSCode](https://code.visualstudio.com/) with [Remote Development Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack), [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.3), [DOTNET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and [Docker](https://www.docker.com/get-started/) installed (Docker Desktop is recommended). At least 16GB of RAM, 4-Core CPU and an SSD are recommended if you intend to run entire Docker Compose. Please also note, initial launch will require considerable time and web bandwidth, as it will download required Docker images. Containers use Linux-based images, but for **host** it's recommended to use Windows 10 or 11 based machine with [WSL2](https://learn.microsoft.com/en-us/windows/wsl/install) installed (as Mac and Linux testing are pending). 

## What's included
- VSCode recommended extension lists, task and launch files
- `app-console` application configured to use all databases in infrastructure
- `app-aspnet` application configured to use all services in infrastructure
- `certificate` script to setup dev-certificate trusted by `host` and usable by infrastructure containers  

Infrastructure: 
|service name in compose.yaml|service type|description|
-|-|-
`azurite-storage` | Docker Image | [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) storage emulator based on [Azure Storage](https://learn.microsoft.com/en-us/azure/storage/), provides Blobs, Queue Storage, and Table Storage
`duende-identity-server` | ASPNET app with Dockerfile | [Duende Identity Server](https://duendesoftware.com/products/identityserver): [OAuth](https://oauth.net/2/) and [OIDC](https://openid.net/) server
`mongo-document-db` | Docker Image | [Mongo document database](https://www.mongodb.com/)
`ms-sql-db` | Docker Image | [Microsoft SQL Server 2022](https://www.microsoft.com/en-us/sql-server)
`seq-open-telemetry-logs` | Docker Image | [Seq](https://datalust.co/seq): logs storage and search service

## First run

Once you've cloned the repository, generate a dev-certificate trusted by your machine and available to Containers:

```powershell
.\certificate\generate.ps1
```

Launch infrastructure services:

```powershell
docker compose down; docker compose --profile infrastructure up -d --build 
```

Then run console-app to make sure DBs are available:
```powershell
cd app-console;
dotnet run;
```

Or run ASPNET application 

```powershell
cd app-aspnet;
dotnet run;
```

Open page `https://localhost:5002/` and login with username: alice, password: alice or username: bob, password: bob. You should see a page with information about currently logged-in user abd DB connections statuses.

Navigate to `http://localhost:5340/` to see your application logs.

Examine [.\app-console\Program.cs](\app-console\Program.cs) or [.\app-aspnet\Pages\Index.cshtml.cs](\app-aspnet\Pages\Index.cshtml.cs) to see how dependencies are used. 

## Running app-aspnet in container

If you want to develop `app-aspnet` in container - run
```powershell
docker compose down; docker compose --profile infrastructure --profile app-aspnet up -d --build 
```

SSH into container or use VSCode extension to connect VSCode to it and in `/source/app` (default workdir in Dockerfile) run `dotnet run`. Navigate to `https://localhost:5002/` You will have to instruct your browser to trust certificate of Identity Server. This method also works with `app-console` profile.

## Database management and persistance

Check `compose.yaml` `ports` section for each service to see, which ports are exposed. `volumes` section will have instructions how to save data in `host`. 


