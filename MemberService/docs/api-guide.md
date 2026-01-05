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

### Validate Token
驗證 JWT Token 是否有效，供其他微服務調用以確認使用者身份。

```http
GET /api/auth/validate
Authorization: Bearer <access_token>
```

**成功回應 (200)**:
```json
{
  "isValid": true,
  "userId": 1234567890123456,
  "expiresAt": "2025-12-05T10:15:00Z"
}
```

**失敗回應 (401)**:
```json
{
  "success": false,
  "error": {
    "code": "INVALID_TOKEN",
    "message": "JWT Token 無效或已過期",
    "timestamp": "2025-12-05T10:00:00Z",
    "path": "/api/auth/validate"
  }
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