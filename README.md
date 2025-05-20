# Classroom Reservation System

A modern, secure, and user-friendly classroom reservation system for university use. Built with ASP.NET Core Razor Pages, Entity Framework, and Bootstrap.

## Features
- **Role-based access:** Admin & Instructor roles
- **Classroom reservation with conflict and holiday checks**
- **Batch (group) reservations**
- **Feedback system (with admin email notification)**
- **Logging (database + file for errors)**
- **Bootstrap-based modern UI/UX (modals, toasts, AJAX)**
- **Pagination, filtering, and responsive design**
- **Security:** Lockout, validation, anti-XSS, CSRF
- **Unit tests for core logic**

## Technologies Used
- ASP.NET Core 8 (Razor Pages)
- Entity Framework Core
- Microsoft Identity (role & user management)
- Bootstrap 5
- SQL Server (or LocalDB)
- xUnit (unit testing)

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (or LocalDB)

### Installation
1. **Clone the repository:**
   ```bash
   git clone <repo-url>
   cd UniClassReserve
   ```
2. **Configure the database:**
   - Edit `UniClassReserve/appsettings.json` and set your connection string under `DefaultConnection`.
   - Configure SMTP settings for email notifications.
3. **Apply migrations:**
   ```bash
   dotnet ef database update --project UniClassReserve
   ```
4. **Run the project:**
   ```bash
   dotnet run --project UniClassReserve
   ```
   The app will be available at `https://localhost:5001` (or the port shown in the console).

### Default Users & Roles
- The first user created is assigned the **Admin** role (`admin@admin.com` / `Admin123!`).
- All other users are assigned the **Instructor** role by default.
- You can add users and assign roles via the Admin Panel.

### Running Tests
```bash
dotnet test
```
All unit tests are located in the `UniClassReserve.Tests` project.

## Main Screens & Features
- **Admin Panel:** User, classroom, and term management; reservation approval/rejection; view all feedback and logs.
- **Instructor Panel:** Request reservations (single or batch), view/cancel reservations, leave feedback, see calendar.
- **Feedback:** Instructors can leave feedback for classrooms/terms; admins receive email notifications.
- **Logging:** All actions are logged; errors are also written to a file.
- **Security:** Lockout after 5 failed logins, CSRF protection, input validation, anti-XSS.

## Screenshots
_Add screenshots here if desired._

## Contact
For questions or contributions, please contact the project owner or open an issue.

---
_This project was developed as a university term project for CENG382._ 