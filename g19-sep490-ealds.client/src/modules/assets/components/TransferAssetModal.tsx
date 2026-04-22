import { useEffect, useRef, useState } from 'react';
import { message } from 'antd';
import { assetInstanceService, getStatusLabel, type AssetInstanceResponse } from '../services/assetService';
import { transferRequestService, type AssetLocationOption } from '../services/transferRequestService';
import { SelectAssetsModal, type SelectableAsset } from './SelectAssetsModal';
import './TransferAssetModal.css';

/** Local calendar date YYYY-MM-DD (avoids UTC off-by-one with `toISOString().slice(0, 10)`). */
function todayLocalISODate(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function instanceToSelectable(a: AssetInstanceResponse): SelectableAsset {
  const locationLabel =
    a.currentLocationId != null ? `AL-${a.currentLocationId}` : '—';
  return {
    assetId: a.assetInstanceId,
    assetCatalogCode: (a.assetCode && a.assetCode.trim()) || '—',
    code: a.instanceCode,
    name:
      (a.assetName && a.assetName.trim()) || (a.assetCode && a.assetCode.trim()) || a.instanceCode,
    assetTypeName: (a.assetTypeName && a.assetTypeName.trim()) || '—',
    locationLabel,
    statusLabel: getStatusLabel(a.statusName),
    quantity: 1,
    currentDepartmentName: a.currentDepartmentName ?? '—',
    currentDepartmentId: a.currentDepartmentId ?? null,
  };
}

export interface TransferEditDraft {
  assetRequestId: number;
  draftFormJson: string;
}

interface AssetInfo {
  assetInstanceId?: number;
  assetId?: number;
  /** Mã tài sản (catalog). */
  assetCatalogCode?: string;
  code: string;
  name: string;
  type: string;
  specification: string;
  purchaseDate: string;
  warrantyExpiry: string;
  currentValue: string;
  remainingValue: string;
  location: string;
  status: string;
  admissionDate: string;
  department: string;
  currentDepartmentId?: number | null;
}

interface TransferAssetModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: any) => void;
  assetInfo: AssetInfo | null;
  fromDepartmentId?: number | null;
  /** When true with fromDepartmentId, "Từ phòng ban" is fixed (e.g. trưởng phòng chỉ điều chuyển từ đơn vị mình). */
  lockFromDepartment?: boolean;
  mode?: 'location' | 'department';
  /** Optional, stored in draft JSON for context; không còn dùng để bắt buộc chọn phòng ban. */
  currentUserDepartmentId?: number | null;
  /** Mở để tiếp tục sửa bản nháp chưa hoàn tất. */
  editDraft?: TransferEditDraft | null;
}

