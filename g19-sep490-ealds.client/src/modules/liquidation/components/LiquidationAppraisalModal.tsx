import { useEffect, useState } from 'react';
import { message } from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import {
  disposalExecutionService,
  type DisposalExecutionDto,
} from '../../requests/services/disposalExecutionService';
import { parseIntegerMoneyInput } from '../../../shared/utils/moneyInput';
import './LiquidationAppraisalModal.css';
import '../../assets/components/MarkDamagedAssetModal.css';

interface AppraisalMember {
  id: string;
  name: string;
  position: string;
}

function axiosErrorDetail(e: unknown): string | null {
  const err = e as { response?: { data?: unknown } };
  const d = err?.response?.data;
  if (typeof d === 'string') return d;
  if (d && typeof d === 'object') {
    const o = d as Record<string, unknown>;
    if (typeof o.detail === 'string') return o.detail;
    if (typeof o.title === 'string') return o.title;
    if (typeof o.message === 'string') return o.message;
  }
  return null;
}

export interface LiquidationAppraisalModalProps {
  open: boolean;
  assetRequestId: number | null;
  requestCode?: string;
  assetName?: string;
  userId: number | undefined;
  onClose: () => void;
  onSuccess: () => void | Promise<void>;
}

export function LiquidationAppraisalModal({
  open,
  assetRequestId,
  requestCode,
  assetName,
  userId,
  onClose,
  onSuccess,
}: LiquidationAppraisalModalProps) {
  const [hydrating, setHydrating] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [dto, setDto] = useState<DisposalExecutionDto | null>(null);

  // Thông tin chung
  const [appraisalDate, setAppraisalDate] = useState<Dayjs | null>(null);
  const [appraisalLocation, setAppraisalLocation] = useState('');
  const [minutesNo, setMinutesNo] = useState('');

  // Thành phần tham gia (hội đồng định giá)
  const [members, setMembers] = useState<AppraisalMember[]>([
    { id: '1', name: '', position: '' },
  ]);

  // Chi tiết tài sản
  const [assetSpecs, setAssetSpecs] = useState('');
  const [assetCondition, setAssetCondition] = useState('');
  const [assetOrigin, setAssetOrigin] = useState('');

  // Kết quả định giá
  const [appraisedValueText, setAppraisedValueText] = useState('');
  const [appraisalConclusion, setAppraisalConclusion] = useState('');

  useEffect(() => {
    if (!open || assetRequestId == null) {
      setDto(null);
      setLoadError(null);
      return;
    }
    let cancelled = false;

    // Reset form
    setAppraisalDate(null);
    setAppraisalLocation('');
    setMinutesNo('');
    setMembers([{ id: '1', name: '', position: '' }]);
    setAssetSpecs('');
    setAssetCondition('');
    setAssetOrigin('');
    setAppraisedValueText('');
    setAppraisalConclusion('');
    setLoadError(null);

    // Khởi tạo DTO mặc định
    setDto({
      assetRequestId,
      disposalExecutionId: null,
      status: 0,
      canEdit: true,
      canFinalize: false,
      assetRequestStatus: 2,
      blockFinalizeReason: null,
    });

    // Load bản nháp nếu có
    setHydrating(true);
    disposalExecutionService
      .getByAssetRequest(assetRequestId)
      .then((d) => {
        if (cancelled) return;
        setDto(d);
        setLoadError(null);
        
        // Hydrate dữ liệu đã lưu
        setAppraisalDate(d.plannedExecutionDate ? dayjs(d.plannedExecutionDate) : null);
        setMinutesNo(d.minutesNo ?? '');
        setAppraisedValueText(
          d.actualDisposalValue != null && !Number.isNaN(Number(d.actualDisposalValue))
            ? Math.floor(Number(d.actualDisposalValue)).toLocaleString('en-US')
            : '',
        );
        setAppraisalConclusion(d.executionNote ?? '');
        
        // Parse thông tin từ executionNote nếu có format đặc biệt
        // (có thể lưu JSON hoặc format khác trong executionNote)
      })
      .catch((e) => {
        if (cancelled) return;
        const detail = axiosErrorDetail(e);
        setLoadError(detail ?? 'Không đọc được dữ liệu từ máy chủ.');
        message.warning(
          'Không tải được bản nháp đã lưu; bạn vẫn có thể nhập form mới.',
          5,
        );
      })
      .finally(() => {
        if (!cancelled) setHydrating(false);
      });
    return () => {
      cancelled = true;
    };
  }, [open, assetRequestId]);

  if (!open || assetRequestId == null) return null;

  const completed = (dto?.status ?? 0) >= 2;
  const canEdit = !completed && (dto?.canEdit ?? true);

  const handleAddMember = () => {
    const newId = String(Date.now());
    setMembers([...members, { id: newId, name: '', position: '' }]);
  };

  const handleRemoveMember = (id: string) => {
    if (members.length <= 1) {
      message.warning('Phải có ít nhất 1 thành viên hội đồng.');
      return;
    }
    setMembers(members.filter((m) => m.id !== id));
  };

  const handleMemberChange = (id: string, field: 'name' | 'position', value: string) => {
    setMembers(members.map((m) => (m.id === id ? { ...m, [field]: value } : m)));
  };

  const buildAppraisalData = () => {
    const appraisedValue = parseIntegerMoneyInput(appraisedValueText);
    
    // Gộp thông tin vào executionNote dạng JSON để lưu
    const appraisalData = {
      location: appraisalLocation.trim() || null,
      members: members.filter(m => m.name.trim() || m.position.trim()),
      assetSpecs: assetSpecs.trim() || null,
      assetCondition: assetCondition.trim() || null,
      assetOrigin: assetOrigin.trim() || null,
      conclusion: appraisalConclusion.trim() || null,
    };

    return {
      userId: userId!,
      appraisalDate: appraisalDate?.toISOString() ?? null,
      appraisalMinutesNo: minutesNo.trim() || null,
      appraisalConclusion: JSON.stringify(appraisalData),
      appraisedValue,
    };
  };

  const handleSaveDraft = async () => {
    if (!userId) return;
    setSaving(true);
    try {
      const payload = buildAppraisalData();
      // Tạm thời lưu vào các trường hiện có
      const savePayload = {
        userId,
        plannedExecutionDate: payload.appraisalDate,
        executedDate: null,
        executionMethod: null,
        buyerName: null,
        buyerContact: null,
        contractNo: null,
        invoiceNo: null,
        minutesNo: payload.appraisalMinutesNo,
        actualDisposalValue: payload.appraisedValue,
        expenseValue: null,
        attachmentUrls: null,
        executionNote: payload.appraisalConclusion,
      };
      
      const next = await disposalExecutionService.save(assetRequestId, savePayload);
      setDto(next);
      message.success('Đã lưu nháp biên bản thẩm định.');
    } catch (e) {
      const detail = axiosErrorDetail(e);
      message.error(detail?.slice(0, 200) ?? 'Lưu nháp thất bại.');
    } finally {
      setSaving(false);
    }
  };

  const handleRecordAppraisal = async () => {
    if (!userId) return;
    
    // Validate
    if (!appraisalDate) {
      message.error('Vui lòng nhập ngày thẩm định.');
      return;
    }
    if (!minutesNo.trim()) {
      message.error('Vui lòng nhập số biên bản thẩm định.');
      return;
    }
    if (!appraisedValueText.trim()) {
      message.error('Vui lòng nhập giá trị định giá.');
      return;
    }
    const hasValidMember = members.some(m => m.name.trim());
    if (!hasValidMember) {
      message.error('Vui lòng nhập ít nhất 1 thành viên hội đồng.');
      return;
    }

    setSaving(true);
    try {
      const payload = buildAppraisalData();
      const next = await disposalExecutionService.recordAppraisal(assetRequestId, {
        userId,
        appraisalDate: payload.appraisalDate!,
        appraisalMinutesNo: payload.appraisalMinutesNo,
        appraisalConclusion: payload.appraisalConclusion,
      });
      setDto(next);
      message.success('Đã ghi nhận biên bản thẩm định.');
      await onSuccess();
      onClose();
    } catch (e) {
      message.error(axiosErrorDetail(e)?.slice(0, 200) ?? 'Ghi nhận thẩm định thất bại.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mark-damaged-modal-overlay" role="dialog" aria-modal="true">
      <div className="mark-damaged-modal liquidation-appraisal-modal">
        <button type="button" className="mark-damaged-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="mark-damaged-modal__close">×</span>
        </button>

        <div className="mark-damaged-modal__header">
          <h2 className="mark-damaged-modal__title">
            Biên bản thẩm định tài sản — {requestCode ?? `YC-${assetRequestId}`}
          </h2>
        </div>

        <div className="mark-damaged-modal__body">
          <div className="mark-damaged-modal__content">
            {hydrating && (
              <p style={{ marginBottom: 8, fontSize: 13 }}>Đang kiểm tra bản nháp đã lưu (nếu có)…</p>
            )}
            {loadError && (
              <p className="appraisal-hint-error" style={{ marginBottom: 12 }}>
                Không đọc được bản nháp: {loadError.slice(0, 200)}
              </p>
            )}
            {completed && (
              <p style={{ marginBottom: 12, color: '#16a34a' }}>Đã hoàn tất ghi nhận biên bản thẩm định.</p>
            )}

            {/* I. Thông tin chung */}
            <div className="mark-damaged-form-section">
              <h3 className="mark-damaged-section-title">I. Thông tin chung</h3>
              <div className="appraisal-form-grid">
                <div className="mark-damaged-form__item">
                  <label htmlFor="appr-date">
                    Ngày thẩm định<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    id="appr-date"
                    type="date"
                    className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                    disabled={!canEdit}
                    value={appraisalDate ? appraisalDate.format('YYYY-MM-DD') : ''}
                    onChange={(e) => setAppraisalDate(e.target.value ? dayjs(e.target.value) : null)}
                  />
                </div>
                <div className="mark-damaged-form__item">
                  <label htmlFor="appr-location">Địa điểm lập biên bản</label>
                  <input
                    id="appr-location"
                    type="text"
                    className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                    disabled={!canEdit}
                    value={appraisalLocation}
                    onChange={(e) => setAppraisalLocation(e.target.value)}
                    placeholder="Ví dụ: Phòng họp tầng 2"
                  />
                </div>
                <div className="mark-damaged-form__item">
                  <label htmlFor="appr-minutes">
                    Số biên bản<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    id="appr-minutes"
                    type="text"
                    className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                    disabled={!canEdit}
                    value={minutesNo}
                    onChange={(e) => setMinutesNo(e.target.value)}
                    placeholder="Ví dụ: BB-ĐG-001/2026"
                  />
                </div>
              </div>
            </div>

            {/* II. Thành phần tham gia (Hội đồng định giá) */}
            <div className="mark-damaged-form-section" style={{ marginTop: 16 }}>
              <h3 className="mark-damaged-section-title">II. Thành phần tham gia (Hội đồng định giá)</h3>
              <div className="appraisal-members-list">
                {members.map((member, index) => (
                  <div key={member.id} className="appraisal-member-row">
                    <div className="appraisal-member-index">{index + 1}</div>
                    <div className="appraisal-member-fields">
                      <input
                        type="text"
                        className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                        disabled={!canEdit}
                        placeholder="Họ và tên"
                        value={member.name}
                        onChange={(e) => handleMemberChange(member.id, 'name', e.target.value)}
                      />
                      <input
                        type="text"
                        className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                        disabled={!canEdit}
                        placeholder="Chức vụ"
                        value={member.position}
                        onChange={(e) => handleMemberChange(member.id, 'position', e.target.value)}
                      />
                    </div>
                    {canEdit && (
                      <button
                        type="button"
                        className="appraisal-member-remove"
                        onClick={() => handleRemoveMember(member.id)}
                        disabled={members.length <= 1}
                        aria-label="Xóa thành viên"
                      >
                        ×
                      </button>
                    )}
                  </div>
                ))}
              </div>
              {canEdit && (
                <button
                  type="button"
                  className="appraisal-add-member-btn"
                  onClick={handleAddMember}
                >
                  + Thêm thành viên
                </button>
              )}
            </div>

            {/* III. Chi tiết tài sản */}
            <div className="mark-damaged-form-section" style={{ marginTop: 16 }}>
              <h3 className="mark-damaged-section-title">III. Chi tiết tài sản</h3>
              <div className="mark-damaged-form__item">
                <label htmlFor="appr-asset-name">Tên tài sản</label>
                <input
                  id="appr-asset-name"
                  type="text"
                  className="mark-damaged-input--disabled"
                  disabled
                  value={assetName ?? ''}
                />
              </div>
              <div className="mark-damaged-form__item">
                <label htmlFor="appr-specs">Đặc điểm kỹ thuật</label>
                <textarea
                  id="appr-specs"
                  className="mark-damaged-textarea"
                  rows={2}
                  readOnly={!canEdit}
                  disabled={!canEdit}
                  value={assetSpecs}
                  onChange={(e) => setAssetSpecs(e.target.value)}
                  placeholder="Mô tả đặc điểm kỹ thuật của tài sản"
                />
              </div>
              <div className="mark-damaged-form__item">
                <label htmlFor="appr-condition">Tình trạng chất lượng</label>
                <textarea
                  id="appr-condition"
                  className="mark-damaged-textarea"
                  rows={2}
                  readOnly={!canEdit}
                  disabled={!canEdit}
                  value={assetCondition}
                  onChange={(e) => setAssetCondition(e.target.value)}
                  placeholder="Mô tả tình trạng hiện tại của tài sản"
                />
              </div>
              <div className="mark-damaged-form__item">
                <label htmlFor="appr-origin">Nguồn gốc tài sản</label>
                <input
                  id="appr-origin"
                  type="text"
                  className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                  disabled={!canEdit}
                  value={assetOrigin}
                  onChange={(e) => setAssetOrigin(e.target.value)}
                  placeholder="Ví dụ: Mua từ nhà cung cấp X năm 2020"
                />
              </div>
            </div>

            {/* IV. Kết quả định giá */}
            <div className="mark-damaged-form-section" style={{ marginTop: 16 }}>
              <h3 className="mark-damaged-section-title">IV. Kết quả định giá</h3>
              <div className="mark-damaged-form__item">
                <label htmlFor="appr-value">
                  Giá trị định giá (VNĐ)<span style={{ color: '#ef4444' }}>*</span>
                </label>
                <div className="disposal-appraisal-money-input">
                  <input
                    id="appr-value"
                    type="text"
                    inputMode="numeric"
                    className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                    disabled={!canEdit}
                    value={appraisedValueText}
                    onChange={(e) => {
                      const n = parseIntegerMoneyInput(e.target.value);
                      setAppraisedValueText(n == null ? '' : Math.floor(n).toLocaleString('en-US'));
                    }}
                    placeholder="0"
                  />
                  <span className="disposal-appraisal-money-suffix">đ</span>
                </div>
              </div>
              <div className="mark-damaged-form__item">
                <label htmlFor="appr-conclusion">Kết luận thẩm định</label>
                <textarea
                  id="appr-conclusion"
                  className="mark-damaged-textarea"
                  rows={4}
                  readOnly={!canEdit}
                  disabled={!canEdit}
                  value={appraisalConclusion}
                  onChange={(e) => setAppraisalConclusion(e.target.value)}
                  placeholder="Nhập kết luận, nhận xét của hội đồng thẩm định"
                />
              </div>
            </div>
          </div>
        </div>

        <div className="mark-damaged-modal__footer">
          <button type="button" className="mark-damaged-btn-draft" onClick={onClose} disabled={saving}>
            Đóng
          </button>
          {canEdit && (
            <>
              <button
                type="button"
                className="mark-damaged-btn-draft"
                disabled={saving || !userId}
                onClick={() => void handleSaveDraft()}
              >
                {saving ? 'Đang lưu...' : 'Lưu nháp'}
              </button>
              <button
                type="button"
                className="mark-damaged-btn-submit"
                disabled={saving || !userId}
                onClick={() => void handleRecordAppraisal()}
              >
                Ghi nhận biên bản thẩm định
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
