# WSUSSCN2-API

A containerized microservice platform for automating the ingestion, processing, and serving of Windows Update metadata from the `wsusscn2.cab` file.

## Features

- **Automated Sync**: Periodically downloads and processes the latest `wsusscn2.cab` file
- **Metadata Extraction**: Parses update metadata and stores it in PostgreSQL
- **CAB Rebuilding**: Creates WUA-compatible CAB files grouped by various strategies
- **REST API**: Provides access to update metadata and CAB files
- **Token Authentication**: Secure API access with role-based permissions
- **Web UI**: User interface for managing tokens and viewing data
- **Containerized**: Runs as a set of Docker containers

## Quick Start

### Prerequisites

- Docker and Docker Compose
- 2GB+ of RAM for the containers
- 10GB+ of disk space

### Docker Compose Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/WSUSSCN2-API.git
   cd WSUSSCN2-API
   ```

2. Configure the environment:
   ```bash
   # Review and modify the default configuration
   cp iaac/.env.example iaac/.env
   ```

3. Start the services:
   ```bash
   cd iaac
   docker-compose up -d
   ```

4. Access the UI:
   ```
   http://localhost:3000
   ```
   Default credentials: `admin` / `admin`

### Kubernetes Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/WSUSSCN2-API.git
   cd WSUSSCN2-API
   ```

2. Build and push the Docker images:
   ```bash
   # Build API image
   docker build -t wsusscn2-api/api:latest ./api

   # Build microservices images
   docker build -t wsusscn2-api/sync:latest ./backend/microservices/sync
   docker build -t wsusscn2-api/parse:latest ./backend/microservices/parse
   docker build -t wsusscn2-api/rebuild:latest ./backend/microservices/rebuild

   # Build UI image
   docker build -t wsusscn2-api/ui:latest ./ui

   # Push images to your registry if needed
   ```

3. Deploy to Kubernetes:
   ```bash
   # Create namespace and deploy components
   kubectl apply -f iaac/k8s/namespace.yaml
   kubectl apply -f iaac/k8s/configmap.yaml
   kubectl apply -f iaac/k8s/secrets.yaml
   kubectl apply -f iaac/k8s/postgres.yaml
   kubectl apply -f iaac/k8s/redis.yaml
   kubectl apply -f iaac/k8s/minio.yaml
   kubectl apply -f iaac/k8s/api.yaml
   kubectl apply -f iaac/k8s/microservices.yaml
   kubectl apply -f iaac/k8s/ui.yaml
   ```

4. Access the UI:
   ```
   http://wsusscn2-api.local
   ```
   Default credentials: `admin` / `admin`

   Note: You may need to add an entry to your hosts file or configure DNS to resolve `wsusscn2-api.local` to your Kubernetes ingress IP.

## Architecture

The system consists of several microservices:

- **API Gateway**: Entry point for all client requests
- **Sync Service**: Downloads and verifies the `wsusscn2.cab` file
- **Parse Service**: Extracts metadata from the CAB file
- **Rebuild Service**: Creates WUA-compatible CAB files
- **UI**: Web interface for managing tokens and viewing data

For more details, see [Architecture Documentation](docs/architecture.md).

## API Endpoints

| Method | Path                     | Description                 |
| ------ | ------------------------ | --------------------------- |
| GET    | `/updates`               | List updates                |
| GET    | `/updates/{id}`          | Get update detail           |
| GET    | `/updates/changed-since` | Get updates since timestamp |
| GET    | `/cabs/{group}`          | Get pre-signed CAB link     |
| GET    | `/source`                | Pre-signed source CAB link  |
| POST   | `/sync/trigger`          | Manual CAB sync             |
| GET    | `/tokens`                | List all tokens             |
| POST   | `/tokens`                | Create token                |
| PUT    | `/tokens/{id}`           | Update permissions/label    |
| DELETE | `/tokens/{id}`           | Revoke/delete token         |

For more details on permissions, see [Permissions Documentation](docs/permissions.md).

## Configuration

The system is configured through environment variables. See [.env](iaac/.env) for available options.

## Development

### Project Structure

```
WSUSSCN2-API/
├── api/                              # REST API
├── backend/microservices/            # Backend microservices
├── ui/                               # Radix UI frontend
├── configs/                          # Configuration files
├── iaac/                             # Infrastructure as code
└── docs/                             # Documentation
```

### Building from Source

Each component can be built separately:

```bash
# Build API
cd api
dotnet build

# Build microservices
cd backend/microservices/sync
dotnet build

# Build UI
cd ui
npm install
npm run build
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.