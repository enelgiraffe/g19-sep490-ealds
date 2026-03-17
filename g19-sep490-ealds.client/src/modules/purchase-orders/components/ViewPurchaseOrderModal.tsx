import { useEffect, useMemo, useState } from 'react';
import { message } from 'antd';
import { purchaseOrderService, type PurchaseOrderDetail } from '../services/purchaseOrderService';
import { assetCapitalizationService } from '../../assets/services/assetCapitalizationService';
import './ViewPurchaseOrderModal.css';

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ duyệt', color: 'warning' },
  2: { label: 'Duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Chờ ngân sách', color: 'warning' },
};

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString('vi-VN');
  } catch {
    return iso;
  }
}

interface ViewPurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  data: PurchaseOrderDetail | null;
  currentUserId?: number | null;
  currentUserRole?: string | null;
  onActionCompleted?: (assetRequestId: number) => void | Promise<void>;
}

export function ViewPurchaseOrderModal({
  open,
  onClose,
  data,
  currentUserId,
  currentUserRole,
  onActionCompleted,
}: ViewPurchaseOrderModalProps) {
  if (!open || !data) return null;

  const [isApproveOpen, setIsApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [capitalizeNote, setCapitalizeNote] = useState('');
  const [capitalizing, setCapitalizing] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editTitle, setEditTitle] = useState(data.title ?? '');
  const [editDescription, setEditDescription] = useState(data.description ?? '');
  const [editDocuments, setEditDocuments] = useState<{ name: string; url: string }[]>([]);
  const [docName, setDocName] = useState('');
  const [docUrl, setDocUrl] = useState('');
  const [savingEdit, setSavingEdit] = useState(false);

  const normalizedRole = String(currentUserRole ?? '').toUpperCase();
  const isAccountantRole = normalizedRole === 'ACCOUNTANT';

  // Accountant action points:
  // - status=0: accountant approves/rejects (forward to director / return to draft)
  // - status=1: accountant already approved => accountant can still update info/attachments
  // - status=1/2: accountant can capitalize asset (if AssetId exists)
  const canAccountantApprove = isAccountantRole && !!currentUserId && data.status === 0;
  const canRecordCapitalization =
    isAccountantRole && !!currentUserId && (data.status === 1 || data.status === 2) && !!data.assetId;
  const canEditAfterAccountantApprove = isAccountantRole && !!currentUserId && data.status === 1;

  const parsedProposedData = useMemo(() => {
    try {
      if (!data.proposedData) return null;
      return JSON.parse(data.proposedData) as any;
    } catch {
      return null;
    }
  }, [data.proposedData]);

  // Init editable fields/docs when opening or data changes (best-effort)
  useEffect(() => {
    if (!open || !data) return;
    setEditTitle(data.title ?? '');
    setEditDescription(data.description ?? '');
    if (parsedProposedData && Array.isArray((parsedProposedData as any).documents)) {
      const docs = (parsedProposedData as any).documents
        .filter((d: any) => d && (d.url || d.fileUrl))
        .map((d: any, idx: number) => ({
          name: String(d.name ?? `Tài liệu ${idx + 1}`),
          url: String(d.url ?? d.fileUrl),
        }));
      setEditDocuments(docs);
    } else {
      setEditDocuments([]);
    }
    setIsEditing(false);
    setDocName('');
    setDocUrl('');
  }, [open, data, parsedProposedData]);

  const handleSubmitApproval = async () => {
    if (!currentUserId) {
      message.error('Không lấy được thông tin người dùng.');
      return;
    }
    setSubmitting(true);
    try {
      if (decision === 'approved') {
        await purchaseOrderService.approveAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Đã chuyển yêu cầu sang giám đốc (Chờ duyệt).');
      } else {
        await purchaseOrderService.rejectAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Đã trả yêu cầu về Nháp.');
      }
      await onActionCompleted?.(data.assetRequestId);
      setIsApproveOpen(false);
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Thao tác phê duyệt thất bại.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleRecordCapitalization = async () => {
    if (!currentUserId) {
      message.error('Không lấy được thông tin người dùng.');
      return;
    }
    if (!data.assetId) {
      message.error('Đơn mua này chưa được gắn AssetId nên chưa thể ghi tăng.');
      return;
    }
    setCapitalizing(true);
    try {
      await assetCapitalizationService.changeStatus({
        assetId: data.assetId,
        note: capitalizeNote.trim() || null,
      });
      message.success('Đã biến đơn mua thành tài sản cố định (ghi tăng).');
      await onActionCompleted?.(data.assetRequestId);
    } catch (e: unknown) {
      const err = e as { response?: { data?: any } };
      const msg = err?.response?.data?.message ?? err?.response?.data ?? 'Ghi tăng tài sản thất bại.';
      message.error(typeof msg === 'string' ? msg : 'Ghi tăng tài sản thất bại.');
    } finally {
      setCapitalizing(false);
    }
  };

  const handleSaveEdit = async () => {
    if (!currentUserId) {
      message.error('Không lấy được thông tin người dùng.');
      return;
    }
    if (!editTitle.trim()) {
      message.error('Vui lòng nhập Lý do đề nghị.');
      return;
    }
    setSavingEdit(true);
    try {
      const nextProposed = {
        ...(parsedProposedData && typeof parsedProposedData === 'object' ? parsedProposedData : {}),
        documents: editDocuments.map((d) => ({ name: d.name, url: d.url })),
      };
      await purchaseOrderService.update(data.assetRequestId, {
        userId: data.userId,
        assetId: data.assetId ?? null,
        title: editTitle.trim(),
        description: editDescription.trim() || null,
        proposedData: JSON.stringify(nextProposed),
        createdBy: currentUserId,
        status: 1,
      });
      message.success('Đã cập nhật đơn mua sau khi kế toán phê duyệt.');
      setIsEditing(false);
      await onActionCompleted?.(data.assetRequestId);
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Cập nhật thất bại.');
    } finally {
      setSavingEdit(false);
    }
  };

  const statusConfig = STATUS_MAP[data.status] ?? STATUS_MAP[0];
  let equipment: { stt: number; name: string; quantity: number; machineCode?: string; unit?: string; estimatedPrice?: string }[] = [];
  let totalPrice = '—';
  try {
    if (data.proposedData) {
      const parsed = JSON.parse(data.proposedData) as {
        equipment?: { name?: string; quantity?: number; machineCode?: string; unit?: string; estimatedPrice?: string }[];
        totalPrice?: string;
      };
      if (Array.isArray(parsed.equipment)) {
        equipment = parsed.equipment.map((e, i) => ({
          stt: i + 1,
          name: e.name ?? '—',
          quantity: e.quantity ?? 1,
          machineCode: e.machineCode,
          unit: e.unit,
          estimatedPrice: e.estimatedPrice,
        }));
      }
      if (parsed.totalPrice) totalPrice = parsed.totalPrice;
    }
  } catch {
    // keep empty
  }

  const statusClassName =
    statusConfig.color === 'success'
      ? 'view-purchase-status-tag view-purchase-status-tag--success'
      : statusConfig.color === 'warning'
      ? 'view-purchase-status-tag view-purchase-status-tag--warning'
      : 'view-purchase-status-tag';

  return (
    <div className="view-purchase-modal-overlay" role="dialog" aria-modal="true">
      <div className="view-purchase-modal">
        <div className="view-purchase-modal__header">
          <div className="view-purchase-modal__header-left">
            <h2 className="view-purchase-modal__title">Chi tiết đơn mua</h2>
            <span className={statusClassName}>{statusConfig.label}</span>
          </div>
        </div>

        <div className="view-purchase-modal__body">
          <div className="view-purchase-modal__content">
            <div className="view-purchase-form">
              {/* Thông tin chung */}
              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Người gửi</label>
                  <div className="view-purchase-form__value">
                    {data.creatorName ?? data.createdBy}
                  </div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Phòng ban</label>
                  <div className="view-purchase-form__value">—</div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Lý do đề nghị</label>
                  {isEditing ? (
                    <input
                      className="approve-purchase-textarea"
                      style={{ height: 40 }}
                      value={editTitle}
                      onChange={(e) => setEditTitle(e.target.value)}
                    />
                  ) : (
                    <div className="view-purchase-form__value">{data.title}</div>
                  )}
                </div>
                <div className="view-purchase-form__field">
                  <label>Thời gian cần vật tư</label>
                  <div className="view-purchase-form__value">{formatDate(data.createDate)}</div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Nhà cung cấp đề xuất</label>
                  <div className="view-purchase-form__value">—</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Loại tài sản</label>
                  <div className="view-purchase-form__value">—</div>
                </div>
              </div>

              {/* Mã yêu cầu & tiêu đề */}
              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Mã yêu cầu</label>
                  <div className="view-purchase-form__value">YC-{data.assetRequestId}</div>
                </div>
              </div>

              {/* Danh mục vật tư */}
              {equipment.length > 0 && (
                <div className="view-purchase-form__section">
                  <h3 className="view-purchase-form__section-title">Danh mục vật tư</h3>
                  <table className="view-purchase-equipment-table">
                    <thead>
                      <tr>
                        <th>STT</th>
                        <th>Tên vật tư</th>
                        <th>Số lượng</th>
                        <th>Mã máy</th>
                        <th>Đơn vị tính</th>
                        <th>Đơn giá dự tính</th>
                      </tr>
                    </thead>
                    <tbody>
                      {equipment.map((item) => (
                        <tr key={item.stt}>
                          <td>{item.stt}</td>
                          <td>{item.name}</td>
                          <td>{item.quantity}</td>
                          <td>{item.machineCode ?? '—'}</td>
                          <td>{item.unit ?? '—'}</td>
                          <td className="view-purchase-equipment-price">
                            {item.estimatedPrice ?? '—'}
                          </td>
                        </tr>
                      ))}
                      <tr className="view-purchase-equipment-total">
                        <td colSpan={5}>Thành tiền</td>
                        <td className="view-purchase-equipment-price">{totalPrice}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              )}

              {data.proposedData && equipment.length === 0 && (
                <div className="view-purchase-form__section">
                  <label>Dữ liệu đề xuất</label>
                  <pre
                    className="view-purchase-form__value"
                    style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}
                  >
                    {data.proposedData}
                  </pre>
                </div>
              )}

              {/* Mục đích sử dụng */}
              {data.description && (
                <div className="view-purchase-form__section">
                  <label>Mục đích sử dụng</label>
                  {isEditing ? (
                    <textarea
                      className="approve-purchase-textarea"
                      value={editDescription}
                      onChange={(e) => setEditDescription(e.target.value)}
                    />
                  ) : (
                    <div className="view-purchase-form__value">{data.description}</div>
                  )}
                </div>
              )}

              {/* Tài liệu đính kèm */}
              <div className="view-purchase-form__section">
                <h3 className="view-purchase-form__section-title">Tài liệu đính kèm</h3>
                <div className="view-purchase-attachments">
                  {editDocuments.length === 0 ? (
                    <div className="view-purchase-form__value">—</div>
                  ) : (
                    editDocuments.map((doc, idx) => (
                      <div key={`${doc.url}-${idx}`} className="view-purchase-attachment-item">
                        <span className="view-purchase-attachment-number">#{idx + 1}</span>
                        <span className="view-purchase-attachment-name">{doc.name || `Tài liệu ${idx + 1}`}</span>
                        <button
                          type="button"
                          className="view-purchase-attachment-download"
                          onClick={() => window.open(doc.url, '_blank')}
                        >
                          Mở
                        </button>
                        {isEditing && (
                          <button
                            type="button"
                            className="view-purchase-attachment-download"
                            onClick={() =>
                              setEditDocuments((prev) => prev.filter((_, i) => i !== idx))
                            }
                          >
                            Xoá
                          </button>
                        )}
                      </div>
                    ))
                  )}
                </div>
                {isEditing && (
                  <div style={{ marginTop: 12, display: 'grid', gap: 8 }}>
                    <input
                      className="approve-purchase-textarea"
                      style={{ height: 40 }}
                      placeholder="Tên tài liệu"
                      value={docName}
                      onChange={(e) => setDocName(e.target.value)}
                    />
                    <input
                      className="approve-purchase-textarea"
                      style={{ height: 40 }}
                      placeholder="Link tài liệu (URL)"
                      value={docUrl}
                      onChange={(e) => setDocUrl(e.target.value)}
                    />
                    <button
                      type="button"
                      className="view-purchase-btn-approve"
                      onClick={() => {
                        const name = docName.trim() || `Tài liệu ${editDocuments.length + 1}`;
                        const url = docUrl.trim();
                        if (!url) {
                          message.error('Vui lòng nhập URL tài liệu.');
                          return;
                        }
                        setEditDocuments((prev) => [...prev, { name, url }]);
                        setDocName('');
                        setDocUrl('');
                      }}
                    >
                      Thêm tài liệu
                    </button>
                  </div>
                )}
              </div>

              {isAccountantRole && data.status === 1 && (
                <div className="view-purchase-form__section">
                  <h3 className="view-purchase-form__section-title">Kế toán xử lý sau duyệt</h3>
                  <div className="view-purchase-form__row">
                    <div className="view-purchase-form__field">
                      <label>Ghi chú ghi tăng (không bắt buộc)</label>
                      <textarea
                        className="approve-purchase-textarea"
                        placeholder="Không cần thiết"
                        value={capitalizeNote}
                        onChange={(e) => setCapitalizeNote(e.target.value)}
                      />
                      {!data.assetId && (
                        <div className="view-purchase-form__value" style={{ marginTop: 8 }}>
                          Chưa có <b>AssetId</b> gắn với đơn mua nên chưa thể ghi tăng.
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )}

              {/* Ghi chú của người gửi */}
              {data.description && (
                <div className="view-purchase-form__section">
                  <div className="view-purchase-feedback-box">
                    <label>Ghi chú của người gửi</label>
                    <div className="view-purchase-feedback-content">{data.description}</div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="view-purchase-modal__footer">
          <button
            type="button"
            onClick={onClose}
            className="view-purchase-btn-close"
          >
            Quay lại
          </button>
          {canEditAfterAccountantApprove && (
            <>
              {isEditing ? (
                <>
                  <button
                    type="button"
                    className="view-purchase-btn-approve"
                    disabled={savingEdit}
                    onClick={handleSaveEdit}
                  >
                    <span>{savingEdit ? 'Đang lưu...' : 'Lưu thay đổi'}</span>
                  </button>
                  <button
                    type="button"
                    className="view-purchase-btn-close"
                    onClick={() => {
                      setIsEditing(false);
                      setEditTitle(data.title ?? '');
                      setEditDescription(data.description ?? '');
                    }}
                  >
                    Huỷ
                  </button>
                </>
              ) : (
                <button
                  type="button"
                  className="view-purchase-btn-approve"
                  onClick={() => setIsEditing(true)}
                >
                  <span>Chỉnh sửa</span>
                </button>
              )}
            </>
          )}
          {canRecordCapitalization && (
            <button
              type="button"
              className="view-purchase-btn-approve"
              disabled={capitalizing}
              onClick={handleRecordCapitalization}
            >
              <span className="view-purchase-btn-approve-icon">🏷</span>
              <span>{capitalizing ? 'Đang ghi tăng...' : 'Biến thành tài sản cố định'}</span>
            </button>
          )}
          {canAccountantApprove && (
            <button
              type="button"
              className="view-purchase-btn-approve"
              onClick={() => setIsApproveOpen(true)}
            >
              <span className="view-purchase-btn-approve-icon">📋</span>
              <span>Phê duyệt</span>
            </button>
          )}
        </div>
      </div>

      {canAccountantApprove && isApproveOpen && (
        <div
          className="approve-purchase-modal-overlay"
          role="dialog"
          aria-modal="true"
        >
          <div className="approve-purchase-modal">
            <div className="approve-purchase-modal__header">
              <h3 className="approve-purchase-modal__title">Phê duyệt đơn</h3>
            </div>

            <div className="approve-purchase-modal__body">
              <div className="approve-purchase-form">
                <div className="approve-purchase-form__row">
                  <div className="approve-purchase-form__field">
                    <label>Phê duyệt</label>
                    <select
                      className="approve-purchase-select"
                      value={decision}
                      onChange={(e) =>
                        setDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')
                      }
                    >
                      <option value="approved">Phê duyệt</option>
                      <option value="rejected">Từ chối</option>
                    </select>
                  </div>
                  <div className="approve-purchase-form__field">
                    <label>Ghi chú</label>
                    <textarea
                      className="approve-purchase-textarea"
                      placeholder="Không cần thiết"
                      value={comment}
                      onChange={(e) => setComment(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="approve-purchase-modal__footer">
              <button
                type="button"
                className="approve-purchase-btn-back"
                onClick={() => setIsApproveOpen(false)}
              >
                ← Quay lại
              </button>
              <button
                type="button"
                className="approve-purchase-btn-approve"
                disabled={submitting}
                onClick={handleSubmitApproval}
              >
                <span className="approve-purchase-btn-approve-icon">📋</span>
                <span>Phê duyệt</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
