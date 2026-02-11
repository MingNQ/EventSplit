# 🎯 Event Split Payment Web App – TODO

---

# 📌 PROJECT OVERVIEW

Ứng dụng web cho phép:
- Người dùng đăng ký / đăng nhập
- Tạo event
- Thêm người tham gia
- Chia tiền đầu người
- Thanh toán bằng QR
- Theo dõi trạng thái thanh toán
- Tự động nhắc nhở khi quá hạn

---

# 🧱 PHASE 0 – Planning & Setup

## ☐ Define MVP Scope
- [ ] Authentication (Register/Login)
- [ ] Create Event
- [ ] Add Participants
- [ ] Split money equally
- [ ] Generate QR thanh toán
- [ ] Confirm payment
- [ ] Deadline & Overdue status
- [ ] Payment reminder job

---

## ☐ Choose Tech Stack

### Backend
- [ ] ASP.NET Core Web API
- [ ] EF Core
- [ ] MSSQL / PostgreSQL
- [ ] JWT Authentication
- [ ] Hangfire (Background Job)
- [ ] SignalR (Realtime)

### Frontend
- [ ] React + Vite
- [ ] TailwindCSS
- [ ] Axios
- [ ] React Query
- [ ] React Router

---

# 🏗 PHASE 1 – Backend Foundation

## ☐ Setup Clean Architecture
├──src
    ├── Domain
    ├── Application
    ├── Infrastructure
    ├── WebAPI

- [ ] Create solution
- [ ] Setup project references
- [ ] Configure DI
- [ ] Setup EF Core
- [ ] Create initial migration

---

## ☐ Authentication

### Database
- [ ] Users table
- [ ] RefreshTokens table

### API
- [ ] Register endpoint
- [ ] Login endpoint
- [ ] Generate JWT
- [ ] Refresh token endpoint
- [ ] Middleware authorization
- [ ] Password hashing (BCrypt)

### Testing
- [ ] Test login via Postman
- [ ] Test token expiration
- [ ] Test refresh flow

---

# 📅 PHASE 2 – Event Core Logic

## ☐ Database Tables

- [ ] Events
- [ ] EventParticipants
- [ ] EventExpenses

### Enum
- [ ] PaymentStatus (Pending, Paid, Overdue)

---

## ☐ Event CRUD

- [ ] Create Event
- [ ] Get Event Detail
- [ ] Get My Events
- [ ] Update Event
- [ ] Delete Event

### Validation
- [ ] Deadline > Now
- [ ] TotalAmount > 0

---

## ☐ Add Participants

- [ ] Add by email
- [ ] Prevent duplicate participant
- [ ] Only creator can modify participants
- [ ] Validate user exists

---

## ☐ Split Money Logic

- [ ] Equal split calculation
- [ ] Store AmountToPay
- [ ] Handle rounding
- [ ] Use transaction when creating event + participants

---

# 💰 PHASE 3 – QR Payment

## ☐ QR Generation (MVP)

- [ ] Input bank code
- [ ] Input account number
- [ ] Input account name
- [ ] Generate VietQR string
- [ ] Render QR image
- [ ] Add transfer content: EVENTID_USERID

---

## ☐ Confirm Payment

### MVP
- [ ] Confirm Payment API
- [ ] Update PaymentStatus → Paid
- [ ] Set PaidAt
- [ ] Prevent double confirmation

### Advanced (Optional)
- [ ] Integrate payment gateway webhook
- [ ] Auto update payment status

---

# 🔔 PHASE 4 – Deadline & Reminder

## ☐ Setup Hangfire

- [ ] Install Hangfire
- [ ] Configure dashboard
- [ ] Create recurring job (every 5 minutes)

---

## ☐ Overdue Job Logic

- [ ] Find participants:
  - Status = Pending
  - Deadline < Now
- [ ] Update status → Overdue

---

## ☐ Reminder System

- [ ] Send email reminder
- [ ] Optional: in-app notification
- [ ] Optional: push notification

---

# ⚡ PHASE 5 – Realtime Update

## ☐ Setup SignalR

- [ ] Create PaymentHub
- [ ] Join group by EventId
- [ ] Broadcast when payment updated

---

## ☐ Frontend Realtime

- [ ] Connect to hub
- [ ] Update UI on payment change
- [ ] Show status badge (Paid / Pending / Overdue)

---

# 🎨 PHASE 6 – Frontend Development

## ☐ Authentication UI

- [ ] Login page
- [ ] Register page
- [ ] Store JWT
- [ ] Auto refresh token

---

## ☐ Event UI

- [ ] Dashboard (My Events)
- [ ] Create Event form
- [ ] Event detail page
- [ ] Participant list
- [ ] Payment status display

---

## ☐ QR UI

- [ ] QR modal
- [ ] Countdown to deadline
- [ ] "I have paid" button

---

# 🔐 PHASE 7 – Security & Validation

## ☐ Authorization

- [ ] Only creator edit/delete event
- [ ] Only participant view event
- [ ] Validate ownership in API

---

## ☐ Concurrency Handling

- [ ] Add RowVersion column
- [ ] Handle optimistic concurrency
- [ ] Wrap critical updates in transaction

---

## ☐ Rate Limiting

- [ ] Limit login attempts
- [ ] Limit confirm payment API

---

# 🧪 PHASE 8 – Testing

## Backend
- [ ] Unit test business logic
- [ ] Integration test APIs
- [ ] Test concurrency scenarios

## Frontend
- [ ] Test full payment flow
- [ ] Test deadline behavior
- [ ] Test token refresh

---

# 🚀 PHASE 9 – Deployment

## Backend
- [ ] Create Dockerfile
- [ ] Setup Docker Compose (API + DB)
- [ ] Deploy to VPS / Azure / Render

## Frontend
- [ ] Production build
- [ ] Deploy to Vercel / Netlify

---

# 📦 PHASE 10 – Documentation

## Technical Docs
- [ ] ERD Diagram
- [ ] Architecture Diagram
- [ ] Swagger API documentation

## README.md
- [ ] Setup guide
- [ ] Environment variables
- [ ] Demo account
- [ ] Screenshots

---

# 🏁 FINAL CHECKLIST

- [ ] Auth works correctly
- [ ] Payment logic accurate
- [ ] Overdue auto updates
- [ ] No concurrency bugs
- [ ] UI responsive & clean
- [ ] Deployment successful
- [ ] Tested with multiple users

---

# 📅 Suggested Timeline (8 Weeks)

| Week | Tasks |
|------|-------|
| 1 | Setup + Auth |
| 2 | Event CRUD |
| 3 | Participants + Split logic |
| 4 | QR Payment |
| 5 | Background job |
| 6 | Frontend core |
| 7 | Realtime + Polish |
| 8 | Testing + Deployment |

---

# ✅ PROJECT COMPLETION GOAL

A production-ready mini Splitwise-like application  
with authentication, QR payment, deadline tracking,  
background job processing, and realtime updates.
