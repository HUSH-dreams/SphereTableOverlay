Add-Type -AssemblyName CredentialManager
$cred = Get-StoredCredential -Target 'git:https://github.com'
$pw = $cred.GetNetworkCredential().Password
$pw | Out-File -FilePath "$env:TEMP\gh_token.txt" -Encoding utf8
Write-Output "Token saved"
