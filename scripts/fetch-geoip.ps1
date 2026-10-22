# Downloads the DB-IP Lite country database (CC BY 4.0, https://db-ip.com/db) into
# src/Rat/Data/geoip-country.csv.gz and embeds it into the app (offline country lookup).
$dest = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/Rat/Data/geoip-country.csv.gz'

for ($offset = 0; $offset -le 2; $offset++) {
    $month = (Get-Date).AddMonths(-$offset).ToString('yyyy-MM')
    $url = "https://download.db-ip.com/free/dbip-country-lite-$month.csv.gz"
    try {
        Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
        Write-Host "downloaded $month -> $dest ($([Math]::Round((Get-Item $dest).Length / 1MB)) MB)"
        exit 0
    }
    catch {
        Remove-Item $dest -ErrorAction SilentlyContinue
    }
}
throw 'could not download the DB-IP country database'
