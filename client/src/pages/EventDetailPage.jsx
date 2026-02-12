import { useState, useEffect, useCallback } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { HubConnectionBuilder } from '@microsoft/signalr';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function EventDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [event, setEvent] = useState(null);
  const [loading, setLoading] = useState(true);
  const [email, setEmail] = useState('');
  const [addingParticipant, setAddingParticipant] = useState(false);
  const [error, setError] = useState('');
  const [showQR, setShowQR] = useState(false);
  const [qrImageUrl, setQrImageUrl] = useState(null);
  const [qrLoading, setQrLoading] = useState(false);
  const [expenseDesc, setExpenseDesc] = useState('');
  const [expenseAmount, setExpenseAmount] = useState('');
  const [addingExpense, setAddingExpense] = useState(false);
  const [countdown, setCountdown] = useState(null);

  const loadEvent = useCallback(async () => {
    try {
      const res = await api.get(`/event/${id}`);
      setEvent(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadEvent();
  }, [loadEvent]);

  // Realtime countdown timer - updates every second
  useEffect(() => {
    if (!event) return;

    const updateCountdown = () => {
      const now = new Date();
      const deadline = new Date(event.deadline);
      const diff = deadline - now;

      if (diff <= 0) {
        setCountdown({ expired: true, text: '⏰ Đã quá hạn', days: 0, hours: 0, minutes: 0, seconds: 0 });
        return;
      }

      const days = Math.floor(diff / 86400000);
      const hours = Math.floor((diff % 86400000) / 3600000);
      const minutes = Math.floor((diff % 3600000) / 60000);
      const seconds = Math.floor((diff % 60000) / 1000);

      setCountdown({ expired: false, days, hours, minutes, seconds });
    };

    updateCountdown();
    const timer = setInterval(updateCountdown, 1000);
    return () => clearInterval(timer);
  }, [event]);

  // SignalR Realtime
  useEffect(() => {
    const token = localStorage.getItem('token');
    const connection = new HubConnectionBuilder()
      .withUrl(`http://localhost:5000/hubs/payment?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    connection.start().then(() => {
      connection.invoke('JoinEventGroup', id);
    }).catch(console.error);

    connection.on('PaymentConfirmed', () => loadEvent());
    connection.on('ParticipantAdded', () => loadEvent());
    connection.on('ParticipantRemoved', () => loadEvent());
    connection.on('EventUpdated', () => loadEvent());
    connection.on('ExpenseAdded', () => loadEvent());
    connection.on('ExpenseDeleted', () => loadEvent());
    connection.on('OverduePaymentsMarked', () => loadEvent());

    return () => { connection.stop(); };
  }, [id, loadEvent]);

  const handleAddParticipant = async (e) => {
    e.preventDefault();
    setError('');
    setAddingParticipant(true);
    try {
      await api.post(`/event/${id}/participants`, { email });
      setEmail('');
      await loadEvent();
    } catch (err) {
      setError(err.response?.data?.error || 'Failed');
    } finally {
      setAddingParticipant(false);
    }
  };

  const handleRemoveParticipant = async (participantId) => {
    if (!confirm('Xóa người này khỏi event?')) return;
    try {
      await api.delete(`/event/${id}/participants/${participantId}`);
      await loadEvent();
    } catch (err) {
      setError(err.response?.data?.error || 'Failed');
    }
  };

  const handleConfirmPayment = async (participantId) => {
    try {
      await api.post(`/event/${id}/participants/${participantId}/confirm`);
      await loadEvent();
    } catch (err) {
      setError(err.response?.data?.error || 'Failed');
    }
  };

  const handleDeleteEvent = async () => {
    if (!confirm('Bạn có chắc muốn xóa event này?')) return;
    try {
      await api.delete(`/event/${id}`);
      navigate('/');
    } catch (err) {
      setError(err.response?.data?.error || 'Failed');
    }
  };

  const handleAddExpense = async (e) => {
    e.preventDefault();
    setAddingExpense(true);
    try {
      await api.post(`/event/${id}/expenses`, {
        description: expenseDesc,
        amount: parseFloat(expenseAmount),
      });
      setExpenseDesc('');
      setExpenseAmount('');
      await loadEvent();
    } catch (err) {
      setError(err.response?.data?.error || 'Failed');
    } finally {
      setAddingExpense(false);
    }
  };

  const handleDeleteExpense = async (expenseId) => {
    if (!confirm('Xóa khoản chi phí này?')) return;
    try {
      await api.delete(`/event/${id}/expenses/${expenseId}`);
      await loadEvent();
    } catch (err) {
      setError(err.response?.data?.error || 'Failed');
    }
  };

  const formatMoney = (n) => new Intl.NumberFormat('vi-VN', {
    style: 'currency', currency: 'VND',
  }).format(n);

  const formatDate = (d) => new Date(d).toLocaleString('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Paid': return <span className="badge badge-success">✅ Đã thanh toán</span>;
      case 'Overdue': return <span className="badge badge-danger">⚠️ Quá hạn</span>;
      default: return <span className="badge badge-warning">⏳ Chưa thanh toán</span>;
    }
  };

  const loadQRImage = async () => {
    setQrLoading(true);
    try {
      const perPerson = event.participants?.length > 0
        ? event.totalAmount / event.participants.length
        : event.totalAmount;
      const params = new URLSearchParams();
      params.set('amount', Math.round(perPerson).toString());
      if (event.bankCode) params.set('bankCode', event.bankCode);
      if (event.bankAccountNumber) params.set('accountNumber', event.bankAccountNumber);
      if (event.bankAccountName) params.set('accountName', event.bankAccountName);
      params.set('content', `${event.title} - ${id.substring(0, 8)}`);
      const res = await api.get(`/event/${id}/qr?${params.toString()}`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      setQrImageUrl(url);
    } catch (err) {
      setError('Không thể tải mã QR: ' + (err.response?.status || err.message));
    } finally {
      setQrLoading(false);
    }
  };

  const handleToggleQR = () => {
    if (!showQR && !qrImageUrl) {
      loadQRImage();
    }
    setShowQR(!showQR);
  };

  if (loading) return (
    <div className="loading-container"><div className="spinner"></div><p>Đang tải...</p></div>
  );

  if (!event) return (
    <div className="loading-container"><p>Không tìm thấy event</p><Link to="/">Quay lại</Link></div>
  );

  const isCreator = event.creatorId === user?.userId;
  const totalExpenses = event.expenses?.reduce((sum, e) => sum + e.amount, 0) || 0;

  return (
    <div className="page">
      <nav className="navbar">
        <div className="nav-brand">
          <Link to="/" className="nav-back">← Quay lại</Link>
        </div>
      </nav>

      <div className="container">
        {error && <div className="alert alert-error">{error}</div>}

        {/* Event Header */}
        <div className="detail-header">
          <div>
            <h2>{event.title}</h2>
            {event.description && <p className="text-muted">{event.description}</p>}
            <p className="text-sm text-muted">Tạo bởi: {event.creatorName} · {formatDate(event.createdAt)}</p>
          </div>
          {isCreator && (
            <button className="btn btn-danger" onClick={handleDeleteEvent}>🗑 Xóa Event</button>
          )}
        </div>

        {/* Stats Cards */}
        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-card-icon">💰</div>
            <div className="stat-card-label">Tổng tiền</div>
            <div className="stat-card-value">{formatMoney(event.totalAmount)}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-icon">👥</div>
            <div className="stat-card-label">Số người</div>
            <div className="stat-card-value">{event.participants?.length || 0}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-icon">💵</div>
            <div className="stat-card-label">Mỗi người</div>
            <div className="stat-card-value">
              {event.participants?.length > 0
                ? formatMoney(event.totalAmount / event.participants.length)
                : '—'}
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-card-icon">📅</div>
            <div className="stat-card-label">Hạn cuối</div>
            <div className="stat-card-value" style={{ fontSize: '0.85rem' }}>
              {formatDate(event.deadline)}
            </div>
          </div>
        </div>

        {/* Realtime Countdown */}
        {countdown && (
          <div className={`countdown-banner ${countdown.expired ? 'countdown-expired' : ''}`}>
            {countdown.expired ? (
              <div className="countdown-expired-text">
                <span className="countdown-icon">⏰</span>
                <span>Đã quá hạn thanh toán!</span>
              </div>
            ) : (
              <>
                <div className="countdown-label">⏳ Thời gian còn lại</div>
                <div className="countdown-timer">
                  <div className="countdown-unit">
                    <span className="countdown-value">{String(countdown.days).padStart(2, '0')}</span>
                    <span className="countdown-unit-label">Ngày</span>
                  </div>
                  <span className="countdown-separator">:</span>
                  <div className="countdown-unit">
                    <span className="countdown-value">{String(countdown.hours).padStart(2, '0')}</span>
                    <span className="countdown-unit-label">Giờ</span>
                  </div>
                  <span className="countdown-separator">:</span>
                  <div className="countdown-unit">
                    <span className="countdown-value">{String(countdown.minutes).padStart(2, '0')}</span>
                    <span className="countdown-unit-label">Phút</span>
                  </div>
                  <span className="countdown-separator">:</span>
                  <div className="countdown-unit">
                    <span className="countdown-value">{String(countdown.seconds).padStart(2, '0')}</span>
                    <span className="countdown-unit-label">Giây</span>
                  </div>
                </div>
              </>
            )}
          </div>
        )}

        {/* QR Payment */}
        {(event.bankAccountNumber || event.bankCode) && (
          <div className="section-card">
            <div className="section-header">
              <h3>🏦 Thông tin thanh toán</h3>
              <button className="btn btn-primary btn-sm" onClick={handleToggleQR}>
                {showQR ? 'Ẩn QR' : '📱 Hiện QR Code'}
              </button>
            </div>
            <div className="bank-info">
              {event.bankCode && <p><strong>Ngân hàng:</strong> {event.bankCode}</p>}
              {event.bankAccountNumber && <p><strong>STK:</strong> {event.bankAccountNumber}</p>}
              {event.bankAccountName && <p><strong>Chủ TK:</strong> {event.bankAccountName}</p>}
            </div>
            {showQR && (
              <div className="qr-container">
                {qrLoading ? (
                  <div className="loading-container" style={{ minHeight: '120px' }}>
                    <div className="spinner"></div>
                    <p>Đang tạo QR...</p>
                  </div>
                ) : qrImageUrl ? (
                  <>
                    <img src={qrImageUrl} alt="QR Code (VietQR)" className="qr-image" />
                    <p className="text-sm text-muted">Quét mã QR bằng app ngân hàng để thanh toán (VietQR)</p>
                  </>
                ) : (
                  <p className="text-muted">Không thể tải mã QR</p>
                )}
              </div>
            )}
          </div>
        )}

        {/* Participants */}
        <div className="section-card">
          <div className="section-header">
            <h3>👥 Người tham gia ({event.participants?.length || 0})</h3>
          </div>

          {isCreator && (
            <form onSubmit={handleAddParticipant} className="add-form">
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Nhập email người tham gia..."
                required
              />
              <button type="submit" className="btn btn-primary btn-sm" disabled={addingParticipant}>
                {addingParticipant ? '...' : '➕ Thêm'}
              </button>
            </form>
          )}

          {event.participants?.length === 0 ? (
            <p className="text-muted text-center py-2">Chưa có người tham gia</p>
          ) : (
            <div className="participant-list">
              {event.participants.map((p) => (
                <div key={p.id} className={`participant-item ${p.paymentStatus === 'Paid' ? 'paid' : ''}`}>
                  <div className="participant-info">
                    <div className="participant-avatar">
                      {p.fullName.charAt(0).toUpperCase()}
                    </div>
                    <div>
                      <div className="participant-name">{p.fullName}</div>
                      <div className="participant-email">{p.email}</div>
                    </div>
                  </div>
                  <div className="participant-amount">{formatMoney(p.amountToPay)}</div>
                  <div className="participant-status">
                    {getStatusBadge(p.paymentStatus)}
                    {p.paidAt && <span className="text-xs text-muted">{formatDate(p.paidAt)}</span>}
                  </div>
                  <div className="participant-actions">
                    {p.paymentStatus !== 'Paid' && (isCreator || p.userId === user?.userId) && (
                      <button className="btn btn-success btn-xs" onClick={() => handleConfirmPayment(p.id)}>
                        ✅ Xác nhận
                      </button>
                    )}
                    {isCreator && (
                      <button className="btn btn-ghost btn-xs" onClick={() => handleRemoveParticipant(p.id)}>
                        🗑
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Expenses */}
        <div className="section-card">
          <div className="section-header">
            <h3>📝 Chi tiết chi phí</h3>
            {totalExpenses > 0 && (
              <span className="text-muted">Tổng: {formatMoney(totalExpenses)}</span>
            )}
          </div>

          <form onSubmit={handleAddExpense} className="add-form">
            <input
              type="text"
              value={expenseDesc}
              onChange={(e) => setExpenseDesc(e.target.value)}
              placeholder="Mô tả chi phí..."
              required
            />
            <input
              type="number"
              value={expenseAmount}
              onChange={(e) => setExpenseAmount(e.target.value)}
              placeholder="Số tiền"
              required
              min="1"
              style={{ maxWidth: '140px' }}
            />
            <button type="submit" className="btn btn-primary btn-sm" disabled={addingExpense}>
              {addingExpense ? '...' : '➕'}
            </button>
          </form>

          {event.expenses?.length > 0 && (
            <div className="expense-list">
              {event.expenses.map((exp) => (
                <div key={exp.id} className="expense-item">
                  <div>
                    <div className="expense-desc">{exp.description}</div>
                    <div className="text-xs text-muted">{exp.createdByName} · {formatDate(exp.createdAt)}</div>
                  </div>
                  <div className="expense-item-right">
                    <div className="expense-amount">{formatMoney(exp.amount)}</div>
                    {isCreator && (
                      <button className="btn btn-ghost btn-xs" onClick={() => handleDeleteExpense(exp.id)}
                        title="Xóa chi phí">
                        🗑
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
