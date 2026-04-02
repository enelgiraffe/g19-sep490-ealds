import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import '../../assets/components/MarkDamagedAssetModal.css';

export interface LiquidationDisposalApproveModalProps {
  open: boolean;
  onClose: () => void;
  row: TransferRequestListItem | null;
  decision: 'approved' | 'rejected';
  onDecisionChange: (v: 'approved' | 'rejected') => void;
  comment: string;
  onCommentChange: (v: string) => void;
  submitting: boolean;
  onConfirm: () => void | Promise<void>;
}

export function LiquidationDisposalApproveModal({
  open,
  onClose,
  row,
  decision,
  onDecisionChange,
  comment,
  onCommentChange,
  submitting,
  onConfirm,
}: LiquidationDisposalApproveModalProps) {
  if (!open || !row) return null;

  return (
    <div className="mark-damaged-modal-overlay" role="dialog" aria-modal="true">
      <div className="mark-damaged-modal">
        <button type="button" className="mark-damaged-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="mark-damaged-modal__close">×</span>
        </button>

        <div className="mark-damaged-modal__header">
          <h2 className="mark-damaged-modal__title">Phê duyệt yêu cầu thanh lý — {row.code}</h2>
        </div>

        <div className="mark-damaged-modal__body">
          <div className="mark-damaged-modal__content">
            <div className="mark-damaged-form-section">
              <h3 className="mark-damaged-section-title">Quyết định</h3>
              <div className="mark-damaged-form__item">
                <label htmlFor="liq-appr-decision">Phê duyệt</label>
                <select
                  id="liq-appr-decision"
                  className="mark-damaged-input"
                  value={decision}
                  onChange={(e) => onDecisionChange(e.target.value === 'rejected' ? 'rejected' : 'approved')}
                  disabled={submitting}
                >
                  <option value="approved">Phê duyệt</option>
                  <option value="rejected">Từ chối</option>
                </select>
              </div>
              <div className="mark-damaged-form__item">
                <label htmlFor="liq-appr-comment">Ghi chú</label>
                <textarea
                  id="liq-appr-comment"
                  className="mark-damaged-textarea"
                  rows={4}
                  placeholder="Không bắt buộc"
                  value={comment}
                  onChange={(e) => onCommentChange(e.target.value)}
                  disabled={submitting}
                />
              </div>
            </div>
          </div>
        </div>

        <div className="mark-damaged-modal__footer">
          <button type="button" className="mark-damaged-btn-draft" onClick={onClose} disabled={submitting}>
            ← Quay lại
          </button>
          <button type="button" className="mark-damaged-btn-submit" onClick={() => void onConfirm()} disabled={submitting}>
            Xác nhận
          </button>
        </div>
      </div>
    </div>
  );
}
