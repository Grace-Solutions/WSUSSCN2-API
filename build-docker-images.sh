#!/bin/bash
# Bash script to build all Docker images for WSUSSCN2-API
# Run from the root directory of the project
#
# Usage:
#   ./build-docker-images.sh [options]
#
# Options:
#   --clean     Remove all existing containers and images before building
#   --force     Force rebuild by using --no-cache
#   --services  Comma-separated list of services to build (default: all)
#               Valid values: api, sync, parse, rebuild, ui
#   --help      Show this help message

set -e

# Define colors for output
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Default values
CLEAN=false
FORCE=false
SERVICES="api,sync,parse,rebuild,ui"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --clean)
            CLEAN=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --services)
            SERVICES="$2"
            shift 2
            ;;
        --help)
            echo "Usage: ./build-docker-images.sh [options]"
            echo ""
            echo "Options:"
            echo "  --clean     Remove all existing containers and images before building"
            echo "  --force     Force rebuild by using --no-cache"
            echo "  --services  Comma-separated list of services to build (default: all)"
            echo "              Valid values: api, sync, parse, rebuild, ui"
            echo "  --help      Show this help message"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Define all image names and paths
declare -A all_images=(
    ["api"]="wsusscn2-api/api:latest|./api"
    ["sync"]="wsusscn2-api/sync:latest|./backend/microservices/sync"
    ["parse"]="wsusscn2-api/parse:latest|./backend/microservices/parse"
    ["rebuild"]="wsusscn2-api/rebuild:latest|./backend/microservices/rebuild"
    ["ui"]="wsusscn2-api/ui:latest|./ui"
)

# Parse services to build
IFS=',' read -ra SERVICES_ARRAY <<< "$SERVICES"
SERVICES_TO_BUILD=()

for service in "${SERVICES_ARRAY[@]}"; do
    service=$(echo "$service" | tr -d '[:space:]' | tr '[:upper:]' '[:lower:]')
    if [[ -n "${all_images[$service]}" ]]; then
        SERVICES_TO_BUILD+=("$service")
    else
        echo -e "${YELLOW}Warning: Unknown service '$service', skipping${NC}"
    fi
done

if [[ ${#SERVICES_TO_BUILD[@]} -eq 0 ]]; then
    echo -e "${RED}No valid services specified. Valid services are: api, sync, parse, rebuild, ui${NC}"
    exit 1
fi

# Function to clean Docker resources
clean_docker() {
    echo -e "${YELLOW}Cleaning Docker resources...${NC}"

    # Stop and remove containers
    containers=$(docker ps -a --filter "name=wsusscn2-api" --format "{{.Names}}" 2>/dev/null || true)
    if [[ -n "$containers" ]]; then
        echo -e "${CYAN}Stopping and removing containers...${NC}"
        docker stop $containers 2>/dev/null || true
        docker rm $containers 2>/dev/null || true
    fi

    # Remove images
    for service in "${!all_images[@]}"; do
        IFS='|' read -r image_name image_path <<< "${all_images[$service]}"
        image_id=$(docker images -q "$image_name" 2>/dev/null || true)
        if [[ -n "$image_id" ]]; then
            echo -e "${CYAN}Removing image: $image_name${NC}"
            docker rmi -f "$image_name" 2>/dev/null || true
        fi
    done

    echo -e "${GREEN}Docker resources cleaned${NC}"
}

# Function to build an image
build_image() {
    local name=$1
    local path=$2
    local force=$3

    echo -e "${CYAN}Building image: $name from $path${NC}"

    # Check if image already exists and not forcing rebuild
    if [[ "$force" != "true" ]]; then
        image_id=$(docker images -q "$name" 2>/dev/null || true)
        if [[ -n "$image_id" ]]; then
            echo -e "${YELLOW}Image already exists. Use --force to rebuild.${NC}"
            return 0
        fi
    fi

    # Build arguments
    build_args=("-t" "$name")

    if [[ "$force" == "true" ]]; then
        echo -e "${YELLOW}Force rebuild enabled (--no-cache)${NC}"
        build_args+=("--no-cache")
    fi

    build_args+=("$path")

    if docker build "${build_args[@]}"; then
        echo -e "${GREEN}Successfully built image: $name${NC}"
    else
        echo -e "${RED}Failed to build image: $name${NC}"
        exit 1
    fi
}

# Main script
echo -e "${YELLOW}Starting Docker build process for WSUSSCN2-API...${NC}"
echo -e "${YELLOW}Services to build: ${SERVICES_TO_BUILD[*]}${NC}"

# Check if Docker is available
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker is not available. Please install Docker and try again.${NC}"
    exit 1
fi

# Clean if requested
if [[ "$CLEAN" == "true" ]]; then
    clean_docker
fi

# Build each image
for service in "${SERVICES_TO_BUILD[@]}"; do
    IFS='|' read -r image_name image_path <<< "${all_images[$service]}"
    build_image "$image_name" "$image_path" "$FORCE"
done

echo -e "${GREEN}Docker build process completed!${NC}"
