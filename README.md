# Montenegro Fiscalization API

A REST API implementation for electronic invoice fiscalization with Montenegro's Tax Administration system, built with .NET 8 and cloud-native architecture.

## Overview

This project provides a complete solution for businesses operating in Montenegro to comply with electronic fiscalization requirements. It handles invoice registration, cash deposit reporting, and communication with the Tax Administration's Central Information System (CIS).

## Features

### Core Functionality
- **Invoice Fiscalization** - Register cash and non-cash invoices with the Tax Administration
- **Deposit Management** - Report initial cash deposits and withdrawals
- **IIC Generation** - Create unique invoice identification codes with digital signatures
- **QR Code Support** - Generate verification QR codes for printed invoices
- **Multi-tenant Architecture** - Support multiple companies on a single deployment

### Technical Features
- RESTful API design with OpenAPI documentation
- JWT-based authentication with API key management
- Demo mode for testing without certificates
- Docker support for containerized deployment
- Health checks and monitoring endpoints
- Comprehensive error handling with localized messages

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQLite (for development) or SQL Server (for production)
- Digital certificates from Montenegro Tax Administration (for production)

### Quick Start

1. Clone the repository and navigate to the API directory:
```bash
cd MontenegroFiscalization.Api
```

2. Update configuration in `appsettings.json`:
```json
{
  "Fiscalization": {
    "TIN": "your-tin-number",
    "BusinessUnitCode": "your-business-unit",
    "TCRCode": "your-tcr-code",
    "DemoMode": true
  }
}
```

3. Run the application:
```bash
dotnet run
```

4. Access Swagger UI at `http://localhost:5189`

## Authentication

Get an authentication token using your API key:
```bash
curl -X POST http://localhost:5189/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"apiKey": "demo-api-key-12345"}'
```

Use the token in subsequent requests:
```bash
-H "Authorization: Bearer TOKEN"
```

## API Endpoints

### Fiscalization
- `POST /api/v1/fiscalization/invoice` - Fiscalize an invoice
- `POST /api/v1/fiscalization/deposit` - Register cash deposit
- `GET /api/v1/fiscalization/health` - Service health check

### Authentication
- `POST /api/v1/auth/token` - Exchange API key for JWT token
- `GET /api/v1/auth/validate` - Validate current token

### Certificate Management
- `POST /api/v1/certificate/upload` - Upload digital certificate
- `GET /api/v1/certificate/info` - Get certificate information
- `GET /api/v1/certificate/validate` - Check certificate validity

## Configuration

### Environment Variables

The application supports configuration through environment variables:
- `ASPNETCORE_ENVIRONMENT` - Development, Staging, or Production
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Fiscalization__DemoMode` - Enable/disable demo mode
- `JWT__Secret` - Secret key for JWT token generation

### Certificate Setup

For production use, place your certificates in the configured directory:
```
/app/Certificates/
├── signature.pfx
└── seal.pfx
```

## Architecture

The solution follows clean architecture principles with three main layers:

### Core Layer
Contains business logic, models, and interfaces with no external dependencies.

### Infrastructure Layer
Implements external services including:
- Tax Administration communication
- Certificate management
- Database persistence
- Authentication services

### API Layer
RESTful endpoints, request/response DTOs, and API-specific services.

## Demo Mode

Demo mode allows testing without real certificates or Tax Administration connectivity. When enabled:
- IIC codes are generated using SHA256 instead of RSA signatures
- Mock FIC codes are returned immediately
- No actual communication with Tax Administration servers
- Perfect for development and integration testing

To disable demo mode for production:
```json
{
  "Fiscalization": {
    "DemoMode": false
  }
}
```

## Deployment

### Docker

Build and run with Docker:
```bash
docker build -t montenegro-fiscalization .
docker run -p 8080:80 montenegro-fiscalization
```

### Docker Compose

For complete stack with database:
```bash
docker-compose up
```

### Production Checklist
- [ ] Obtain digital certificates from Tax Administration
- [ ] Configure production database
- [ ] Set strong JWT secret key
- [ ] Disable demo mode
- [ ] Configure SSL/TLS
- [ ] Set up monitoring and logging
- [ ] Configure backup strategy

## Testing

Run unit tests:
```bash
dotnet test
```

Test invoice fiscalization:
```bash
curl -X POST http://localhost:5189/api/v1/fiscalization/invoice \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d @test-invoice.json
```

## Error Handling

The API returns standardized error responses with appropriate HTTP status codes:
```json
{
  "success": false,
  "message": "Fiscalization failed",
  "errors": ["Certificate expired", "Invalid business unit code"]
}
```

Error codes from the Tax Administration are automatically translated and included in the response.