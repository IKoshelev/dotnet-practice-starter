$pass='3333gggggPPPPGGGGG******vvvv2222222'
$passHash=$(echo $pass | docker run --rm -i datalust/seq config hash)
"SEQ_FIRSTRUN_ADMINPASSWORDHASH=$passHash" | Out-File -Encoding UTF8 -NoNewline -FilePath "$PSScriptRoot/password-hash.env"
