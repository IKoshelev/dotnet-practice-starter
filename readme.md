To run the whole thing:

```powershell
.\infrastructure\duende-identity-server\certificate\generate.ps1
docker-compose up -d

#or 

docker-compose down; docker-compose up --build -d 

```