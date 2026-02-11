# 📖 Hướng dẫn phát triển EventSplit

## Cấu trúc Clean Architecture

```
src/
├── EventSplit.Domain/           # Layer 1: Entities & Enums (không phụ thuộc gì)
│   ├── Entities/
│   │   ├── User.cs              # Người dùng
│   │   ├── Event.cs             # Sự kiện
│   │   ├── EventParticipant.cs  # Người tham gia + trạng thái thanh toán
│   │   ├── EventExpense.cs      # Chi tiết chi phí
│   │   ├── RefreshToken.cs      # Refresh token (JWT rotation)
│   │   └── Notification.cs      # Thông báo in-app
│   └── Enums/
│       └── PaymentStatus.cs     # Pending | Paid | Overdue
│
├── EventSplit.Application/      # Layer 2: Business Logic (phụ thuộc Domain)
│   ├── DTOs/Dtos.cs             # Request/Response records
│   ├── Interfaces/
│   │   ├── IAppDbContext.cs     # Database abstraction
│   │   ├── ITokenService.cs     # JWT + Refresh token
│   │   └── IEmailService.cs     # Email abstraction
│   └── Services/
│       ├── AuthService.cs       # Đăng ký, đăng nhập, JWT, refresh token
│       ├── EventService.cs      # CRUD event, chia tiền, thanh toán, overdue + email
│       └── NotificationService.cs # CRUD thông báo in-app
│
├── EventSplit.Infrastructure/   # Layer 3: Data Access (phụ thuộc Application)
│   ├── Data/AppDbContext.cs     # EF Core DbContext + cấu hình bảng
│   └── Services/
│       ├── TokenService.cs      # Tạo JWT + refresh token
│       └── EmailService.cs      # Gửi email qua SMTP
│
└── EventSplit.WebAPI/           # Layer 4: API Layer (phụ thuộc Infrastructure)
    ├── Controllers/
    │   ├── AuthController.cs    # POST register, login, refresh + GET /me
    │   ├── EventController.cs   # Full CRUD + participants + QR (VietQR)
    │   └── NotificationController.cs # GET/POST notifications
    ├── Hubs/PaymentHub.cs       # SignalR hub (realtime)
    ├── Jobs/OverduePaymentJob.cs# Hangfire recurring job + SignalR broadcast
    └── Program.cs               # DI, middleware, routing
```

---

## Thêm tính năng mới

### Thêm Entity mới

1. Tạo class trong `Domain/Entities/`
2. Thêm `DbSet<T>` vào `IAppDbContext` + `AppDbContext`
3. Cấu hình relationships trong `AppDbContext.OnModelCreating()`
4. Chạy lại app (SQLite dùng `EnsureCreated()`)

### Thêm API endpoint mới

1. Tạo DTO trong `Application/DTOs/Dtos.cs`
2. Thêm logic trong `Application/Services/`
3. Tạo controller/method trong `WebAPI/Controllers/`
4. Đăng ký DI trong `Program.cs` nếu cần
5. Thêm `[Authorize]` nếu cần authentication

### Thêm Background Job

1. Tạo job class trong `WebAPI/Jobs/`
2. Đăng ký DI trong `Program.cs`
3. Thêm `RecurringJob.AddOrUpdate<T>()` trong `Program.cs`

### Thêm Service mới

1. Tạo interface trong `Application/Interfaces/`
2. Tạo implementation trong `Infrastructure/Services/`
3. Đăng ký trong `Program.cs`: `builder.Services.AddScoped<IMyService, MyService>()`

---

## Frontend

```
client/src/
├── App.jsx                 # Router + ProtectedRoute / GuestRoute
├── index.css               # Toàn bộ CSS (dark theme, responsive, animations)
├── contexts/
│   └── AuthContext.jsx     # Quản lý JWT + RefreshToken + user state
├── services/
│   └── api.js              # Axios instance + JWT interceptor + auto refresh
└── pages/
    ├── LoginPage.jsx        # Đăng nhập
    ├── RegisterPage.jsx     # Đăng ký
    ├── DashboardPage.jsx    # Danh sách event + notification bell
    ├── CreateEventPage.jsx  # Form tạo event
    └── EventDetailPage.jsx  # Chi tiết + countdown + participants + QR + expenses
```

### Auto Refresh Token Flow

```
Request → 401 Unauthorized
  → Interceptor catches error
  → POST /api/auth/refresh (with stored refreshToken)
  → Success: Update tokens, retry original request
  → Failure: Redirect to /login
  → Queue concurrent requests during refresh
```

