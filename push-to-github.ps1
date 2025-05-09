# PowerShell script to push WSUSSCN2-API to GitHub under Grace-Solutions organization
# Run from the root directory of the project

param (
    [Parameter(Mandatory=$true)]
    [string]$PersonalAccessToken,
    
    [string]$RepoName = "WSUSSCN2-API",
    
    [string]$Description = "A containerized microservice platform for automating the ingestion, processing, and serving of Windows Update metadata from the wsusscn2.cab file.",
    
    [switch]$Private
)

$ErrorActionPreference = "Stop"

# Function to check if a command exists
function Test-Command {
    param (
        [string]$Command
    )
    
    $exists = $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
    return $exists
}

# Check if git is installed
if (-not (Test-Command "git")) {
    Write-Host "Git is not installed. Please install Git and try again." -ForegroundColor Red
    exit 1
}

# Check if GitHub CLI is installed
$useGitHubCLI = Test-Command "gh"

# Organization name
$orgName = "Grace-Solutions"

# Repository visibility
$visibility = if ($Private) { "private" } else { "public" }

# Create GitHub repository
Write-Host "Creating GitHub repository: $orgName/$RepoName..." -ForegroundColor Cyan

if ($useGitHubCLI) {
    # Using GitHub CLI
    Write-Host "Using GitHub CLI to create repository..." -ForegroundColor Yellow
    
    # Check if already authenticated
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Authenticating with GitHub CLI using token..." -ForegroundColor Yellow
        $env:GITHUB_TOKEN = $PersonalAccessToken
        gh auth login --with-token
    }
    
    # Create repository
    gh repo create "$orgName/$RepoName" --description "$Description" --$visibility
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create repository using GitHub CLI." -ForegroundColor Red
        exit 1
    }
} else {
    # Using REST API
    Write-Host "Using GitHub REST API to create repository..." -ForegroundColor Yellow
    
    $headers = @{
        Authorization = "token $PersonalAccessToken"
        Accept = "application/vnd.github.v3+json"
    }
    
    $body = @{
        name = $RepoName
        description = $Description
        private = [bool]$Private
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "https://api.github.com/orgs/$orgName/repos" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    } catch {
        Write-Host "Failed to create repository using GitHub API: $_" -ForegroundColor Red
        exit 1
    }
}

# Configure Git remote
Write-Host "Configuring Git remote..." -ForegroundColor Cyan

# Check if remote already exists
$remoteExists = git remote -v | Select-String -Pattern "origin" -Quiet

if ($remoteExists) {
    Write-Host "Remote 'origin' already exists. Updating URL..." -ForegroundColor Yellow
    git remote set-url origin "https://github.com/$orgName/$RepoName.git"
} else {
    Write-Host "Adding remote 'origin'..." -ForegroundColor Yellow
    git remote add origin "https://github.com/$orgName/$RepoName.git"
}

# Stage all files
Write-Host "Staging all files..." -ForegroundColor Cyan
git add .

# Commit changes
Write-Host "Committing changes..." -ForegroundColor Cyan
git commit -m "Initial commit of WSUSSCN2-API"

# Push to GitHub
Write-Host "Pushing to GitHub..." -ForegroundColor Cyan

# Set credential helper to store the token
$env:GIT_ASKPASS = "echo"
$env:GIT_USERNAME = "x-access-token"
$env:GIT_PASSWORD = $PersonalAccessToken

# Push to GitHub
git push -u origin main

# Clean up environment variables
$env:GIT_ASKPASS = $null
$env:GIT_USERNAME = $null
$env:GIT_PASSWORD = $null
$env:GITHUB_TOKEN = $null

Write-Host "Repository successfully pushed to GitHub: https://github.com/$orgName/$RepoName" -ForegroundColor Green
Write-Host "You can now clone it using: git clone https://github.com/$orgName/$RepoName.git" -ForegroundColor Green
