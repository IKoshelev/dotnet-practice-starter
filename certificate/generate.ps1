# WARNING this NOT for production, education only
$characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'.ToCharArray()
$pass = -join ($characters | Get-Random -Count 16)
"ASPNETCORE_Kestrel__Certificates__Default__Password=$pass" | Out-File -Encoding UTF8 -NoNewline -FilePath "$PSScriptRoot/pass.env"

dotnet dev-certs https --clean
dotnet dev-certs https -ep "$PSScriptRoot/dev-certificate.pfx" -p $pass
dotnet dev-certs https --trust