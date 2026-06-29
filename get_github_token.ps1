Add-Type -AssemblyName CredentialManager
$cred = Get-StoredCredential -Target 'git:https://github.com'
$pw = $cred.GetNetworkCredential().Password
Write-Output $pw
