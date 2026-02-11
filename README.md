# 💸 EventSplit – Ứng dụng Chia Tiền Sự Kiện

Ứng dụng web cho phép tạo sự kiện, thêm người tham gia, chia tiền đầu người, thanh toán qua mã QR (chuẩn VietQR), theo dõi trạng thái thanh toán realtime, và nhận thông báo nhắc nhở tự động.

---

## ✨ Tính năng chính

| Tính năng | Mô tả |
|-----------|-------|
| 🔐 **Đăng ký / Đăng nhập** | JWT Authentication + Refresh Token tự động, mã hóa BCrypt |
| 📅 **Quản lý Event** | Tạo, sửa, xóa sự kiện với validation (TotalAmount > 0, Deadline > Now) |
| 👥 **Thêm người tham gia** | Mời qua email, tự động chia tiền đều, chống trùng |
| 💰 **Chia tiền tự động** | Tính toán số tiền mỗi người, tự tính lại khi thay đổi thành viên |
| 📱 **QR Code thanh toán (VietQR)** | Tạo mã QR chuẩn EMVCo/NAPAS, hỗ trợ 30+ ngân hàng Việt Nam |
| ✅ **Xác nhận thanh toán** | Người tạo hoặc người tham gia xác nhận, chống double confirm |
| ⏱ **Countdown realtime** | Đếm ngược thời gian còn lại đến hạn thanh toán (ngày:giờ:phút:giây) |
| ⏰ **Nhắc nhở quá hạn** | Hangfire tự động đánh dấu "Quá hạn" + gửi email + thông báo in-app |
| 🔔 **Hệ thống thông báo** | Notification bell trên Dashboard, đánh dấu đã đọc, click để xem event |
| ⚡ **Realtime update** | SignalR cập nhật trạng thái thanh toán, thành viên, chi phí tức thì |
| 📝 **Chi tiết chi phí** | Thêm từng khoản chi cho event |

---

## 🏗 Kiến trúc

```
EventSplit/
├── src/                          # Backend (.NET 10)
│   ├── EventSplit.Domain/        # Entities, Enums
│   ├── EventSplit.Application/   # DTOs, Services, Interfaces
│   ├── EventSplit.Infrastructure/# DbContext, TokenService, EmailService
│   └── EventSplit.WebAPI/        # Controllers, SignalR Hub, Hangfire Jobs
├── client/                       # Frontend (React 19 + Vite 7)
│   └── src/
│       ├── pages/                # Login, Register, Dashboard, CreateEvent, EventDetail
│       ├── contexts/             # AuthContext (JWT + RefreshToken)
│       └── services/             # Axios API client (auto refresh token)
├── docs/                         # Tài liệu phát triển
└── TODO.md                       # Roadmap chi tiết
```

### Tech Stack

| Layer | Công nghệ |
|-------|----------|
| **Backend** | ASP.NET Core 10, EF Core 10, SQLite |
| **Auth** | JWT Bearer + Refresh Token, BCrypt.Net |
| **Realtime** | SignalR |
| **Background Jobs** | Hangfire (Memory Storage) |
| **QR Code** | QRCoder + VietQR EMVCo format |
| **Email** | System.Net.Mail (SMTP) |
| **Frontend** | React 19, Vite 7, React Router 7 |
| **HTTP Client** | Axios (auto refresh token interceptor) |

---

## 🚀 Hướng dẫn cài đặt

### Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

### 1. Clone project

```bash
git clone <repo-url>
cd EventSplit
```

### 2. Chạy Backend

```bash
cd src
dotnet build
dotnet run --project EventSplit.WebAPI
```

Backend sẽ chạy tại: **http://localhost:5000**
- Swagger UI: http://localhost:5000/swagger
- Hangfire Dashboard: http://localhost:5000/hangfire

### 3. Chạy Frontend

Mở terminal mới:

```bash
cd client
npm install
npm run dev
```

Frontend sẽ chạy tại: **http://localhost:5173**

---

## 📡 API Endpoints

### Auth
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/auth/register` | Đăng ký tài khoản |
| POST | `/api/auth/login` | Đăng nhập |
| POST | `/api/auth/refresh` | Làm mới JWT bằng refresh token |
| GET | `/api/auth/me` | Thông tin user (🔒) |

### Event
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/event` | Tạo event mới (🔒) |
| GET | `/api/event` | Danh sách event của tôi (🔒) |
| GET | `/api/event/{id}` | Chi tiết event (🔒) |
| PUT | `/api/event/{id}` | Cập nhật event (🔒 creator) |
| DELETE | `/api/event/{id}` | Xóa event (🔒 creator) |

### Participants
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/event/{id}/participants` | Thêm người tham gia (🔒 creator) |
| DELETE | `/api/event/{id}/participants/{pid}` | Xóa người tham gia (🔒 creator) |
| POST | `/api/event/{id}/participants/{pid}/confirm` | Xác nhận thanh toán (🔒) |

### Expenses & QR
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/event/{id}/expenses` | Thêm chi phí (🔒) |
| POST | `/api/event/{id}/qr` | Tạo QR code VietQR (🔒) |

