# API Guide

## Authentication Endpoints

### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "username": "User Name"
}
```

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

### Refresh Token
```http
POST /api/auth/refresh-token
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "refreshToken": "base64_encoded_token"
}
```

### Logout
```http
POST /api/auth/logout
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "refreshToken": "base64_encoded_token"
}
```

## Error Responses

All errors follow this format:
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable message",
    "timestamp": "2025-12-05T10:00:00Z",
    "path": "/api/auth/register"
  }
}
```

## Success Responses

Success responses include:
```json
{
  "success": true,
  "data": { ... }
}
```