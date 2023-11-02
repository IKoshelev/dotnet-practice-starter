# WARNING this NOT for production, education only
$characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'.ToCharArray()
$pass = -join ($characters | Get-Random -Count 16)
$pass | Out-File -NoNewline -FilePath "$PSScriptRoot/pass.txt"

dotnet dev-certs https --clean
dotnet dev-certs https -ep "$PSScriptRoot/IdenttityServer.pfx" -p $pass
dotnet dev-certs https --trust