### SignalR Integration

`EventDetailPage.jsx` tự động kết nối SignalR hub và lắng nghe các sự kiện:
- `PaymentConfirmed` – khi ai đó xác nhận thanh toán
- `ParticipantAdded` / `ParticipantRemoved` – thay đổi thành viên
- `EventUpdated` – event được cập nhật
- `ExpenseAdded` – thêm chi phí mới
- `OverduePaymentsMarked` – Hangfire đánh dấu quá hạn

### Countdown Timer

`EventDetailPage.jsx` sử dụng `setInterval` 1 giây để cập nhật countdown realtime:
- Hiển thị: `DD : HH : MM : SS` (ngày, giờ, phút, giây)
- Khi hết hạn: hiển thị trạng thái "Đã quá hạn" với animation shake
- CSS animations: blink separator, fade-in, pulse

---

## Database Schema (ERD)

```
┌─────────────┐     ┌──────────────────┐     ┌──────────────┐
│    Users     │     │     Events       │     │ EventExpenses│
├─────────────┤     ├──────────────────┤     ├──────────────┤
│ Id (PK)     │◄────│ CreatorId (FK)   │     │ Id (PK)      │
│ FullName    │     │ Id (PK)          │◄────│ EventId (FK) │
│ Email (UQ)  │     │ Title            │     │ Description  │
│ PasswordHash│     │ Description      │     │ Amount       │
│ CreatedAt   │     │ TotalAmount      │     │ CreatedByUserId│
└─────────────┘     │ Deadline         │     │ CreatedAt    │
       │            │ BankCode         │     └──────────────┘
       │            │ BankAccountNumber│
       │            │ BankAccountName  │
       │            │ CreatedAt        │
       │            └──────────────────┘
       │                    │
       │            ┌──────────────────┐
       ├───────────►│EventParticipants │
       │            ├──────────────────┤
       │            │ Id (PK)          │
       │            │ EventId (FK)     │
       │            │ UserId (FK)      │
       │            │ AmountToPay      │
       │            │ PaymentStatus    │
       │            │ PaidAt           │
       │            └──────────────────┘
       │
       │            ┌──────────────────┐
       ├───────────►│  RefreshTokens   │
       │            ├──────────────────┤
       │            │ Id (PK)          │
       │            │ UserId (FK)      │
       │            │ Token (UQ)       │
       │            │ ExpiresAt        │
       │            │ IsRevoked        │
       │            │ CreatedAt        │
       │            └──────────────────┘
       │
       │            ┌──────────────────┐
       └───────────►│  Notifications   │
                    ├──────────────────┤
                    │ Id (PK)          │
                    │ UserId (FK)      │
                    │ Message          │
                    │ Type             │
                    │ EventId (nullable)│
                    │ IsRead           │
                    │ CreatedAt        │
                    └──────────────────┘
```

---

## VietQR EMVCo Format

QR code được tạo theo chuẩn **EMVCo Merchant Presented QR** với các trường:

| Tag | Tên | Giá trị |
|-----|-----|---------|
| 00 | Payload Format Indicator | `01` |
| 01 | Point of Initiation | `11` (static) |
| 38 | Merchant Account Info (NAPAS) | BankBIN + Account Number |
| 52 | Merchant Category Code | `5999` |
| 53 | Transaction Currency | `704` (VND) |
| 54 | Transaction Amount | Số tiền (nếu > 0) |
| 58 | Country Code | `VN` |
| 62 | Additional Data | Nội dung chuyển khoản |
| 63 | CRC | CRC16-CCITT checksum |

---

## Chuyển sang PostgreSQL / MSSQL

1. Thay package NuGet trong `Infrastructure.csproj`:
   - PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL`
   - MSSQL: `Microsoft.EntityFrameworkCore.SqlServer`
2. Cập nhật `Program.cs`:
   ```csharp
   // PostgreSQL
   options.UseNpgsql(connectionString);
   // MSSQL
   options.UseSqlServer(connectionString);
   ```
3. Cập nhật connection string trong `appsettings.json`
4. Thay `Hangfire.MemoryStorage` bằng `Hangfire.PostgreSql` hoặc `Hangfire.SqlServer`

---

## Cấu hình Email (SMTP)

Để bật email nhắc nhở quá hạn, cấu hình trong `appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "From": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

> **Gmail**: Cần bật 2FA và tạo [App Password](https://myaccount.google.com/apppasswords).
> Nếu để trống, hệ thống vẫn hoạt động bình thường, chỉ log warning và bỏ qua email.
