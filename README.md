# E-Commerce BE 🛍️

This is the backend for a full-featured e-commerce web application built with ASP.NET Core. It provides a RESTful API for managing products, users, orders, and more.

## ✨ Features

- **User Authentication & Authorization:** Secure user registration and login with JWT. Role-based access control (Admin, User).
- **Product Management:** Admins can create, read, update, and delete products.
- **Shopping Cart:** Users can add, update, and remove items from their cart.
- **Order Management:** Users can place orders and view their order history. Admins can manage all orders.
- **Search & Pagination:** Easily search for products and navigate through pages of results.

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
5. **Update the database connection string**
   - Open `appsettings.json` and update the `DefaultConnection` string with your SQL Server credentials.
6. **Apply migrations**
   ```sh
   dotnet ef database update
   ```
7. **Run the application**
   ```sh
   dotnet run
   ```

## 📂 Project Structure

```
E-Commerce-BE/
├── Controllers/      # API controllers
├── Models/           # Data models and DTOs
├── Services/         # Business logic and services
├── Views/            # Razor views
├── wwwroot/          # Static assets (CSS, JS, images)
├── Migrations/       # Database migrations
├── Properties/       # Launch settings
├── appsettings.json  # Application configuration
└── Program.cs        # Main application entry point
```

## 📸 Screenshots

*(Add screenshots of your application here)*

## 🤝 Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.
