# CONFIG
$server = "root@188.225.45.211"
$path = "/var/www/waldau"
$project = "C:\Users\savva\OneDrive\Documents\Domashka\BKP\WaldauCastle"

Write-Host "Publishing project..."

dotnet publish $project -c Release -o "$project\publish"

Write-Host "Uploading to server..."

scp -r "$project\publish\*" "${server}:$path"

Write-Host "Restarting service..."

ssh $server "systemctl restart waldau"

Write-Host "Done! Site updated."