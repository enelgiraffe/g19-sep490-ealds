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
  /** Mã tài sản (catalog). */
  assetCatalogCode: string;
  /** Mã cá thể. */
  code: string;
  /** Tên tài sản. */
  name: string;
  /** Loại tài sản. */
  assetTypeName: string;
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
    assetCatalogCode: (a.assetCode && a.assetCode.trim()) || '—',
    code: a.instanceCode,
    name: (a.assetName && a.assetName.trim()) || (a.assetCode && a.assetCode.trim()) || a.instanceCode,
    assetTypeName: (a.assetTypeName && a.assetTypeName.trim()) || '—',
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
  restrictToDepartmentId,
  forTransferSelection = false,
}: {
  open: boolean;
  onClose: () => void;
  initialSelectedIds?: number[];
  onConfirm: (assets: SelectableAsset[]) => void;
  enforceSameDepartment?: boolean;
  /** When set, only load instances currently assigned to this department (GET currentDepartmentId). */
  restrictToDepartmentId?: number | null;
  /** When true, list is not limited to the signed-in department head’s department (server flag). */
  forTransferSelection?: boolean;
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
      const deptFilter =
        restrictToDepartmentId != null &&
        Number.isFinite(restrictToDepartmentId) &&
        restrictToDepartmentId > 0
          ? restrictToDepartmentId
          : undefined;
      const params: GetAssetInstancesParams = {
        keyword: kw || undefined,
        ...(deptFilter != null ? { currentDepartmentId: deptFilter } : {}),
        ...(forTransferSelection
          ? { forTransferSelection: true, status: 1 }
          : {}),
      };
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
  }, [open, keyword, restrictToDepartmentId, forTransferSelection]);

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
          <h2 className="select-assets__title">Danh sách cá thể</h2>
        </div>

        <div className="select-assets__body">
          <div className="select-assets__toolbar">
            <div className="select-assets__search">
              <span className="select-assets__search-icon">🔍</span>
              <input
                type="text"
                placeholder="Nhập tên, mã cá thể để tìm kiếm"
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
              />
            </div>
          </div>

          <div className="select-assets__table-wrapper">
            <table className="select-assets__table">
              <thead>
                <tr>
                  <th className="select-assets__cell select-assets__cell--checkbox" />
                  <th>Mã tài sản</th>
                  <th>Mã cá thể</th>
                  <th>Tên tài sản</th>
                  <th>Loại tài sản</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={6} className="select-assets__empty">
                      Đang tải...
                    </td>
                  </tr>
                ) : rows.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="select-assets__empty">
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
                                message.warning('Chỉ đượn các cá thể cùng phòng ban/vị trí nguồn.');
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
                        <td>{r.assetCatalogCode}</td>
                        <td>{r.code}</td>
                        <td>{r.name}</td>
                        <td>{r.assetTypeName}</td>
                        <td>{r.statusLabel}</td>
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
            Đã chọn <strong>{selectedIds.size}</strong>/{rows.length} cá thể
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

