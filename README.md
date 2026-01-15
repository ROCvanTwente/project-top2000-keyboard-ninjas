# Project Top2000 - Keyboard Ninjas

Dit project bevat een ASP.NET Core Web API met JWT authenticatie, Identity, rollen, refresh tokens, CORS configuratie en SQL Server database.

**🎯 Gebouwd met .NET 10.0**

## Repository Structuur

```
project-top2000-keyboard-ninjas/
├── README.md                       # Dit bestand
├── TemplateJwtProject.slnx         # Solution file
├── .gitignore                      # Git ignore configuratie
├── .gitattributes                  # Git attributes
└── TemplateJwtProject/             # Hoofdproject
    ├── Program.cs                  # Applicatie entry point & configuratie
    ├── TemplateJwtProject.csproj   # Project bestand
    ├── TemplateJwtProject.http     # HTTP test requests
    ├── appsettings.json            # Applicatie configuratie
    ├── appsettings.Development.json # Development configuratie
    ├── NuGet.Config                # NuGet configuratie
    ├── Constants/
    │   └── Roles.cs                # Rol constanten (Admin, User)
    ├── Controllers/
    │   ├── AuthController.cs       # Login/Register/Refresh endpoints
    │   ├── AdminController.cs      # Admin-only endpoints
    │   └── TestController.cs       # Voorbeeld rol-gebaseerde endpoints
    ├── Data/
    │   └── AppDbContext.cs         # Entity Framework DbContext
    ├── Docs/
    │   ├── README.md               # Gedetailleerde technische documentatie
    │   ├── ADMIN_SETUP.md          # Guide voor admin gebruiker
    │   └── REFRESH_TOKENS.md       # Refresh token documentatie
    ├── Migrations/                 # Database migraties
    ├── Models/
    │   ├── ApplicationUser.cs      # Custom Identity User
    │   ├── RefreshToken.cs         # Refresh token model
    │   └── DTOs/
    │       ├── RegisterDto.cs
    │       ├── LoginDto.cs
    │       ├── AuthResponseDto.cs
    │       ├── RefreshTokenDto.cs
    │       └── AssignRoleDto.cs
    ├── Properties/
    │   └── launchSettings.json     # Launch configuratie
    └── Services/
        ├── JwtService.cs           # JWT token generatie
        ├── RefreshTokenService.cs  # Refresh token beheer
        └── RoleInitializer.cs      # Initialiseer rollen bij startup
```

## Features

- ✅ ASP.NET Core Identity met `ApplicationUser`
- ✅ JWT Bearer Token authenticatie
- ✅ Refresh Tokens voor automatische token renewal
- ✅ Role-based Authorization (User & Admin rollen)
- ✅ CORS configuratie voor frontend integratie
- ✅ SQL Server database met Entity Framework Core
- ✅ Register & Login endpoints
- ✅ Admin endpoints voor gebruikersbeheer
- ✅ Token revocation & logout from all devices

## Vereisten

- .NET 10.0 SDK
- SQL Server (lokaal of Azure)

## Aan de slag

1. **Clone de repository**
   ```bash
   git clone https://github.com/ROCvanTwente/project-top2000-keyboard-ninjas.git
   cd project-top2000-keyboard-ninjas
   ```

2. **Configureer de database connection string**
   
   Pas `appsettings.json` aan met je eigen SQL Server connectie:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=MyProject;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

3. **Voer database migraties uit**
   ```bash
   cd TemplateJwtProject
   dotnet ef database update
   ```

4. **Start de applicatie**
   ```bash
   dotnet run
   ```

De API draait standaard op `https://localhost:7003`

## API Endpoints

| Endpoint | Methode | Beschrijving | Authenticatie |
|----------|---------|--------------|---------------|
| `/api/auth/register` | POST | Nieuwe gebruiker registreren | Publiek |
| `/api/auth/login` | POST | Inloggen | Publiek |
| `/api/auth/refresh-token` | POST | Nieuwe access token | Publiek |
| `/api/auth/revoke-token` | POST | Token intrekken | JWT |
| `/api/auth/logout-all` | POST | Uitloggen op alle apparaten | JWT |
| `/api/admin/users` | GET | Alle gebruikers | Admin |
| `/api/admin/assign-role` | POST | Rol toewijzen | Admin |
| `/api/admin/remove-role` | POST | Rol verwijderen | Admin |
| `/api/test/user` | GET | Test User endpoint | User rol |
| `/api/test/admin` | GET | Test Admin endpoint | Admin rol |

## Documentatie

Zie de `TemplateJwtProject/Docs/` map voor gedetailleerde documentatie:

- [README.md](TemplateJwtProject/Docs/README.md) - Uitgebreide technische documentatie
- [ADMIN_SETUP.md](TemplateJwtProject/Docs/ADMIN_SETUP.md) - Admin gebruiker setup
- [REFRESH_TOKENS.md](TemplateJwtProject/Docs/REFRESH_TOKENS.md) - Refresh token implementatie

## NuGet Packages

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (10.0.0)
- `Microsoft.EntityFrameworkCore.SqlServer` (10.0.0)
- `Microsoft.EntityFrameworkCore.Tools` (10.0.0)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.0)
- `Microsoft.AspNetCore.OpenApi` (10.0.0)

## Licentie

Dit project is gemaakt voor educatieve doeleinden bij ROC van Twente.
