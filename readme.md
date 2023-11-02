To run the whole thing:

```powershell
.\infrastructure\identity-server-duende\certificate\generate.ps1
docker-compose up -d

#or 

docker-compose down; docker-compose up --build -d 
```