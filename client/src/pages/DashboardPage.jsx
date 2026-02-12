import { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function DashboardPage() {
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  // Notifications
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [showNotifications, setShowNotifications] = useState(false);
  const notifRef = useRef(null);

  useEffect(() => {
    loadEvents();
    loadUnreadCount();
  }, []);

  // Close notification dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (notifRef.current && !notifRef.current.contains(e.target)) {
        setShowNotifications(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const loadEvents = async () => {
    try {
      const res = await api.get('/event');
      setEvents(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadUnreadCount = async () => {
    try {
      const res = await api.get('/notification/unread-count');
      setUnreadCount(res.data.count);
    } catch (err) {
      console.error(err);
    }
  };

  const loadNotifications = async () => {
    try {
      const res = await api.get('/notification?limit=15');
      setNotifications(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const toggleNotifications = () => {
    if (!showNotifications) {
      loadNotifications();
    }
    setShowNotifications(!showNotifications);
  };

  const handleMarkAllRead = async () => {
    try {
      await api.post('/notification/read-all');
      setUnreadCount(0);
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (err) {
      console.error(err);
    }
  };

  const handleNotificationClick = async (notif) => {
    if (!notif.isRead) {
      try {
        await api.post(`/notification/${notif.id}/read`);
        setUnreadCount(prev => Math.max(0, prev - 1));
        setNotifications(prev => prev.map(n => n.id === notif.id ? { ...n, isRead: true } : n));
      } catch (err) {
        console.error(err);
      }
    }
    if (notif.eventId) {
      setShowNotifications(false);
      navigate(`/events/${notif.eventId}`);
    }
  };

  const formatTimeAgo = (dateStr) => {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Vừa xong';
    if (mins < 60) return `${mins} phút trước`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours} giờ trước`;
    const days = Math.floor(hours / 24);
    return `${days} ngày trước`;
  };

  const getStatusInfo = (ev) => {
    if (ev.participantCount === 0) return { text: 'Chưa có người', cls: 'badge-neutral' };
    if (ev.paidCount === ev.participantCount) return { text: 'Hoàn tất', cls: 'badge-success' };
    if (new Date(ev.deadline) < new Date()) return { text: 'Quá hạn', cls: 'badge-danger' };
    return { text: `${ev.paidCount}/${ev.participantCount} đã thanh toán`, cls: 'badge-warning' };
  };

  const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    day: '2-digit', month: '2-digit', year: 'numeric',
  });

  const formatMoney = (n) => new Intl.NumberFormat('vi-VN', {
    style: 'currency', currency: 'VND',
  }).format(n);

  return (
    <div className="dashboard">
      <nav className="navbar">
        <div className="nav-brand">
          <span className="nav-logo">💸</span>
          <span>EventSplit</span>
        </div>
        <div className="nav-actions">
          {/* Notification Bell */}
          <div className="nav-notification" ref={notifRef}>
            <button className="notification-btn" onClick={toggleNotifications} title="Thông báo">
              🔔
              {unreadCount > 0 && (
                <span className="notification-badge">{unreadCount > 9 ? '9+' : unreadCount}</span>
              )}
            </button>
            {showNotifications && (
              <div className="notification-dropdown">
                <div className="notification-dropdown-header">
                  <span>🔔 Thông báo</span>
                  {unreadCount > 0 && (
                    <button className="btn btn-ghost btn-xs" onClick={handleMarkAllRead}>
                      Đọc tất cả
                    </button>
                  )}
                </div>
                {notifications.length === 0 ? (
                  <div className="notification-empty">Chưa có thông báo</div>
                ) : (
                  notifications.map((notif) => (
                    <div
                      key={notif.id}
                      className={`notification-item ${!notif.isRead ? 'unread' : ''}`}
                      onClick={() => handleNotificationClick(notif)}
                    >
                      <div>{notif.message}</div>
                      <div className="notification-time">{formatTimeAgo(notif.createdAt)}</div>
                    </div>
                  ))
                )}
              </div>
            )}
          </div>
          <span className="nav-user">👋 {user?.fullName}</span>
          <button className="btn btn-ghost" onClick={logout}>Đăng xuất</button>
        </div>
      </nav>

      <div className="container">
        <div className="dashboard-header">
          <div>
            <h2>Sự kiện của tôi</h2>
            <p className="text-muted">Quản lý tất cả các event chia tiền</p>
          </div>
          <Link to="/events/create" className="btn btn-primary">
            ✨ Tạo Event mới
          </Link>
        </div>

        {loading ? (
          <div className="loading-container">
            <div className="spinner"></div>
            <p>Đang tải...</p>
          </div>
        ) : events.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📋</div>
            <h3>Chưa có event nào</h3>
            <p>Hãy tạo event đầu tiên và bắt đầu chia sẻ chi phí</p>
            <Link to="/events/create" className="btn btn-primary">Tạo Event</Link>
          </div>
        ) : (
          <div className="event-grid">
            {events.map((ev) => {
              const status = getStatusInfo(ev);
              return (
                <div key={ev.id} className="event-card" onClick={() => navigate(`/events/${ev.id}`)}>
                  <div className="event-card-header">
                    <h3>{ev.title}</h3>
                    <span className={`badge ${status.cls}`}>{status.text}</span>
                  </div>
                  <div className="event-card-body">
                    <div className="event-stat">
                      <span className="stat-label">💰 Tổng tiền</span>
                      <span className="stat-value">{formatMoney(ev.totalAmount)}</span>
                    </div>
                    <div className="event-stat">
                      <span className="stat-label">👥 Thành viên</span>
                      <span className="stat-value">{ev.participantCount} người</span>
                    </div>
                    <div className="event-stat">
                      <span className="stat-label">📅 Hạn cuối</span>
                      <span className="stat-value">{formatDate(ev.deadline)}</span>
                    </div>
                  </div>
                  <div className="event-card-footer">
                    {ev.isCreator ? (
                      <span className="tag tag-creator">👑 Người tạo</span>
                    ) : (
                      <span className="tag tag-member">🙋 Thành viên</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
