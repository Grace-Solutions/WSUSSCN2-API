# PowerShell script to build all Docker images for WSUSSCN2-API
# Run from the root directory of the project
#
# Usage:
#   .\build-docker-images.ps1 [-Clean] [-Force] [-Services api,sync,parse,rebuild,ui]
#
# Parameters:
#   -Clean    : Remove all existing containers and images before building
#   -Force    : Force rebuild by using --no-cache
#   -Services : Comma-separated list of services to build (default: all)
#               Valid values: api, sync, parse, rebuild, ui

param (
    [switch]$Clean,
    [switch]$Force,
    [string]$Services = "api,sync,parse,rebuild,ui"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Define image names and paths
$allImages = @(
    @{Name = "wsusscn2-api/api"; Path = "./api"; Service = "api"},
    @{Name = "wsusscn2-api/sync"; Path = "./backend/microservices/sync"; Service = "sync"},
    @{Name = "wsusscn2-api/parse"; Path = "./backend/microservices/parse"; Service = "parse"},
    @{Name = "wsusscn2-api/rebuild"; Path = "./backend/microservices/rebuild"; Service = "rebuild"},
    @{Name = "wsusscn2-api/ui"; Path = "./ui"; Service = "ui"}
)

# Parse services parameter
$servicesToBuild = $Services.Split(',') | ForEach-Object { $_.Trim().ToLower() }

# Filter images based on services parameter
$images = $allImages | Where-Object { $servicesToBuild -contains $_.Service }

if ($images.Count -eq 0) {
    Write-Host "No valid services specified. Valid services are: api, sync, parse, rebuild, ui" -ForegroundColor Red
    exit 1
}

# Function to clean Docker resources
function Clean-Docker {
    Write-Host "Cleaning Docker resources..." -ForegroundColor Yellow

    # Stop and remove containers
    $containers = docker ps -a --filter "name=wsusscn2-api" --format "{{.Names}}" 2>$null
    if ($containers) {
        Write-Host "Stopping and removing containers..." -ForegroundColor Cyan
        docker stop $containers 2>$null
        docker rm $containers 2>$null
    }

    # Remove images
    foreach ($image in $allImages) {
        $imageName = "$($image.Name):latest"
        $imageId = docker images -q $imageName 2>$null
        if ($imageId) {
            Write-Host "Removing image: $imageName" -ForegroundColor Cyan
            docker rmi -f $imageName 2>$null
        }
    }

    Write-Host "Docker resources cleaned" -ForegroundColor Green
}

# Function to build an image
function Build-Image {
    param (
        [string]$Name,
        [string]$Path,
        [switch]$ForceRebuild
    )

    Write-Host "Building image: $Name from $Path" -ForegroundColor Cyan

    try {
        $buildArgs = @()
        $buildArgs += "-t"
        $buildArgs += "$($Name):latest"

        if ($ForceRebuild) {
            $buildArgs += "--no-cache"
            Write-Host "Force rebuild enabled (--no-cache)" -ForegroundColor Yellow
        } else {
            # Check if image already exists
            $imageExists = docker images -q "$($Name):latest" 2>$null
            if ($imageExists) {
                Write-Host "Image already exists. Use -Force to rebuild." -ForegroundColor Yellow
                return
            }
        }

        $buildArgs += $Path

        docker build $buildArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to build image: $Name" -ForegroundColor Red
            exit 1
        }

        Write-Host "Successfully built image: $Name" -ForegroundColor Green
    }
    catch {
        Write-Host "Error building image: $Name - $_" -ForegroundColor Red
        exit 1
    }
}

# Main script
Write-Host "Starting Docker build process for WSUSSCN2-API..." -ForegroundColor Yellow
Write-Host "Services to build: $Services" -ForegroundColor Yellow

# Check if Docker is available
try {
    docker --version | Out-Null
}
catch {
    Write-Host "Docker is not available. Please install Docker and try again." -ForegroundColor Red
    exit 1
}

# Clean if requested
if ($Clean) {
    Clean-Docker
}

# Build each image
foreach ($image in $images) {
    Build-Image -Name $image.Name -Path $image.Path -ForceRebuild:$Force
}

Write-Host "Docker build process completed!" -ForegroundColor Green
