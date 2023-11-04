$pass=$(echo '3333gggggPPPPGGGGG******vvvv2222222' | docker run --rm -i datalust/seq config hash)
"SEQ_FIRSTRUN_ADMINPASSWORDHASH=$pass" | Out-File -Encoding UTF8 -NoNewline -FilePath "$PSScriptRoot/password-hash.env"
