# User Directory - .NET 8 Web API + Google OIDC Authentication

This project is the **backend API** for the User Directory application.

It provides:

- .NET 8 ASP.NET Core Web API
- CRUD endpoints for users
- Entity Framework Core 8
- SQLite database at `data/app.db`
- Swagger / OpenAPI
- CORS for the React development server
- Google OpenID Connect JWT bearer validation
- Authorization policy with an optional Google-email allow-list

The React frontend is a separate project. Both services run independently.

---

# 1. Architecture

```text
                Google
                  |
             OIDC Sign-In
                  |
                  v
          React SPA :5173
                  |
       Authorization: Bearer JWT
                  |
                  v
          .NET API :7080
                  |
       JWT validation + authorization
                  |
                  v
             SQLite DB
          data/app.db
```

The API protects **all `/api/users` endpoints**. The list is not public in this sample; the user must authenticate with Google before reading or modifying users.

---

# 2. API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/users` | Get all users |
| GET | `/api/users/{id}` | Get one user |
| POST | `/api/users` | Add user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |

All endpoints require a valid Google bearer credential.

---

# 3. Google authentication - what we implemented

The frontend signs the user in with Google Identity Services. Google returns an OpenID Connect **ID token (JWT)**.

React sends that credential to this API as:

```http
Authorization: Bearer <google-id-token>
```

The .NET API validates:

- JWT signature using Google's OpenID Connect metadata
- issuer
- audience (the Google Web Client ID)
- token lifetime/expiration

ASP.NET Core JWT bearer authentication is designed to validate signature, issuer, audience and token expiration before accepting a JWT. citehttps://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0

> Important: this sample uses the Google OIDC **ID token as the bearer credential for this first-party API**. This is a deliberately simple architecture for the User Directory demo. For a larger production platform with multiple APIs, delegated permissions, or service-to-service access, use a dedicated authorization server/API access-token model.

---

# 4. Google Cloud setup - complete step-by-step

The Google Cloud configuration is shared by the React and .NET services.

Google's current Sign in with Google setup requires an OAuth 2.0 Client ID and configuration of the application's authorized JavaScript origins. citehttps://developers.google.com/identity/gsi/web/guides/get-google-api-clientid

## Step 4.1 - Create/select Google Cloud project

Open:

https://console.cloud.google.com/

1. Click **Select a project**.
2. Click **New Project**.
3. Create:

```text
Project name: User Directory
```

4. Select the project.

## Step 4.2 - Configure Google Auth Platform

Open:

**Google Auth Platform**

Click **Get started** if the platform has not been configured.

Set:

```text
App name: User Directory
User support email: your Google account
Audience: External
Contact email: your Google account
```

Click **Create**.

For an External application in testing mode, add the Google accounts that should be allowed to test the application to the test-user list.

## Step 4.3 - Create the Web OAuth Client

Open:

**Google Auth Platform -> Clients**

Click **Create OAuth client**.

Choose:

```text
Application type: Web application
Name: UserDirectory-React
```

Under **Authorized JavaScript origins**, add:

```text
http://localhost:5173
```

For this React `GoogleLogin` implementation, no redirect URI is required.

Create the client.

Google will give you a Client ID similar to:

```text
123456789012-xxxxxxxxxxxxxxxx.apps.googleusercontent.com
```

Copy that Client ID. It is used in both applications.

### Do not put the Client Secret in React

The browser only needs the OAuth Web Client ID. Never place a client secret in React source code or `.env` variables that are shipped to the browser.

---

# 5. Configure the .NET API

Open:

```text
UserDirectory.Api/appsettings.json
```

Find:

```json
"Google": {
  "ClientId": "YOUR_GOOGLE_WEB_CLIENT_ID.apps.googleusercontent.com"
}
```

Replace the placeholder with the Client ID from Google Cloud.

Example:

```json
"Google": {
  "ClientId": "123456789012-xxxxxxxxxxxxxxxx.apps.googleusercontent.com"
}
```

The **same Client ID** must be configured in React's `.env`.

---

# 6. Authorization - restricting which Google users may use the API

Authentication means the API knows that Google authenticated the user.

Authorization determines whether that authenticated user is allowed to use the API.

This project contains the `GoogleUser` authorization policy.

## Allow any authenticated Google account

The default configuration is:

```json
"Authorization": {
  "AllowedEmails": []
}
```

An empty list means any successfully authenticated Google account is allowed.

## Restrict access to selected users

Example:

```json
"Authorization": {
  "AllowedEmails": [
    "admin@example.com",
    "user@example.com"
  ]
}
```

Only those Google accounts can access the protected CRUD endpoints.

