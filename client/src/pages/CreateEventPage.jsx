import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../services/api';

export default function CreateEventPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    title: '',
    description: '',
    totalAmount: '',
    deadline: '',
    bankCode: '',
    bankAccountNumber: '',
    bankAccountName: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await api.post('/event', {
        ...form,
        totalAmount: parseFloat(form.totalAmount),
        deadline: new Date(form.deadline).toISOString(),
      });
      navigate(`/events/${res.data.id}`);
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to create event');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page">
      <nav className="navbar">
        <div className="nav-brand">
          <Link to="/" className="nav-back">← Quay lại</Link>
        </div>
      </nav>
      <div className="container container-sm">
        <div className="page-header">
          <h2>✨ Tạo Event mới</h2>
          <p className="text-muted">Điền thông tin sự kiện và bắt đầu chia tiền</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="form-card">
          <div className="form-section">
            <h3>📝 Thông tin sự kiện</h3>
            <div className="form-group">
              <label htmlFor="title">Tên sự kiện *</label>
              <input id="title" name="title" value={form.title} onChange={handleChange}
                placeholder="VD: Sinh nhật bạn Hoa" required />
            </div>
            <div className="form-group">
              <label htmlFor="description">Mô tả</label>
              <textarea id="description" name="description" value={form.description}
                onChange={handleChange} placeholder="Mô tả chi tiết sự kiện..." rows={3} />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="totalAmount">Tổng số tiền (VNĐ) *</label>
                <input id="totalAmount" name="totalAmount" type="number" value={form.totalAmount}
                  onChange={handleChange} placeholder="500000" required min="1" />
              </div>
              <div className="form-group">
                <label htmlFor="deadline">Hạn thanh toán *</label>
                <input id="deadline" name="deadline" type="datetime-local" value={form.deadline}
                  onChange={handleChange} required />
              </div>
            </div>
          </div>

          <div className="form-section">
            <h3>🏦 Thông tin thanh toán (QR)</h3>
            <p className="text-muted text-sm">Thông tin ngân hàng để tạo mã QR thanh toán</p>
            <div className="form-group">
              <label htmlFor="bankCode">Mã ngân hàng</label>
              <input id="bankCode" name="bankCode" value={form.bankCode} onChange={handleChange}
                placeholder="VD: VCB, TCB, MB..." />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="bankAccountNumber">Số tài khoản</label>
                <input id="bankAccountNumber" name="bankAccountNumber" value={form.bankAccountNumber}
                  onChange={handleChange} placeholder="0123456789" />
              </div>
              <div className="form-group">
                <label htmlFor="bankAccountName">Tên chủ TK</label>
                <input id="bankAccountName" name="bankAccountName" value={form.bankAccountName}
                  onChange={handleChange} placeholder="NGUYEN VAN A" />
              </div>
            </div>
          </div>

          <div className="form-actions">
            <Link to="/" className="btn btn-ghost">Hủy</Link>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Đang tạo...' : '🚀 Tạo Event'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
