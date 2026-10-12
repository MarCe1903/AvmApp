# AvmApp

A shopping application with an Angular frontend and an ASP.NET Core Web API. It includes account registration, product browsing, favorites, purchases, order tracking, and customer/seller chat. Google sign-in and email-based MFA require your own provider settings.

## Requirements

- .NET 9 SDK and the .NET 9 ASP.NET Core runtime.
- Node.js 24 and npm. The checked-in Angular CLI also supports Node.js 20.19+ or 22.12+ within those major versions.
- SQL Server. The Windows instructions below use SQL Server Express LocalDB, which must be installed separately.
- A SQL query tool, such as SQL Server Management Studio, for the category and sample-product setup.

The main walkthrough uses PowerShell on Windows. Linux and macOS users need a reachable SQL Server instance and should use the alternative connection/environment settings below.

## 1. Download the project

Download and extract the source ZIP, or clone the repository using its GitHub URL. Open a terminal in the project root: the folder containing `backend`, `frontend`, and this README.

Check the installed tools:

```powershell
dotnet --list-sdks
dotnet --list-runtimes
node --version
npm --version
```

## 2. Create your private settings

From the project root, copy the configuration template:

```powershell
Copy-Item backend/Project4.WebAPI/appsettings.Development.example.json backend/Project4.WebAPI/appsettings.Development.json
```

Open the new `appsettings.Development.json` file. This file is ignored by Git; keep the tracked example file empty of real credentials.

For Windows LocalDB, set `ConnectionStrings:DefaultConnection` to:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AvmAppDev;Trusted_Connection=True;TrustServerCertificate=True"
```

Use a new database name that does not already contain tables. The API creates its core tables at first startup and applies additional schema changes. It does not include a full migration workflow for an existing Northwind database.

Start LocalDB if necessary:

```powershell
sqllocaldb start MSSQLLocalDB
```

Generate a random JWT signing key:

```powershell
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
```

Paste the generated value into `Jwt:Secret` in your private settings. The application requires at least 32 characters. Keep the key private.

### Using another SQL Server instance

Replace the connection string with one appropriate for your server. For example, the following is a template for local SQL authentication; replace both placeholders in the private file:

```json
"DefaultConnection": "Server=localhost,1433;Database=AvmAppDev;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=True;TrustServerCertificate=True"
```

The account must have permission to create the development database and modify its schema. `TrustServerCertificate=True` is for local development; use a properly validated server certificate for deployment.

## 3. Configure the product database connection

The API uses two database contexts. Authentication and purchases read the JSON connection string; the product and order-status repositories read `AVMAPP_NORTHWIND_CONNECTION`. Point both at the **same development database**.

In the PowerShell terminal where you will start the API:

```powershell
$localSettings = Get-Content backend/Project4.WebAPI/appsettings.Development.json -Raw | ConvertFrom-Json
$env:AVMAPP_NORTHWIND_CONNECTION = $localSettings.ConnectionStrings.DefaultConnection
```

This environment variable lasts for the current terminal session. Set it again when opening a new backend terminal. ConsoleUI uses the same variable.

For Bash on Linux or macOS, export your actual connection string before starting the API:

```bash
export AVMAPP_NORTHWIND_CONNECTION='YOUR_SQL_SERVER_CONNECTION_STRING'
```

## 4. Start the backend

Trust the local HTTPS certificate:

```powershell
dotnet dev-certs https --trust
```

Certificate trust may need additional browser or operating-system configuration on Linux.

Restore dependencies and start the API from the project root:

```powershell
dotnet restore backend/YazilimEgitim.sln
dotnet run --project backend/Project4.WebAPI --launch-profile https
```

Leave this terminal running. The API listens at `https://localhost:7026`. Its first startup creates the application tables and the initial order statuses. An empty database will not contain products yet.

## 5. Add a category and a sample product

The current API does not automatically create the `Categories` table, which the product-details endpoint needs. After the first successful backend startup, connect your SQL query tool to the same SQL Server instance and execute the following against `AvmAppDev`. If you chose another database name, replace the first line.

```sql
USE [AvmAppDev];
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        CategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryName = N'Demo')
    INSERT INTO dbo.Categories (CategoryName) VALUES (N'Demo');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Demo Product')
BEGIN
    INSERT INTO dbo.Products
        (CategoryId, ProductName, QuantityPerUnit, UnitPrice, UnitsInStock)
    SELECT TOP (1) CategoryId, N'Demo Product', N'1 item', 100.00, 10
    FROM dbo.Categories
    WHERE CategoryName = N'Demo';
END;
```