This is useful for a small internal application. For larger applications, use a proper role/permission model rather than maintaining a simple email list.

---

# 7. How authentication works in the API

The relevant configuration is in `Program.cs`.

The API uses:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);
```

Google is configured as the authority:

```text
https://accounts.google.com
```

The configured Web Client ID is used as the expected audience.

The API also enables:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

The controller uses the `GoogleUser` policy to require an authenticated and authorized Google user.

---

# 8. CORS configuration

The API allows the React development origin:

```text
http://localhost:5173
```

Configured in `Program.cs`:

```text
WithOrigins("http://localhost:5173")
```

Do not change this to `AllowAnyOrigin()` for production.

When deploying the frontend, replace the localhost origin with the real frontend URL.

---

# 9. SQLite database

The database connection is:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=data/app.db"
}
```

The API creates the `data` directory and uses:

```text
data/app.db
```

The application currently uses `Database.EnsureCreated()` for simple setup.

---

# 10. Run the .NET API

Open a terminal in the `UserDirectory.Api` folder.

## Restore packages

```powershell
dotnet restore
```

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run
```

Expected URLs:

```text
API:     https://localhost:7080
Swagger: https://localhost:7080/swagger
```

Keep the API terminal running while using the React application.

---

# 11. HTTPS development certificate

If the browser reports an HTTPS certificate problem, run:

```powershell
dotnet dev-certs https --trust
```

Then restart the API.

---

# 12. Test the API with Swagger

Open:

```text
https://localhost:7080/swagger
```

You should see:

```text
GET    /api/users
POST   /api/users
GET    /api/users/{id}
PUT    /api/users/{id}
DELETE /api/users/{id}
```

Because the API is protected, direct Swagger calls require a valid bearer credential if Swagger authentication is configured for the environment.

The easiest end-to-end test is to sign in through the React application and let React send the bearer credential automatically.

---

# 13. End-to-end request flow

```text
1. User opens React
        |
        v
2. Clicks Sign in with Google
        |
        v
3. Google authenticates user
        |
        v
4. Google returns ID token JWT
        |
        v
5. React stores token in sessionStorage
        |
        v
6. React calls .NET API
        |
        | Authorization: Bearer <JWT>
        v
7. ASP.NET Core JWT middleware validates token
        |
        +--> signature
        +--> issuer
        +--> audience
        +--> expiration
        |
        v
8. GoogleUser authorization policy runs
        |
        +--> authenticated user allowed?
        |
        v
9. Controller executes CRUD operation
        |
        v
10. EF Core -> SQLite data/app.db
```

---

# 14. Common errors and fixes

## `Failed to fetch` in React

Make sure the API is running:

```text
https://localhost:7080/swagger
```

If Swagger does not open, start the API first.

## `address already in use` / port 7080

Another process is already using port 7080.

Find it:

```cmd
netstat -ano | findstr :7080
```

Then identify the process:

```cmd
tasklist | findstr <PID>
```

Stop it if appropriate:

```cmd
taskkill /PID <PID> /F
```

Then run the API again.

## `401 Unauthorized`

Check:

- React successfully signed in with Google.
- React sends `Authorization: Bearer ...`.
- React and API use the same Google Web Client ID.
- `Google:ClientId` is correct in `appsettings.json`.
- The token is valid and not expired.

## `403 Forbidden`

If `AllowedEmails` is configured, make sure the signed-in Google account's email is included.

## Google `origin_mismatch`

Verify Google Cloud has:

```text
http://localhost:5173
```

under **Authorized JavaScript origins**.

## API returns 404 at `/`

That is normal if no root endpoint is defined.

Use:

```text
https://localhost:7080/swagger
```

or:

```text
https://localhost:7080/api/users
```

with authentication.

---

# 15. Production checklist

Before production deployment:

- Use HTTPS for frontend and API.
- Add the production frontend origin to Google Cloud.
- Change the API CORS origin from localhost to the production frontend URL.
- Do not commit secrets or environment-specific configuration.
- Use a stronger authorization model for roles and permissions.
- Consider a dedicated authorization server/API access-token model when multiple APIs or service-to-service calls are introduced.
- Replace `EnsureCreated()` with EF Core migrations for managed schema evolution.
- Review token storage and browser security/XSS protections.
- Configure logging and monitoring.

---

# 16. Technology stack

- .NET 8 ASP.NET Core Web API
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- Entity Framework Core 8
- SQLite
- Swagger / OpenAPI
- Google OpenID Connect / JWT

---

# 17. Related frontend project

The companion frontend is:

```text
UserDirectory-React-Google-Auth
```

Configure the **same Google Web Client ID** in its `.env` file and run it on:

```text
http://localhost:5173
```
