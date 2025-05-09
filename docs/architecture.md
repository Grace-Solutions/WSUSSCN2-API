# WSUSSCN2-API Architecture

WSUSSCN2-API is a containerized microservice platform designed to automate the ingestion, processing, and serving of Windows Update metadata from the `wsusscn2.cab` file.

## System Overview

The system consists of several microservices, each with a specific responsibility:

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│     UI      │────▶│     API     │◀───▶│  Database   │◀───▶│  Microservices │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                           ▲                                       ▲
                           │                                       │
                           ▼                                       ▼
                    ┌─────────────┐                        ┌─────────────┐
                    │    Redis    │                        │    MinIO    │
                    └─────────────┘                        └─────────────┘
```

## Components

### API Gateway

The API Gateway serves as the entry point for all client requests. It:
- Authenticates requests using API tokens
- Authorizes access based on token permissions
- Routes requests to the appropriate services
- Provides a unified REST API for clients

### Microservices

#### Sync Service

Responsible for downloading and verifying the `wsusscn2.cab` file:
- Downloads the file from Microsoft's servers
- Verifies the file integrity
- Stores the file in MinIO
- Triggers the parse service when a new file is available
- Runs on a configurable schedule

#### Parse Service

Responsible for extracting metadata from the `wsusscn2.cab` file:
- Extracts XML metadata from the CAB file
- Parses the metadata into structured data
- Stores the data in PostgreSQL
- Triggers the rebuild service when parsing is complete

#### Rebuild Service

Responsible for creating WUA-compatible CAB files:
- Groups updates according to the configured strategy
- Creates CAB files for each group
- Stores the CAB files in MinIO
- Updates the database with CAB file information

### Storage

#### PostgreSQL

Stores structured data:
- Update metadata
- API tokens
- CAB file information
- Sync history

#### MinIO

Stores binary data:
- Original `wsusscn2.cab` files
- Rebuilt CAB files

#### Redis

Used for:
- Caching frequently accessed data
- Distributed locking
- Message passing between services

## Data Flow

1. The Sync service downloads the `wsusscn2.cab` file and stores it in MinIO
2. The Parse service extracts metadata from the CAB file and stores it in PostgreSQL
3. The Rebuild service creates WUA-compatible CAB files and stores them in MinIO
4. The API Gateway provides access to the data through a REST API
5. The UI provides a user interface for managing tokens and viewing data

## Authentication and Authorization

The system uses token-based authentication with role-based access control:
- Each token has one or more permissions
- Permissions determine what API endpoints the token can access
- Tokens can be created, updated, and revoked through the API
- Token operations are logged for audit purposes

## Deployment

The system is designed to be deployed as a set of Docker containers:
- Each service runs in its own container
- Services communicate through a Docker network
- Configuration is provided through environment variables
- Persistent data is stored in Docker volumes

For Kubernetes deployment, manifests are provided in the `iaac/k8s` directory.