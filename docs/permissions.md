# API Token Permissions

WSUSSCN2-API uses a token-based authentication system with role-based access control (RBAC). Each token can have one or more permissions that determine what API endpoints the token can access.

## Available Permissions

| Permission | Description | Endpoints |
|------------|-------------|-----------|
| `updates:read` | Access to read update metadata | `GET /updates`, `GET /updates/{id}`, `GET /updates/changed-since`, `GET /source` |
| `cabs:read` | Access to download CAB files | `GET /cabs/{group}` |
| `sync:trigger` | Ability to manually trigger update synchronization | `POST /sync/trigger` |
| `admin` | Full administrative access | All endpoints, including token management |

## Permission Combinations

Tokens can have multiple permissions to allow access to different parts of the API. Here are some common combinations:

- **Read-only access**: `updates:read`
- **Download access**: `updates:read`, `cabs:read`
- **Sync access**: `updates:read`, `sync:trigger`
- **Full access**: `updates:read`, `cabs:read`, `sync:trigger`, `admin`

## Token Management

Only tokens with the `admin` permission can manage other tokens. This includes:

- Creating new tokens
- Updating existing tokens
- Revoking tokens
- Viewing all tokens

## Security Considerations

- Tokens should be treated as sensitive information and not shared
- Use the most restrictive set of permissions needed for each use case
- Tokens can be set to expire after a certain time
- Revoke tokens that are no longer needed
- All token operations are logged for audit purposes