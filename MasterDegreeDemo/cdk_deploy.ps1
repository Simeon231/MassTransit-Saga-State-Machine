$dockerStatus = docker info 2>&1

if ($LASTEXITCODE -ne 0 -or $dockerStatus -like "*Cannot connect*") {
    Write-Host ""
    Write-Host "[ERROR] Docker is not running or not responsive. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[INFO] Docker is running. Deploying CDK stack..." -ForegroundColor Green

cdk deploy --require-approval never
