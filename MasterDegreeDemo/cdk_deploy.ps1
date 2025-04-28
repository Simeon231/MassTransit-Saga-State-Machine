try {
    docker info 2>$null | Out-Null
} catch {
    Write-Host ""
    Write-Host "[ERROR] Docker is not running! Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[INFO] Docker is running. Deploying CDK stack..." -ForegroundColor Green

cdk deploy
