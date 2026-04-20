import { useEffect, useState } from 'react';
import './RepairProposalModal.css';

export interface RepairProposalFormValues {
  repairKind: string;
}

export interface RepairProposalAssetItem {
  assetCode: string;
  assetName: string;
}

export interface RepairProposalModalProps {
  open: boolean;
  loading: boolean;
  items: RepairProposalAssetItem[];
  onClose: () => void;
  onSubmit: (values: RepairProposalFormValues) => void | Promise<void>;
}

export function RepairProposalModal({
  open,
  loading,
  items,
  onClose,
  onSubmit,
}: RepairProposalModalProps) {
  const [repairKind, setRepairKind] = useState('');

  useEffect(() => {
    if (!open) return;
    const timer = window.setTimeout(() => {
      setRepairKind('');
    }, 0);
    return () => window.clearTimeout(timer);
  }, [open]);

  if (!open) return null;

  const count = items.length;
  const canSubmit = repairKind.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit || loading) return;
    void onSubmit({
      repairKind: repairKind.trim(),
    });
  };

  return (
    <div className="repair-proposal-modal-overlay" role="dialog" aria-modal="true">
      <div className="repair-proposal-modal">
        <button type="button" className="repair-proposal-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="repair-proposal-modal__close">×</span>
        </button>

        <div className="repair-proposal-modal__header">
          <h2 className="repair-proposal-modal__title">Tạo đơn sửa chữa</h2>
        </div>

        <div className="repair-proposal-modal__body">
          <div className="repair-proposal-modal__content">
            {count > 1 ? (
              <ul className="repair-proposal-modal__asset-list">
                {items.map((it, idx) => (
                  <li key={`${it.assetCode}-${idx}`}>
                    <strong>{it.assetCode || '—'}</strong>
                    {it.assetName ? ` — ${it.assetName}` : null}
                  </li>
                ))}
              </ul>
            ) : null}

            <div className="repair-proposal-form__item">
              <label htmlFor="repair-proposal-kind">
                Phương án sửa chữa đề xuất<span className="repair-proposal-required">*</span>
              </label>
              <textarea
                id="repair-proposal-kind"
                className="repair-proposal-textarea"
                rows={3}
                value={repairKind}
                onChange={(e) => setRepairKind(e.target.value)}
                placeholder="Ví dụ: thay linh kiện, sửa chữa nội bộ, gửi bảo hành nhà cung cấp…"
              />
            </div>
          </div>
        </div>

        <div className="repair-proposal-modal__footer">
          <button
            type="button"
            className="repair-proposal-btn-submit"
            onClick={handleSubmit}
            disabled={loading || !canSubmit}
          >
            {loading ? 'Đang gửi...' : 'Gửi phê duyệt'}
          </button>
          <button type="button" className="repair-proposal-btn-cancel" onClick={onClose} disabled={loading}>
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}
