# E-Commerce Project 🛍️

This is the backend for a full-featured e-commerce web application built with ASP.NET Core. It provides a RESTful API for managing products, users, orders, and more.

## ✨ Features

- **User Authentication & Authorization:** Secure user registration and login with JWT. Role-based access control (Admin, User).
- **Product Management:** Admins can create, read, update, and delete products.
- **Shopping Cart:** Users can add, update, and remove items from their cart.
- **Order Management:** Users can place orders and view their order history. Admins can manage all orders.
- **Search & Pagination:** Easily search for products and navigate through pages of results.

## 🔒 **Security Features**

- **Enterprise-grade security** with OWASP compliance
- **Strong password policies** (12+ chars, complexity requirements)
- **CSRF protection** on all forms and POST actions
- **Rate limiting** to prevent brute force attacks
- **Secure file uploads** with validation and malware detection
- **Encrypted cookies** with security flags
- **Security headers** (CSP, XSS protection, etc.)
- **Account lockout** after failed attempts
- **HTTPS enforcement** with HSTS

## 🛠️ Technologies Used

- **Backend:** ASP.NET Core, C#
- **Database:** Microsoft SQL Server, Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **API:** RESTful
- **Styling:** Tailwind CSS

## 🚀 Getting Started

To get a local copy up and running follow these simple example steps.

### Prerequisites

- .NET 8 SDK
- Microsoft SQL Server
- Node.js and npm (for Tailwind CSS)

### Installation

1. **Clone the repo**

   ```sh
   git clone https://github.com/your_username/E-Commerce-BE.git
   ```

2. **Navigate to the project directory**

   ```sh
   cd E-Commerce-BE/E-Commerce-BE
   ```

3. **Install NPM packages**

   ```sh
   npm install
   ```

4. **Build Tailwind CSS**

   ```sh
   npm run build-css-once
   ```

5. **Configure security settings**

   - Copy `appsettings.json` and set your secure values
   - Set environment variables for production:
     ```bash
     ConnectionStrings__DefaultConnection="your-connection-string"
     BrevoSettings__ApiKey="your-api-key"
     StripeSettings__PublishableKey="your-publishable-key"
     StripeSettings__SecretKey="your-secret-key"
     CookieEncryptionKey="your-32-char-encryption-key"
     ```

6. **Apply migrations**

   ```sh
   dotnet ef database update
   ```

7. **Run the application**
   ```sh
   dotnet run
   ```

## 🔐 **Security Configuration**

### **Required Environment Variables**

```bash
# Database
ConnectionStrings__DefaultConnection="your-connection-string"

# Email Service (Brevo)
BrevoSettings__ApiKey="your-brevo-api-key"
BrevoSettings__SenderName="Store Name"
BrevoSettings__SenderEmail="noreply@store.com"

# Payment Gateway (Stripe)
StripeSettings__PublishableKey="your-stripe-publishable-key"
StripeSettings__SecretKey="your-stripe-secret-key"
StripeSettings__WebhookSecret="your-stripe-webhook-secret"

# Security
CookieEncryptionKey="32-character-encryption-key"
```

### **Security Features Enabled**

- ✅ CSRF Protection
- ✅ Rate Limiting
- ✅ Secure File Uploads
- ✅ Encrypted Cookies
- ✅ Security Headers
- ✅ HTTPS Enforcement
- ✅ Account Lockout
- ✅ Strong Password Policy

## 📂 Project Structure

```
E-Commerce-BE/
├── Controllers/      # API controllers with security
├── Models/           # Data models and DTOs
├── Services/         # Business logic and security services
├── Views/            # Razor views with CSRF tokens
├── wwwroot/          # Static assets (CSS, JS, images)
├── Migrations/       # Database migrations
├── Properties/       # Launch settings
├── appsettings.json  # Application configuration
├── Program.cs        # Main application entry point
└── docs/             # Security documentation
```

## 🛡️ **Security Best Practices**

- **Never commit secrets** to source control
- **Use environment variables** for sensitive configuration
- **Regular security updates** and dependency scanning
- **Monitor security logs** and failed attempts
- **Enable HTTPS** in production
- **Regular security audits** and penetration testing

## 🤝 Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### **Security Contributions**

- Report security vulnerabilities to security@yourstore.com
- Follow responsible disclosure practices
- Test security features thoroughly
- Document security improvements

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

## 🔒 **Security Documentation**

For detailed security information, see [SECURITY.md](docs/SECURITY.md)

---

**Security Level**: Enterprise Grade  
**OWASP Compliance**: Top 10 Protected  
**Last Security Audit**: December 2024
