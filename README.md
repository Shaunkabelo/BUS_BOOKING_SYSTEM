# Mzansi Bus — Bus Booking System

A personal ASP.NET Core MVC (.NET 8) project with SQL Server, Dapper and Razor views.
The customer journey takes guidance from Computicket's search → service selection → seats → checkout flow, with original branding and fictional demo operators.

## Start here

1. Install/use Visual Studio 2022 with ASP.NET development, .NET 8 SDK, SQL Server Express and SQL Server Management Studio (SSMS).
2. Connect to your SQL instance in SSMS (on this computer: `.\SQLEXPRESS`).
3. Open **`Database/Setup.sql`** and run the entire file. It creates **BusBooking**, tables, indexes, four sample buses and 30 days of departures. Re-running preserves existing data and extends sample dates.
4. Check `ConnectionStrings:conn` in `Bus_Booking_System/appsettings.json`. The default is:

   ```text
   Server=.\SQLEXPRESS;Database=BusBooking;Trusted_Connection=True;TrustServerCertificate=true;Connect Timeout=5
   ```

   Change `Server` if using a different SQL instance. Windows authentication uses the account running the app. `TrustServerCertificate=true` is for local development.
5. Open **`Bus_Booking_System/Bus_Booking_System.sln`**, set **Bus_Booking_System** as startup project, and press F5. Alternatively, from the repository root:

   ```powershell
   dotnet run --project Bus_Booking_System --launch-profile http
   ```

6. Open **http://localhost:5188**, create an account, and search **Johannesburg → Durban tomorrow**. Choose seats, enter passenger names and confirm the demo booking.

If the database is absent or unreachable, the home page explains setup instead of opening a broken admin page.

## Become an administrator

Register normally, then run this in SSMS, replacing the email with your own:

```sql
USE BusBooking;
UPDATE travel.Users SET Role = 'Admin' WHERE Email = 'you@example.com';
```

Sign out and back in. **Administration** now appears in the navigation. There is no shared/default administrator password. New registrations always receive the Customer role. Role changes take effect at the next sign-in; existing cookies last up to eight hours.

## Implemented

- Registration, hashed passwords, cookie sign-in/out, customer/admin authorization, account request rate limiting.
- Search by cities, date and 1–6 passengers; prices in ZAR, times in SAST (UTC+2).
- Trip results with departure/arrival, coach type, per-person fare and available seats.
- Seat map and passenger names, validated again on the server.
- Transactional demo checkout, server-calculated totals, repeat-submission protection and a unique database constraint against double-booked seats.
- Customer-owned booking history and printable tickets. **Print / Save as PDF** uses the browser print dialog.
- Cancellation before departure, releasing seats while retaining booking history.
- Admin booking overview and demo-value totals; add/edit/delete buses; add/list/delete departures. Buses with schedules and departures with booking history cannot be deleted. Overlapping trips for a bus are rejected, and capacity cannot change once schedules exist.
- Responsive customer and admin views, server-side validation, antiforgery protection on all mutation forms, database setup page.

## Demo boundaries

This is a functioning local demonstration, **not a live travel service**. Operators, fares and services are fictional. Checkout does not collect payment or card details. Tickets clearly say they are not valid for travel. All passengers use the same flat fare.

Real payments, carrier APIs, email delivery/password recovery, return-trip checkout, fare classes, downloadable server-generated PDFs, user-management screens and production deployment are not implemented. For real payment integration, confirm bookings only after a verified provider callback; add expiring seat holds and payment/refund records first. Do not change the demo confirmation label to “Paid.”

## Project layout

- `Database/Setup.sql`: repeatable schema and sample services.
- `Bus_Booking_System/Models/TravelModels.cs`: active input and display models.
- `Bus_Booking_System/Services/TravelStore.cs`: active parameterized Dapper queries and transactional booking logic.
- `Bus_Booking_System/Controllers`: customer, account and admin flows.
- `Bus_Booking_System/Views` and `wwwroot/css/travel.css`: Razor screens and responsive styling.
- `Bus_Booking_Library`: retained original domain/repository experiments. The new flow uses the `travel` schema via `TravelStore`; the old `sp_InsertBus` / `sp_UpdateBus` procedures are no longer required. Original unused layouts and login views remain for reference.
- `Bus_Booking_Verification`: executable integration checks, without a separate test-framework dependency.

## Verification

From the repository root:

```powershell
dotnet build Bus_Booking_System/Bus_Booking_System.sln
dotnet run --project Bus_Booking_Verification
```

The verification runner requires Windows-authenticated access to local SQL Express with permission to create/drop a database. It creates a uniquely named `BusBooking_Verification_<guid>` database, runs setup twice, starts a private local test server, tests HTTP forms and database behavior, then drops only its disposable database and stops its server. It does not create or modify your `BusBooking` database. To choose another local SQL instance, set `BOOKING_TEST_SERVER`.

Checks cover registration/login, role restrictions, antiforgery enforcement, search, checkout, server pricing, repeat submissions, ticket ownership, concurrent seat requests, invalid seats, cancellation, admin bus creation and schedule conflicts.

Manual visual check: try desktop and narrow/mobile widths; select two seats; print a ticket; confirm a cancelled ticket is labelled Cancelled. Browser screenshots are not part of the automated checks.

## Initial scan

The original solution compiled but had a placeholder customer homepage, login forms pointing to a missing Account controller, dummy edit-bus data, missing delete/update actions and no SQL scripts. Authentication and customer bookings were not implemented. The previous README described planned features rather than working functionality. The old library still has pre-existing nullable-reference warnings on a clean build.

Reference: [Computicket bus booking](https://computicket.com/travel/bus/booking), [Computicket booking guidance](https://ctkcontactcentre.freshdesk.com/support/solutions/articles/43000778489-how-to-book-a-bus-ticket).