export function TransferAssetModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
  fromDepartmentId,
  lockFromDepartment = false,
  mode = 'location',
  currentUserDepartmentId,
  editDraft = null,
}: TransferAssetModalProps) {
  const isHydratingFromEdit = useRef(false);
  const [locations, setLocations] = useState<AssetLocationOption[]>([]);
  const [locationsLoading, setLocationsLoading] = useState(false);
  const [transferDate, setTransferDate] = useState<string>('');
  const [recordNumber, setRecordNumber] = useState<string>('');
  const [reason, setReason] = useState<string>('');
  const [fromLocationId, setFromLocationId] = useState<string>('');
  const [toLocationId, setToLocationId] = useState<string>('');
  const [dateError, setDateError] = useState<string | null>(null);
  const [fromError, setFromError] = useState<string | null>(null);
  const [toError, setToError] = useState<string | null>(null);
  const [assetsError, setAssetsError] = useState<string | null>(null);
  const [isSelectAssetsOpen, setIsSelectAssetsOpen] = useState(false);
  const [selectedAssets, setSelectedAssets] = useState<SelectableAsset[]>([]);

  useEffect(() => {
    if (open) {
      setLocationsLoading(true);
      transferRequestService
        .getAssetLocations()
        .then(setLocations)
        .catch(() => setLocations([]))
        .finally(() => setLocationsLoading(false));
    }
  }, [open]);

  useEffect(() => {
    if (!open) return;
    if (editDraft?.draftFormJson) {
      isHydratingFromEdit.current = true;
      let cancelled = false;
      (async () => {
        try {
          const parsed = JSON.parse(editDraft.draftFormJson) as {
            v?: number;
            recordNumber?: string | null;
            transferDate?: string | null;
            reason?: string | null;
            fromLocationId?: string | null;
            toLocationId?: string | null;
            assetIds?: number[];
            mode?: string;
          };
          if (cancelled) return;
          const minD = todayLocalISODate();
          if (typeof parsed.recordNumber === 'string' && parsed.recordNumber.trim()) {
            setRecordNumber(parsed.recordNumber.trim());
          } else {
            const part = minD.replace(/-/g, '');
            const r = Math.floor(Math.random() * 900 + 100);
            setRecordNumber(`BB-DC-${part}-${r}`);
          }
          if (typeof parsed.transferDate === 'string' && parsed.transferDate) {
            const d = parsed.transferDate.slice(0, 10);
            setTransferDate(d < minD ? minD : d);
          } else {
            setTransferDate(minD);
          }
          setReason(typeof parsed.reason === 'string' ? parsed.reason : '');
          setFromLocationId(
            parsed.fromLocationId != null && String(parsed.fromLocationId) !== ''
              ? String(parsed.fromLocationId)
              : '',
          );
          setToLocationId(
            parsed.toLocationId != null && String(parsed.toLocationId) !== ''
              ? String(parsed.toLocationId)
              : '',
          );
          const ids = Array.isArray(parsed.assetIds)
            ? parsed.assetIds.map((n) => Number(n)).filter((n) => Number.isFinite(n) && n > 0)
            : [];
          if (ids.length > 0) {
            const got = await Promise.all(
              ids.map((id) => assetInstanceService.getById(id).catch(() => null)),
            );
            if (cancelled) return;
            setSelectedAssets(
              got
                .filter((x): x is AssetInstanceResponse => x != null)
                .map((x) => instanceToSelectable(x)),
            );
          } else {
            setSelectedAssets([]);
          }
          setDateError(null);
          setFromError(null);
          setToError(null);
          setAssetsError(null);
        } catch {
          if (!cancelled) message.error('Không đọc được bản nháp đã lưu.');
        } finally {
          if (!cancelled) {
            queueMicrotask(() => {
              isHydratingFromEdit.current = false;
            });
          }
        }
      })();
      return () => {
        cancelled = true;
      };
    }
    isHydratingFromEdit.current = false;
    const today = todayLocalISODate();
    const datePart = today.replace(/-/g, '');
    const randomPart = Math.floor(Math.random() * 900 + 100);
    setRecordNumber(`BB-DC-${datePart}-${randomPart}`);
    setTransferDate(today);
    setReason('');
    setDateError(null);
    setFromError(null);
    setToError(null);
    setAssetsError(null);
    setSelectedAssets([]);
    if (fromDepartmentId != null) {
      setFromLocationId(String(fromDepartmentId));
    } else {
      setFromLocationId('');
    }
    setToLocationId('');
  }, [open, fromDepartmentId, editDraft?.assetRequestId, editDraft?.draftFormJson]);

  useEffect(() => {
    if (!open) return;
    if (editDraft?.draftFormJson) return;
    if (assetInfo) {
      const mapped: SelectableAsset = {
        assetId: assetInfo.assetInstanceId ?? assetInfo.assetId ?? -1,
        assetCatalogCode: assetInfo.assetCatalogCode?.trim() || '—',
        code: assetInfo.code,
        name: assetInfo.name,
        assetTypeName: assetInfo.type?.trim() || '—',
        locationLabel: assetInfo.location ?? '—',
        statusLabel: assetInfo.status ?? '—',
        quantity: 1,
        currentDepartmentName: assetInfo.department ?? '—',
        currentDepartmentId: assetInfo.currentDepartmentId ?? null,
      };
      if (mapped.assetId > 0) setSelectedAssets([mapped]);
    }
  }, [open, assetInfo, editDraft?.draftFormJson]);

  useEffect(() => {
    if (!open) return;
    if (isHydratingFromEdit.current) return;
    const deptId = fromLocationId ? Number(fromLocationId) : NaN;
    if (!Number.isFinite(deptId) || deptId <= 0) {
      if (!assetInfo) setSelectedAssets([]);
      return;
    }
    setSelectedAssets((prev) => prev.filter((a) => a.currentDepartmentId === deptId));
  }, [open, fromLocationId, assetInfo]);

  useEffect(() => {
    if (!open) return;
    if (isHydratingFromEdit.current) return;
    if (!fromLocationId || !toLocationId) return;
    if (fromLocationId === toLocationId) {
      setToLocationId('');
      setToError(null);
    }
  }, [open, fromLocationId, toLocationId]);

  const fromDeptNum = fromLocationId ? Number(fromLocationId) : NaN;
  const validFromDepartmentId =
    Number.isFinite(fromDeptNum) && fromDeptNum > 0 ? fromDeptNum : null;
  const fromSelectLocked =
    lockFromDepartment &&
    fromDepartmentId != null &&
    fromDepartmentId > 0;
  const canPickAssets = !!assetInfo || validFromDepartmentId != null;
  /** In department mode, picker only loads instances currently in the selected "Từ phòng ban". */
  const restrictPickerToDepartmentId =
    mode === 'department' && validFromDepartmentId != null ? validFromDepartmentId : null;

  const minTransferDate = todayLocalISODate();
  const toLocationOptions =
    validFromDepartmentId != null
      ? locations.filter((loc) => loc.locationId !== validFromDepartmentId)
      : locations;

  const isTransferFormComplete = (): boolean => {
    if (!transferDate || transferDate < minTransferDate) return false;
    if (!selectedAssets.some((a) => a.assetId > 0)) return false;
    if (!fromLocationId || !toLocationId) return false;
    if (fromLocationId === toLocationId) return false;
    return true;
  };

  const buildDraftFormJson = (): string => {
    const o = {
      v: 1,
      recordNumber,
      transferDate: transferDate || null,
      reason: reason.trim() || null,
      fromLocationId: fromLocationId || null,
      toLocationId: toLocationId || null,
      mode,
      assetIds: selectedAssets.map((a) => a.assetId).filter((id) => id > 0),
      currentUserDepartmentId: currentUserDepartmentId ?? null,
    };
    return JSON.stringify(o);
  };

  const runValidation = (): boolean => {
    let hasError = false;
    if (!transferDate) {
      setDateError('Vui lòng chọn ngày điều chuyển');
      hasError = true;
    } else if (transferDate < minTransferDate) {
      setDateError('Không được chọn ngày trong quá khứ');
      hasError = true;
    }
    const hasSelectedAssets = selectedAssets.some((a) => a.assetId > 0);
    if (!hasSelectedAssets) {
      setAssetsError('Vui lòng chọn ít nhất một cá thể.');
      hasError = true;
    } else {
      setAssetsError(null);
    }
    if (!fromLocationId) {
      setFromError(mode === 'department' ? 'Chọn phòng ban nguồn' : 'Chọn vị trí nguồn');
      hasError = true;
    }
    if (!toLocationId) {
      setToError(mode === 'department' ? 'Chọn phòng ban đích' : 'Chọn vị trí đích');
      hasError = true;
    }
    return !hasError;
  };

  const buildPayload = (saveAsDraft: boolean) => {
    const transferDateValue = transferDate ? new Date(transferDate) : undefined;
    const repId = editDraft?.assetRequestId;
    return {
      transferDate: transferDateValue,
      reason: reason.trim() || undefined,
      fromLocationId,
      toLocationId,
      assetIds: selectedAssets.map((a) => a.assetId).filter((id) => id > 0),
      saveAsDraft,
      incompleteDraft: false,
      /** Parent passes to API on first create to remove the prior incomplete draft. */
      replaceIncompleteAssetRequestId:
        repId != null && Number.isFinite(repId) && repId > 0 ? repId : null,
    };
  };

  const handleSubmit = () => {
    if (!runValidation()) return;
    onSubmit(buildPayload(false));
    onClose();
  };

  const handleSaveDraft = () => {
    if (isTransferFormComplete()) {
      if (!runValidation()) return;
      onSubmit(buildPayload(true));
    } else {
      onSubmit({
        incompleteDraft: true,
        saveAsDraft: true,
        draftFormJson: buildDraftFormJson(),
        editingAssetRequestId: editDraft?.assetRequestId,
      });
    }
    onClose();
  };

  if (!open) return null;

  return (
    <div className="transfer-modal-overlay" role="dialog" aria-modal="true">
      <div className="transfer-modal">
        <button
          type="button"
          className="transfer-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="transfer-modal__close">×</span>
        </button>

        <div className="transfer-modal__header">
          <h2 className="transfer-modal__title">
            {editDraft ? 'Chỉnh sửa bản nháp điều chuyển' : 'Yêu cầu điều chuyển'}
          </h2>
        </div>

        <div className="transfer-modal__body">
          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Thông tin chung</h3>

            <div className="transfer-form__row">
              <div className="transfer-form__item">
                <label htmlFor="transfer-record-number">Số biên bản</label>
                <input
                  id="transfer-record-number"
                  type="text"
                  value={recordNumber}
                  readOnly
                  className="transfer-input transfer-input--disabled"
                />
              </div>
              <div className="transfer-form__item">
                <label htmlFor="transfer-date">
                  Ngày điều chuyển<span className="transfer-required">*</span>
                </label>
                <input
                  id="transfer-date"
                  type="date"
                  className="transfer-input"
                  min={minTransferDate}
                  value={transferDate}
                  onChange={(e) => {
                    setTransferDate(e.target.value);
                    setDateError(null);
                  }}
                />
                {dateError && <div className="transfer-error-text">{dateError}</div>}
              </div>
            </div>

            <div className="transfer-form__item transfer-form__item--full">
              <label htmlFor="transfer-reason">Lý do điều chuyển</label>
              <textarea
                id="transfer-reason"
                className="transfer-textarea"
                rows={3}
                placeholder="Nhập lý do điều chuyển"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </div>
          </div>

          <div className="transfer-form__section transfer-form__section--locations">
            <h3 className="transfer-section-title">
              {mode === 'department' ? 'Chuyển phòng ban sử dụng' : 'Điều chuyển'}
            </h3>
            <div className="transfer-form__row">
              <div className="transfer-form__item">
                <label htmlFor="transfer-from-location">
                  {mode === 'department' ? 'Từ phòng ban' : 'Từ vị trí'}
                  <span className="transfer-required">*</span>
                </label>
                <select
                  id="transfer-from-location"
                  className="transfer-select"
                  value={fromLocationId}
                  disabled={locationsLoading || fromSelectLocked}
                  aria-readonly={fromSelectLocked || undefined}
                  onChange={(e) => {
                    setFromLocationId(e.target.value);
                    setFromError(null);
                  }}
                >
                  <option value="">
                    {mode === 'department' ? 'Chọn phòng ban nguồn' : 'Chọn vị trí hiện tại'}
                  </option>
                  {locations.map((loc) => (
                    <option key={loc.locationId} value={loc.locationId}>
                      {loc.displayName}
                    </option>
                  ))}
                </select>
                {fromError && <div className="transfer-error-text">{fromError}</div>}
              </div>

              <div className="transfer-form__item">
                <label htmlFor="transfer-to-location">
                  {mode === 'department' ? 'Đến phòng ban' : 'Đến vị trí'}
                  <span className="transfer-required">*</span>
                </label>
                <select
                  id="transfer-to-location"
                  className="transfer-select"
                  value={toLocationId}
                  disabled={locationsLoading}
                  onChange={(e) => {
                    setToLocationId(e.target.value);
                    setToError(null);
                  }}
                >
                  <option value="">
                    {mode === 'department' ? 'Chọn phòng ban đích' : 'Chọn vị trí chuyển đến'}
                  </option>
                  {toLocationOptions.map((loc) => (
                    <option key={loc.locationId} value={loc.locationId}>
                      {loc.displayName}
                    </option>
                  ))}
                </select>
                {toError && <div className="transfer-error-text">{toError}</div>}
              </div>
            </div>
          </div>

          <div className="transfer-form__section">
            <h3 className="transfer-section-title">
              Cá thể được chuyển<span className="transfer-required">*</span>
            </h3>
            <div className="transfer-asset-actions">
              <button
                type="button"
                className="transfer-btn-pick-asset"
                disabled={!canPickAssets}
                title={
                  !canPickAssets
                    ? 'Vui lòng chọn phòng ban / vị trí nguồn trước khi chọn cá thể'
                    : undefined
                }
                onClick={() => {
                  if (!canPickAssets) return;
                  setIsSelectAssetsOpen(true);
                }}
              >
                Chọn cá thể
              </button>
            </div>
            {selectedAssets.length > 0 ? (
              <div className="transfer-asset-table">
                <table>
                  <thead>
                    <tr>
                      <th>STT</th>
                      <th>Mã cá thể</th>
                      <th>Tài sản</th>
                      <th>Vị trí cá thể</th>
                      <th>Tình trạng</th>
                      <th>Số lượng</th>
                      <th>Phòng ban sử dụng</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedAssets.map((a, idx) => (
                      <tr key={a.assetId}>
                        <td>{idx + 1}</td>
                        <td>{a.code}</td>
                        <td>{a.name}</td>
                        <td>{a.locationLabel}</td>
                        <td>{a.statusLabel}</td>
                        <td>1</td>
                        <td>{a.currentDepartmentName}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="transfer-asset-placeholder">
                Vui lòng chọn cá thể từ danh sách cá thể để hiển thị thông tin chi tiết.
              </p>
            )}
            {assetsError && <div className="transfer-error-text">{assetsError}</div>}
          </div>

        </div>

        <div className="transfer-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="transfer-btn-submit"
          >
            Gửi yêu cầu
          </button>
          <button
            type="button"
            onClick={handleSaveDraft}
            className="transfer-btn-draft"
          >
            Nháp
          </button>
          <button
            type="button"
            onClick={onClose}
            className="transfer-btn-cancel"
          >
            Hủy
          </button>
        </div>
      </div>

      <SelectAssetsModal
        open={isSelectAssetsOpen}
        onClose={() => setIsSelectAssetsOpen(false)}
        initialSelectedIds={selectedAssets.map((a) => a.assetId)}
        enforceSameDepartment
        forTransferSelection
        restrictToDepartmentId={restrictPickerToDepartmentId}
        onConfirm={(assets) => {
          setSelectedAssets(assets);
          setAssetsError(null);
          if (fromSelectLocked && fromDepartmentId != null) {
            setFromLocationId(String(fromDepartmentId));
            setFromError(null);
            return;
          }
          const deptIds = Array.from(new Set(assets.map((a) => a.currentDepartmentId).filter((x) => x != null)));
          if (deptIds.length === 1) {
            setFromLocationId(String(deptIds[0]));
            setFromError(null);
          }
        }}
      />
    </div>
  );
}

