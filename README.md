# BusBooking System

Modern web-based bus reservation platform built with **ASP.NET MVC** + **SQL Server**

## ✨ Features

- **Role-based access control** (Admin / Manager / Customer)
- User registration & login (with email confirmation option)
- Browse available bus routes & schedules
- Search buses by:
  - Departure / Arrival city
  - Travel date
  - Number of passengers
- Real-time seat selection (seat map UI)
- Multiple payment gateway integration points (placeholder/stripe/payfast/paystack/etc.)
- Booking history & PDF ticket download
- Admin panel:
  - Manage buses, routes, schedules
  - Add/edit bus types & seat layouts
  - View & manage all bookings
  - Revenue reports & basic analytics
  - User management (block/unblock, role assignment)
- Responsive design (mobile + desktop)

## 🏗 Tech Stack

| Layer             | Technology                          | Version / Notes                     |
|-------------------|-------------------------------------|-------------------------------------|
| Backend           | ASP.NET MVC                         | .NET 6 / .NET 8                     |
| Frontend          | Razor Views + Bootstrap 5           | + partial AJAX where needed         |
| Database          | Microsoft SQL Server                | 2019 / 2022 / Azure SQL compatible  |
| ORM               | Entity Framework Core               | Code-First                          |
| Authentication    | ASP.NET Core Identity               | + Roles & Claims                    |
| Styling           | Bootstrap 5 + Font Awesome          | Custom theme overrides possible     |
| JavaScript        | jQuery + vanilla JS                 | (consider adding htmx or Alpine.js) |
| PDF Generation    | iText7 / Rotativa / DinkToPdf       | for ticket & invoice                |
| Environment       | Local / IIS / Azure App Service     |                                     |




### Prerequisites

- .NET 8.0 SDK (or .NET 6.0)
- SQL Server 2019+ / SQL Express / Azure SQL
- Visual Studio 2022 (recommended) or VS Code + C# Dev Kit
