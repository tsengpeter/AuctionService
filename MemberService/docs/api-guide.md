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

**無效Token回應 (401)**:
```json
{
  "isValid": false,
  "userId": null,
  "expiresAt": null
}
```

**缺少Token回應 (401)**:
```json
{
  "isValid": false,
  "userId": null,
  "expiresAt": null
}
```

## Error Responses

API 錯誤時會返回適當的 HTTP 狀態碼和錯誤詳情。常見錯誤格式包括：

**驗證錯誤 (400)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is required."],
    "Password": ["The Password field is required."]
  },
  "traceId": "00-..."
}
```

**認證錯誤 (401)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid username or password.",
  "instance": "/api/auth/login"
}
```

**資源未找到 (404)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found.",
  "instance": "/api/users/123"
}
```

**資源衝突 (409)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Email already exists.",
  "instance": "/api/auth/register"
}
```

## Success Responses

成功回應直接返回請求的資料結構，不使用額外的包裝層。

**註冊成功 (201)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64_encoded_token",
  "expiresAt": "2025-12-05T10:15:00Z",
  "user": {
    "id": 1234567890123456,
    "email": "user@example.com",
    "username": "User Name"
  },
  "tokenType": "Bearer"
}
```

**登入成功 (200)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64_encoded_token",
  "expiresAt": "2025-12-05T10:15:00Z",
  "user": {
    "id": 1234567890123456,
    "email": "user@example.com",
    "username": "User Name"
  },
  "tokenType": "Bearer"
}
```