### Notifications
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/notification` | Danh sách thông báo (🔒) |
| GET | `/api/notification/unread-count` | Số thông báo chưa đọc (🔒) |
| POST | `/api/notification/{id}/read` | Đánh dấu đã đọc (🔒) |
| POST | `/api/notification/read-all` | Đánh dấu tất cả đã đọc (🔒) |

> 🔒 = Yêu cầu JWT token trong header `Authorization: Bearer <token>`

---

## 🔧 Cấu hình

File `src/EventSplit.WebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=eventsplit.db"
  },
  "Jwt": {
    "Key": "EventSplitSuperSecretKey12345678!",
    "Issuer": "EventSplit",
    "Audience": "EventSplit"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "From": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

| Biến | Mô tả | Mặc định |
|------|-------|---------| 
| `ConnectionStrings:Default` | SQLite connection string | `Data Source=eventsplit.db` |
| `Jwt:Key` | Secret key cho JWT (≥32 ký tự) | `EventSplitSuperSecretKey12345678!` |
| `Jwt:Issuer` | JWT Issuer | `EventSplit` |
| `Jwt:Audience` | JWT Audience | `EventSplit` |
| `Email:SmtpHost` | SMTP server | _(trống = tắt email)_ |
| `Email:SmtpPort` | SMTP port | `587` |
| `Email:From` | Email gửi đi | _(trống = tắt email)_ |
| `Email:Password` | Mật khẩu SMTP / App Password | _(trống = tắt email)_ |

> 💡 **Lưu ý:** Email là tùy chọn. Nếu không cấu hình, hệ thống vẫn hoạt động bình thường, chỉ bỏ qua việc gửi email nhắc nhở.

---

## 📱 VietQR – Thanh toán bằng QR

Hệ thống tạo mã QR theo **chuẩn EMVCo / NAPAS** (VietQR), hỗ trợ các ngân hàng:

| Mã | Ngân hàng | Mã | Ngân hàng |
|----|-----------|-----|----------|
| VCB | Vietcombank | TCB | Techcombank |
| MB | MB Bank | ACB | ACB |
| BIDV | BIDV | VPB | VPBank |
| TPB | TPBank | SHB | SHB |
| MSB | MSB | HDBank | HDBank |
| STB | Sacombank | EIB | Eximbank |
| VIB | VIB | LPB | LienVietPostBank |
| OCB | OCB | NCB | NCB |
| MOMO | MoMo | VIETTEL | Viettel Money |

> Quét mã QR bằng app ngân hàng → Thông tin chuyển khoản được điền sẵn (STK, số tiền, nội dung).

---

## 🧪 Demo nhanh

1. Mở http://localhost:5173 → **Đăng ký** 2 tài khoản (A và B)
2. **Đăng nhập tài khoản A** → Tạo Event (điền tên, số tiền, hạn thanh toán, thông tin ngân hàng)
3. Thêm tài khoản B bằng email → Xem **countdown** đếm ngược
4. Nhấn **"Hiện QR Code"** → Quét mã QR chuẩn VietQR
5. **Đăng nhập tài khoản B** → Xem event, kiểm tra **🔔 thông báo**, nhấn **"Xác nhận"** thanh toán
6. Quay lại tài khoản A → Thấy trạng thái cập nhật **realtime** ✅

---

## 📋 Trạng thái thanh toán

| Status | Emoji | Ý nghĩa |
|--------|-------|---------|
| `Pending` | ⏳ | Chưa thanh toán |
| `Paid` | ✅ | Đã thanh toán |
| `Overdue` | ⚠️ | Quá hạn (tự động bởi Hangfire mỗi 5 phút + email + notification) |

---

## 🔔 Hệ thống thông báo

- **In-app notifications**: Bell icon trên Dashboard hiển thị số thông báo chưa đọc
- **Email reminders**: Gửi email HTML khi thanh toán quá hạn (nếu đã cấu hình SMTP)
- **Realtime updates**: SignalR broadcast khi có thay đổi thanh toán, thành viên, chi phí

### SignalR Events

| Event | Trigger |
|-------|---------|
| `PaymentConfirmed` | Xác nhận thanh toán |
| `ParticipantAdded` | Thêm người tham gia |
| `ParticipantRemoved` | Xóa người tham gia |
| `EventUpdated` | Cập nhật event |
| `ExpenseAdded` | Thêm chi phí |
| `OverduePaymentsMarked` | Hangfire đánh dấu quá hạn |

---

## 🔐 Bảo mật

- **JWT + Refresh Token**: Access token (1 giờ) + Refresh token (30 ngày), tự động làm mới
- **BCrypt**: Mã hóa mật khẩu one-way
- **Authorization**: Chỉ creator có thể edit/delete event, chỉ participant/creator xem được event
- **Double confirm prevention**: Không cho xác nhận thanh toán 2 lần
- **Input validation**: Backend validate TotalAmount > 0, Deadline > Now, Title không trống

---

## 📄 License

MIT
