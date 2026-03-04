# Centralized Communication Service (Email + SMS)

AdvansE-Alert is a centralized email and SMS service designed to serve all websites and applications within the group. It provides a production-ready ASP.NET MVC architecture for managing and sending notifications efficiently across multiple channels and providers.

## 🚀 Features

- **Multi-Channel**: Supports Email (SMTP) and SMS (Twilio, Hubtel).
- **Template Engine**: Dynamic placeholder replacement (e.g., `{{Name}}`).
- **Smart Routing**: Application-specific provider resolution with priority-based fallback.
- **Secure API**: API Key authentication per application.
- **Admin Dashboard**: Premium UI for managing apps, configurations, templates, and logs.
- **Encryption**: Secure storage of SMTP passwords and API keys.

## 🛠️ Tech Stack

- ASP.NET MVC (.NET 8)
- Entity Framework Core (SQL Server)
- MailKit (Email)
- Twilio & Hubtel (SMS)
- Serilog (Logging)
- BCrypt (API Key Hashing)

## 📋 Prerequisites

- .NET 8 SDK
- SQL Server (or Docker instance)
- SMTP Server (e.g., Gmail, SendGrid)
- Twilio/Hubtel Account (for SMS)

## ⚙️ Setup Instructions

1. **Clone the repository.**
2. **Configure Database**: Update the `ConnectionStrings:DefaultConnection` in `appsettings.json`.
   > [!NOTE]
   > On Mac/Linux, ensure you use a standard SQL Server instance as LocalDB is not supported.
3. **Database Migrations**:
   ```bash
   dotnet ef database update
   ```
4. **Run the Application**:
   ```bash
   dotnet run
   ```
5. **Access the Admin Dashboard**: Navigate to `/Admin` in your browser.

## 🔌 API Usage

### Authentication
Include the `X-API-KEY` header in all requests.

### Send Templated Email
`POST /api/communication/email/send`
```json
{
  "Recipient": "user@example.com",
  "TemplateName": "WelcomeEmail",
  "Placeholders": { "Name": "John Doe" }
}
```

### Send Unified Notification (Failover)
`POST /api/communication/notify`
```json
{
  "RecipientEmail": "user@example.com",
  "RecipientPhone": "+123456789",
  "Subject": "Alert",
  "Message": "This is a priority alert."
}
```

## 🔐 Security Note
Sensitive configuration data (passwords, keys) is encrypted using AES-256 before storage in the database.
