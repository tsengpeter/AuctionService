# API Guide

## Authentication Endpoints

### Register
註冊新使用者，成功後需要再次調用 Login 端點獲取 JWT tokens。

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "username": "User Name",
  "phoneNumber": "+886912345678"
}
```

**成功回應 (201)**:
```json
{
  "user": {
    "id": 1234567890123456,
    "email": "user@example.com",
    "username": "User Name",
    "phoneNumber": "+886912345678",
    "emailVerified": false,
    "phoneNumberVerified": false
  },
  "message": "Registration successful. Please login to continue."
}
```

### Login
使用者登入，成功後返回 JWT access token 和 refresh token。

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**成功回應 (200)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64_encoded_token",
  "expiresAt": "2026-01-06T10:15:00Z",
  "user": {
    "id": 1234567890123456,
    "email": "user@example.com",
    "username": "User Name"
  },
  "tokenType": "Bearer"
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
GET /api/auth/validate?token=<jwt_token>
```

**參數說明**:
- `token` (query parameter, optional): 要驗證的 JWT token

**回應 (200)**:
無論Token是否有效，都返回200狀態碼和統一的回應格式：

**有效Token**:
```json
{
  "isValid": true,
  "userId": 1234567890123456,
  "expiresAt": "2026-01-06T10:15:00Z",
  "errorMessage": null
}
```

**無效Token (包括缺少、過期、格式錯誤等)**:
```json
{
  "isValid": false,
  "userId": null,
  "expiresAt": null,
  "errorMessage": "Token has expired"
}
```

## Verification Endpoints

### Send Email Verification Code
發送電子郵件驗證碼。

```http
POST /api/auth/send-email-verification
Authorization: Bearer <access_token>
```

**成功回應 (200)**:
```json
{
  "message": "Verification code sent to your email",
  "cooldownSeconds": 60
}
```

### Verify Email
驗證電子郵件。

```http
POST /api/auth/verify-email
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "code": "123456"
}
```

**成功回應 (200)**:
```json
{
  "message": "Email verified successfully",
  "emailVerified": true
}
```

### Send Phone Verification Code
發送手機簡訊驗證碼。

```http
POST /api/auth/send-phone-verification
Authorization: Bearer <access_token>
```

**成功回應 (200)**:
```json
{
  "message": "Verification code sent to your phone",
  "cooldownSeconds": 60
}
```

### Verify Phone
驗證手機號碼。

```http
POST /api/auth/verify-phone
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "code": "123456"
}
```

**成功回應 (200)**:
```json
{
  "message": "Phone number verified successfully",
  "phoneNumberVerified": true
}
```

## User Endpoints

### Get Current User
取得當前已認證使用者的完整資訊。

```http
GET /api/users/me
Authorization: Bearer <access_token>
```

**成功回應 (200)**:
```json
{
  "id": 1234567890123456,
  "email": "user@example.com",
  "username": "User Name",
  "phoneNumber": "+886912345678",
  "emailVerified": true,
  "phoneNumberVerified": true,
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": "2026-01-22T00:00:00Z"
}
```

### Get User By ID
取得其他使用者的公開資訊（不需認證）。

```http
GET /api/users/{id}
```

**成功回應 (200)**:
```json
{
  "id": 1234567890123456,
  "username": "User Name",
  "createdAt": "2026-01-01T00:00:00Z"
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

**Refresh Token 成功 (200)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "new_base64_encoded_token",
  "expiresAt": "2026-01-06T10:30:00Z",
  "tokenType": "Bearer"
}
```