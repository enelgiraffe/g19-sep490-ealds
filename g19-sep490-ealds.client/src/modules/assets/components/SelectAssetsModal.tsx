import { useEffect, useMemo, useRef, useState } from 'react';
import { message } from 'antd';
import {
  assetInstanceService,
  getStatusLabel,
  type AssetInstanceResponse,
  type GetAssetInstancesParams,
} from '../services/assetService';
import './SelectAssetsModal.css';

/** `assetId` is the physical row id (`assetInstanceId`) for transfer/maintenance APIs. */
export type SelectableAsset = {
  assetId: number;
  code: string;
  name: string;
  locationLabel: string;
  statusLabel: string;
  quantity: number;
  currentDepartmentName: string;
  currentDepartmentId: number | null;
};

function toSelectableAsset(a: AssetInstanceResponse): SelectableAsset {
  const locationLabel =
    a.currentLocationId != null ? `AL-${a.currentLocationId}` : '—';
  return {
    assetId: a.assetInstanceId,
    code: a.instanceCode,
    name: a.assetName ?? a.assetCode ?? a.instanceCode,
    locationLabel,
    statusLabel: getStatusLabel(a.statusName),
    quantity: 1,
    currentDepartmentName: a.currentDepartmentName ?? '—',
    currentDepartmentId: a.currentDepartmentId ?? null,
  };
}

function normalizeKeyword(s: string): string {
  return s.trim();
}

export function SelectAssetsModal({
  open,
  onClose,
  initialSelectedIds,
  onConfirm,
  enforceSameDepartment = true,
}: {
  open: boolean;
  onClose: () => void;
  initialSelectedIds?: number[];
  onConfirm: (assets: SelectableAsset[]) => void;
  enforceSameDepartment?: boolean;
}) {
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState('');
  const [rows, setRows] = useState<SelectableAsset[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(() => new Set());
  const searchDebounceRef = useRef<number | null>(null);

  useEffect(() => {
    if (!open) return;
    setKeyword('');
    setRows([]);
    setSelectedIds(new Set(initialSelectedIds ?? []));
  }, [open, initialSelectedIds]);

  useEffect(() => {
    if (!open) return;
    const kw = normalizeKeyword(keyword);
    if (searchDebounceRef.current != null) {
      window.clearTimeout(searchDebounceRef.current);
    }
    searchDebounceRef.current = window.setTimeout(() => {
      setLoading(true);
      const params: GetAssetInstancesParams = { keyword: kw || undefined };
      assetInstanceService
        .getAll(params)
        .then((data) => setRows(data.map(toSelectableAsset)))
        .catch(() => {
          message.error('Không tải được danh sách tài sản.');
          setRows([]);
        })
        .finally(() => setLoading(false));
    }, 250);

    return () => {
      if (searchDebounceRef.current != null) {
        window.clearTimeout(searchDebounceRef.current);
        searchDebounceRef.current = null;
      }
    };
  }, [open, keyword]);

  const selectedAssets = useMemo(() => {
    const byId = new Map(rows.map((r) => [r.assetId, r]));
    return Array.from(selectedIds)
      .map((id) => byId.get(id))
      .filter((x): x is SelectableAsset => !!x);
  }, [rows, selectedIds]);

  const lockedDepartmentId = useMemo(() => {
    if (!enforceSameDepartment) return null;
    const first = selectedAssets.find((a) => a.currentDepartmentId != null);
    return first?.currentDepartmentId ?? null;
  }, [enforceSameDepartment, selectedAssets]);

  if (!open) return null;

  return (
    <div className="select-assets-overlay" role="dialog" aria-modal="true">
      <div className="select-assets-modal">
        <button
          type="button"
          className="select-assets__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="select-assets__close">×</span>
        </button>

        <div className="select-assets__header">
          <h2 className="select-assets__title">Danh sách tài sản</h2>
        </div>

        <div className="select-assets__body">
          <div className="select-assets__toolbar">
            <div className="select-assets__search">
              <span className="select-assets__search-icon">🔍</span>
              <input
                type="text"
                placeholder="Nhập tên, mã tài sản để tìm kiếm"
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
              />
            </div>
          </div>

          {enforceSameDepartment && lockedDepartmentId != null && (
            <div className="select-assets__hint">
              Đang chọn theo <strong>1 phòng ban nguồn</strong>. Các tài sản thuộc phòng ban khác sẽ không thể chọn.
            </div>
          )}

          <div className="select-assets__table-wrapper">
            <table className="select-assets__table">
              <thead>
                <tr>
                  <th className="select-assets__cell select-assets__cell--checkbox" />
                  <th>Mã tài sản</th>
                  <th>Tài sản</th>
                  <th>Vị trí tài sản</th>
                  <th>Tình trạng</th>
                  <th>Số lượng</th>
                  <th>Phòng ban sử dụng</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={7} className="select-assets__empty">
                      Đang tải...
                    </td>
                  </tr>
                ) : rows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="select-assets__empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  rows.map((r) => {
                    const checked = selectedIds.has(r.assetId);
                    const disabled =
                      enforceSameDepartment &&
                      lockedDepartmentId != null &&
                      r.currentDepartmentId != null &&
                      r.currentDepartmentId !== lockedDepartmentId &&
                      !checked;
                    return (
                      <tr
                        key={r.assetId}
                        className={disabled ? 'select-assets__row select-assets__row--disabled' : 'select-assets__row'}
                      >
                        <td className="select-assets__cell select-assets__cell--checkbox">
                          <input
                            type="checkbox"
                            checked={checked}
                            disabled={disabled}
                            onChange={() => {
                              if (disabled) {
                                message.warning('Chỉ được chọn các tài sản cùng phòng ban/vị trí nguồn.');
                                return;
                              }
                              setSelectedIds((prev) => {
                                const next = new Set(prev);
                                if (next.has(r.assetId)) next.delete(r.assetId);
                                else next.add(r.assetId);
                                return next;
                              });
                            }}
                          />
                        </td>
                        <td>{r.code}</td>
                        <td>{r.name}</td>
                        <td>{r.locationLabel}</td>
                        <td>{r.statusLabel}</td>
                        <td className="select-assets__align-right">{r.quantity}</td>
                        <td>{r.currentDepartmentName}</td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          <div className="select-assets__meta">Tổng số bản ghi {rows.length}</div>
        </div>

        <div className="select-assets__footer">
          <div className="select-assets__selected">
            Đã chọn <strong>{selectedIds.size}</strong>/{rows.length} tài sản
          </div>
          <div className="select-assets__actions">
            <button type="button" className="select-assets__btn select-assets__btn--ghost" onClick={onClose}>
              Hủy
            </button>
            <button
              type="button"
              className="select-assets__btn select-assets__btn--primary"
              disabled={selectedIds.size === 0}
              onClick={() => {
                onConfirm(selectedAssets);
                onClose();
              }}
            >
              Chọn
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