This prepares the catalog used by the Angular interface. The older Orders/Employees/Customers API endpoints use additional Northwind-style tables that this minimal setup does not supply.

Check the catalog by opening `https://localhost:7026/api/products/getproductdetails` in your browser. It should return a successful JSON response containing the sample product. Resolve any certificate warning by trusting the development certificate before using the frontend.

## 6. Start the frontend

Open a second terminal in the project root:

```powershell
cd frontend
npm ci
npm start
```

Open **http://localhost:4200/** in your browser. Keep both terminals running. Register an account, sign in, and browse the sample product.

The frontend already targets `https://localhost:7026/api` in `frontend/src/environments/environment.ts`. Its address and the backend launch profile must match. The backend allows the local frontend origins on port 4200.

## Optional features

### Google sign-in

In your own Google OAuth web application, configure the authorized redirect URI as:

```text
https://localhost:7026/signin-google
```

Put your own `ClientId` and `ClientSecret` under `Authentication:Google` in the private settings. Leave these values empty if you only use email/password registration and login; do not use the Google sign-in button until configured.

### Email and MFA

Fill in `MailSettings:Host`, `Port`, `UserName`, `Password`, `FromEmail`, and `FromName` using your SMTP provider's settings. The email service uses SSL/TLS. Configure and verify delivery before enabling MFA in your profile; MFA login requires a working email service.

`MailSettings:OrderStatusRecipient` is an optional recipient for status-definition update notifications. Leave it empty to skip those notifications. MFA messages are sent to the signing-in user's email address.

### Administrator account

To create a local administrator, fill in `SeedAdmin:Email`, `Password`, and `FullName` in your private settings before starting the API. Remove these settings after the account is created: the startup code reapplies the configured administrator password on every restart.

## Troubleshooting

| Problem | What to check |
| --- | --- |
| Missing database connection or JWT key | Set `DefaultConnection` and `Jwt:Secret` in the private development settings. Start with the provided development launch profile. |
| `Set AVMAPP_NORTHWIND_CONNECTION` error | Set the variable in the same terminal that launches the backend. |
| SQL connection or permission error | Start SQL Server/LocalDB, check the instance name and credentials, and allow development database creation/schema changes. |
| `Invalid object name 'Categories'` | Run the category setup SQL against the same database used by both contexts. |
| Empty product list | Insert the sample product and verify the product-details endpoint. |
| Browser network/certificate error | Trust the HTTPS certificate, keep the API running, and check the frontend API URL. |
| MFA email does not arrive | Check SMTP credentials, sender permissions, port, and TLS support. |
| Port already in use | Stop the conflicting process, or update backend/frontend addresses and CORS origins together. |

## Build checks and current limitations

```powershell
dotnet build backend/YazilimEgitim.sln -m:1
cd frontend
npm run build -- --configuration development
```

The backend and frontend development builds have been checked. The backend has existing compiler warnings. The default frontend production build (`npm run build`) currently fails because `product-list.css` exceeds the configured 10 kB component-style budget. Resolve that before preparing a production deployment.

Local database, SMTP, and Google OAuth connections depend on your own environment and credentials. This guide describes the current setup; these external connections were not verified as part of the source privacy review.

## Keeping private settings out of Git

Keep credentials in ignored local settings, .NET user-secrets, or your deployment platform's secret store. The repository excludes private settings, keys, certificates, database files, logs, and build outputs. Review staged files before publishing: ignore rules do not remove files from earlier commits.

For deployment, configuration can use environment variables such as `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Authentication__Google__ClientSecret`, and `MailSettings__Password`. `AVMAPP_NORTHWIND_CONNECTION` is also required by the product repositories. Configure the public frontend/API URLs, HTTPS, CORS, and access controls for your deployment.

The public source starts with a single anonymized initial commit. Earlier development commits contained personal author information and credentials. Rotate those historical credentials before reuse: replacing Git history does not revoke credentials or remove existing clones, forks, or cached copies. Keep future commit author details anonymous if anonymity is required. If using the source archive, initialize a new repository from the extracted files rather than importing the old development history.
