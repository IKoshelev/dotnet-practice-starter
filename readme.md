To run the whole thing:

```powershell
Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1 duende-identity-server " #you will need to allow certificate in browser :-(
.\certificate\generate.ps1
docker-compose up -d

#or 

docker-compose down; docker-compose up --build -d
1 test

```
