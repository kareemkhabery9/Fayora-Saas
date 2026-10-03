# FayoraTravel SaaS — Auth API Integration

All JSON requests use `Content-Type: application/json`. Every login, refresh, and logout request must include a stable device header:

```http
X-Device-Id: a-unique-id-per-browser-or-device
```

## Company auth

| Action | Method and route | Request body | Result |
| --- | --- | --- | --- |
| Register | `POST /api/company-auth/register/email` | `companyName`, `ownerFirstName`, `ownerLastName`, `email`, `phoneNumber`, `password` | Creates an unverified company and sends a six-digit verification code. |
| Verify email | `POST /api/company-auth/email/verify` | `email`, `code` | `204 No Content` on success. |
| Resend verification | `POST /api/company-auth/email/resend-verification` | `email` | Always `204`; code resend is limited. |
| Login with email | `POST /api/company-auth/login/email` | `email`, `password`, `deviceLanguage` | Returns company data, access token, refresh token. |
| Login with Google | `POST /api/company-auth/login/google` | Existing Google-login request body | Returns company data and tokens. |
| Forgot password | `POST /api/company-auth/password/forgot` | `email` | Always `204`; sends a six-digit code only for an eligible company account. |
| Reset password | `POST /api/company-auth/password/reset` | `email`, `code`, `newPassword` | `204`; invalidates every existing refresh-token session. |
| Refresh session | `POST /api/company-auth/token/refresh` | `refreshToken` | Returns a rotated access and refresh token. |
| Logout | `POST /api/company-auth/logout` | None | Requires company Bearer token; invalidates the current device session. |

`deviceLanguage` is an enum string such as `Arabic` or `English`.

## Super-admin auth

The super-admin UI is separate from the company login UI. It uses the same request shapes but a separate route family:

```text
POST /api/super-admin/auth/login/email
POST /api/super-admin/auth/password/forgot
POST /api/super-admin/auth/password/reset
POST /api/super-admin/auth/token/refresh
POST /api/super-admin/auth/logout
```

There is intentionally no public super-admin registration endpoint. The first account is created only by `BootstrapSuperAdmin` at startup.

## Tokens and protected APIs

Send the access token with protected requests:

```http
Authorization: Bearer <accessToken>
```

The access token expires after one hour. When it expires, call the appropriate `token/refresh` endpoint using the refresh token and the *same* `X-Device-Id`; replace both tokens with the response. If refresh fails, clear local auth state and redirect to the matching login page.

Do not place passwords, access tokens, refresh tokens, SMTP passwords, or service keys in source files or Git. The local backend settings belong in User Secrets; production settings belong in the host's secret/environment configuration.

## Password and code rules

- Password: at least 8 characters with uppercase, lowercase, number, and special character.
- Verification and reset code: six digits, valid for 5 minutes, maximum 3 attempts.
- Resending a code is limited by a 2-minute cooldown.
- Password reset revokes all sessions of that account.

## Email prerequisite

Email verification and password recovery need SMTP settings before messages can be delivered:

```powershell
dotnet user-secrets set "EmailSettings:Sender" "your-sender@gmail.com" --project Fayora.Api
dotnet user-secrets set "EmailSettings:Password" "YOUR_GOOGLE_APP_PASSWORD" --project Fayora.Api
```

Use a Gmail App Password, not the ordinary Gmail password.
