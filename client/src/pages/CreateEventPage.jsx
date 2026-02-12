import { useState, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../services/api';

export default function CreateEventPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    title: '',
    description: '',
    deadline: '',
    bankCode: '',
    bankAccountNumber: '',
    bankAccountName: '',
    creatorJoins: true,
  });
  const [expenses, setExpenses] = useState([{ description: '', amount: '' }]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Auto-calculate total from expense items
  const totalAmount = useMemo(() => {
    return expenses.reduce((sum, e) => sum + (parseFloat(e.amount) || 0), 0);
  }, [expenses]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm({ ...form, [name]: type === 'checkbox' ? checked : value });
  };

  const handleExpenseChange = (index, field, value) => {
    const updated = [...expenses];
    updated[index] = { ...updated[index], [field]: value };
    setExpenses(updated);
  };

  const addExpenseRow = () => {
    setExpenses([...expenses, { description: '', amount: '' }]);
  };

  const removeExpenseRow = (index) => {
    if (expenses.length <= 1) return;
    setExpenses(expenses.filter((_, i) => i !== index));
  };

  // Get min datetime for the deadline picker (current time in Vietnam)
  const getMinDateTime = () => {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
  };

  // Quick deadline shortcuts
  const setQuickDeadline = (days) => {
    const d = new Date();
    d.setDate(d.getDate() + days);
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    setForm({ ...form, deadline: d.toISOString().slice(0, 16) });
  };

  const formatMoney = (n) =>
    new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Validate expenses
    const validExpenses = expenses.filter(
      (e) => e.description.trim() && parseFloat(e.amount) > 0
    );
    if (validExpenses.length === 0) {
      setError('Vui lòng thêm ít nhất một khoản chi phí.');
      return;
    }
    if (!form.deadline) {
      setError('Vui lòng chọn hạn thanh toán.');
      return;
    }

    setLoading(true);
    try {
      const res = await api.post('/event', {
        title: form.title,
        description: form.description,
        totalAmount: totalAmount,
        deadline: form.deadline,
        bankCode: form.bankCode || null,
        bankAccountNumber: form.bankAccountNumber || null,
        bankAccountName: form.bankAccountName || null,
        creatorJoins: form.creatorJoins,
        initialExpenses: validExpenses.map((e) => ({
          description: e.description.trim(),
          amount: parseFloat(e.amount),
        })),
      });
      navigate(`/events/${res.data.id}`);
    } catch (err) {
      setError(err.response?.data?.error || 'Tạo event thất bại');
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
          <p className="text-muted">Thêm các khoản chi phí, hệ thống sẽ tự tính tổng</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="form-card">
          {/* Event Info */}
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
          </div>

          {/* Expense Items */}
          <div className="form-section">
            <h3>💰 Chi phí</h3>
            <p className="text-muted text-sm" style={{ marginBottom: 16 }}>
              Thêm từng khoản chi phí, tổng tiền sẽ được tính tự động
            </p>

            <div className="expense-builder">
              {expenses.map((exp, idx) => (
                <div key={idx} className="expense-builder-row">
                  <input
                    type="text"
                    placeholder="Mô tả (VD: Đặt bàn, Bánh kem...)"
                    value={exp.description}
                    onChange={(e) => handleExpenseChange(idx, 'description', e.target.value)}
                    className="expense-builder-desc"
                    required
                  />
                  <div className="expense-builder-amount-wrap">
                    <input
                      type="number"
                      placeholder="Số tiền"
                      value={exp.amount}
                      onChange={(e) => handleExpenseChange(idx, 'amount', e.target.value)}
                      className="expense-builder-amount"
                      required
                      min="1"
                    />
                    <span className="expense-builder-currency">₫</span>
                  </div>
                  <button type="button" className="btn btn-ghost btn-xs expense-builder-remove"
                    onClick={() => removeExpenseRow(idx)}
                    disabled={expenses.length <= 1}
                    title="Xóa khoản này"
                  >
                    ✕
                  </button>
                </div>
              ))}

              <button type="button" className="btn btn-ghost btn-sm" onClick={addExpenseRow}
                style={{ marginTop: 8 }}>
                ➕ Thêm khoản chi phí
              </button>
            </div>

            {/* Total Summary */}
            <div className="expense-total-bar">
              <span>Tổng cộng:</span>
              <span className="expense-total-value">{formatMoney(totalAmount)}</span>
            </div>
          </div>

          {/* Deadline */}
          <div className="form-section">
            <h3>📅 Hạn thanh toán</h3>
            <div className="deadline-shortcuts">
              <button type="button" className="btn btn-ghost btn-xs" onClick={() => setQuickDeadline(1)}>
                1 ngày
              </button>
              <button type="button" className="btn btn-ghost btn-xs" onClick={() => setQuickDeadline(3)}>
                3 ngày
              </button>
              <button type="button" className="btn btn-ghost btn-xs" onClick={() => setQuickDeadline(7)}>
                1 tuần
              </button>
              <button type="button" className="btn btn-ghost btn-xs" onClick={() => setQuickDeadline(14)}>
                2 tuần
              </button>
              <button type="button" className="btn btn-ghost btn-xs" onClick={() => setQuickDeadline(30)}>
                1 tháng
              </button>
            </div>
            <div className="form-group" style={{ marginBottom: 0 }}>
              <label htmlFor="deadline">Hoặc chọn ngày giờ cụ thể *</label>
              <input id="deadline" name="deadline" type="datetime-local" value={form.deadline}
                onChange={handleChange} required min={getMinDateTime()} />
              {form.deadline && (
                <p className="text-sm text-muted" style={{ marginTop: 6 }}>
                  ⏰ {new Date(form.deadline).toLocaleString('vi-VN', {
                    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                  })}
                </p>
              )}
            </div>
          </div>

          {/* Bank Info */}
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

          {/* Creator Joins */}
          <div className="form-section">
            <label className="checkbox-label" htmlFor="creatorJoins">
              <input type="checkbox" id="creatorJoins" name="creatorJoins"
                checked={form.creatorJoins} onChange={handleChange} />
              <span className="checkbox-mark"></span>
              <div>
                <span className="checkbox-text">🙋 Tôi cũng tham gia event này</span>
                <span className="checkbox-hint">Bạn sẽ được thêm vào danh sách người tham gia</span>
              </div>
            </label>
          </div>

          <div className="form-actions">
            <Link to="/" className="btn btn-ghost">Hủy</Link>
            <button type="submit" className="btn btn-primary" disabled={loading || totalAmount <= 0}>
              {loading ? 'Đang tạo...' : '🚀 Tạo Event'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
