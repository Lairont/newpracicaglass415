# FitStock

ASP.NET Core MVC application for a fitness club with client memberships and a sports nutrition shop.

## PostgreSQL setup

PostgreSQL is required. The app has no in-memory or demo-data fallback. Create a database in pgAdmin, run `sql/fitstock_schema.sql` in that database, and then configure the connection string with new credentials in User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=practic17;Username=postgres;Password=YOUR_PASSWORD"
```

The schema script creates the FitStock tables and records the migration version so startup will not try to recreate them. It does not insert products or accounts. To populate a first presentation database with clearly marked sample products, movements, and orders, optionally run `sql/fitstock_demo_data.sql` once. The app applies future EF Core migrations at startup.

## Authentication

The first account registers as the administrator. Later public registrations create regular client accounts with full name, phone, email/login, and password. A manager or administrator can also issue a client account with a temporary password and a membership plan. Passwords are hashed with ASP.NET Core's password hasher.

The first account registered in an empty database becomes the administrator. The admin panel lets administrators assign `User`, `Manager`, and `Admin` roles. Clients see their membership dates and an in-app warning one day before expiration. Staff see expired memberships marked as debtors. Only staff can change a client's legal name, phone, and membership; a client can change their login nickname, password, and avatar.

Google sign-in and email confirmation are not configured. The OAuth credentials committed in the public reference repository must be revoked and replaced before either feature is enabled.
