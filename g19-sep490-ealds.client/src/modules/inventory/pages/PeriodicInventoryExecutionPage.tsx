import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Input, Select, Button, Spin, message, Modal, Tooltip } from 'antd';
import {
  SearchOutlined,
  ArrowLeftOutlined,
  PrinterOutlined,
} from '@ant-design/icons';
import {
  inventoryService,
  getCurrentUserId,
  SESSION_STATUS,
  type SessionDetail,
  type SessionAssetCheckItem,
  type AssetInventoryDetail,
} from '../services/inventoryService';
import {
  formatAssetStatusVi,
  getInventoryExecutionStatusSelectOptions,
} from '../../assets/services/assetService';
import './PeriodicInventoryExecutionPage.css';

const INVENTORY_STATUS_SELECT_OPTIONS = getInventoryExecutionStatusSelectOptions();

const CHECK_STATUS_TABS: { label: string; value: number | undefined }[] = [
  { label: 'Tất cả', value: undefined },
  { label: 'Chưa kiểm kê', value: 0 },
  { label: 'Đang kiểm kê', value: 1 },
  { label: 'Hoàn tất', value: 2 },
];

function getSessionBadgeClass(status: number): string {
  if (status === 1) return 'exec-badge--in-progress';
  if (status === 2 || status === 4 || status === 6) return 'exec-badge--completed';
  if (status === 3) return 'exec-badge--cancelled';
  return 'exec-badge--default';
}

function formatStatusMatch(match: boolean | null): string {
  if (match === null) return '—';
  return match ? '✓' : '✗';
}

/** Thực tế chưa lưu thì mặc định theo sổ sách (vị trí / trạng thái). */
function defaultActualsFromDetail(detail: AssetInventoryDetail): {
  status: number;
  locationId: number | null;
} {
  return {
    status: detail.actualStatus ?? detail.bookStatus,
    locationId: detail.actualLocationId ?? detail.bookLocationId ?? null,
  };
}

export function PeriodicInventoryExecutionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const sessionIdNum = Number(sessionId);

  const [sessionDetail, setSessionDetail] = useState<SessionDetail | null>(null);
  const [assets, setAssets] = useState<SessionAssetCheckItem[]>([]);
  const [selectedAssetInstanceId, setSelectedAssetInstanceId] = useState<number | null>(null);
  const [assetDetail, setAssetDetail] = useState<AssetInventoryDetail | null>(null);

  const [actualStatus, setActualStatus] = useState<number>(0);
  const [actualLocationId, setActualLocationId] = useState<number | null>(null);

  const [searchText, setSearchText] = useState('');
  const [checkStatusFilter, setCheckStatusFilter] = useState<number | undefined>(undefined);

  const [loadingSession, setLoadingSession] = useState(false);
  const [loadingAssets, setLoadingAssets] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [saving, setSaving] = useState(false);
  const [completing, setCompleting] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelNote, setCancelNote] = useState('');
  const [cancellingSession, setCancellingSession] = useState(false);

  const fetchSession = useCallback(async () => {
    if (!sessionIdNum) return;
    setLoadingSession(true);
    try {
      const data = await inventoryService.getSessionDetail(sessionIdNum);
      setSessionDetail(data);
    } catch {
      message.error('Không thể tải thông tin phiên kiểm kê.');
    } finally {
      setLoadingSession(false);
    }
  }, [sessionIdNum]);

  const fetchAssets = useCallback(async () => {
    if (!sessionIdNum) return;
    setLoadingAssets(true);
    try {
      const data = await inventoryService.getSessionAssets(sessionIdNum, {
        keyword: searchText || undefined,
        checkStatus: checkStatusFilter,
      });
      setAssets(data);
    } catch {
      message.error('Không thể tải danh sách tài sản.');
    } finally {
      setLoadingAssets(false);
    }
  }, [sessionIdNum, searchText, checkStatusFilter]);

  useEffect(() => { fetchSession(); }, [fetchSession]);
  useEffect(() => { fetchAssets(); }, [fetchAssets]);

  const handleSelectAsset = useCallback(async (assetInstanceId: number) => {
    setSelectedAssetInstanceId(assetInstanceId);
    setAssetDetail(null);
    setLoadingDetail(true);
    try {
      const detail = await inventoryService.getAssetInventoryDetail(sessionIdNum, assetInstanceId);
      setAssetDetail(detail);
      const defaults = defaultActualsFromDetail(detail);
      setActualStatus(defaults.status);
      setActualLocationId(defaults.locationId);
    } catch {
      message.error('Không thể tải chi tiết tài sản.');
    } finally {
      setLoadingDetail(false);
    }
  }, [sessionIdNum]);

  const handleReset = () => {
    if (!assetDetail) return;
    const defaults = defaultActualsFromDetail(assetDetail);
    setActualStatus(defaults.status);
    setActualLocationId(defaults.locationId);
  };

  const handleSave = async () => {
    if (!assetDetail) return;
    setSaving(true);
    try {
      await inventoryService.saveAssetInventory(sessionIdNum, {
        assetInstanceId: assetDetail.assetInstanceId,
        actualStatus,
        actualLocationId,
        checkedBy: getCurrentUserId(),
      });
      message.success('Đã lưu thông tin kiểm kê.');
      try {
        const refreshed = await inventoryService.getAssetInventoryDetail(
          sessionIdNum,
          assetDetail.assetInstanceId,
        );
        setAssetDetail(refreshed);
        const afterSave = defaultActualsFromDetail(refreshed);
        setActualStatus(afterSave.status);
        setActualLocationId(afterSave.locationId);
      } catch {
        /* saved OK; form giữ nguyên nếu tải lại chi tiết lỗi */
      }
      fetchAssets();
      fetchSession();
    } catch {
      message.error('Lưu thất bại. Vui lòng thử lại.');
    } finally {
      setSaving(false);
    }
  };

  const handleComplete = async () => {
    setCompleting(true);
    try {
      const result = await inventoryService.completeSession(sessionIdNum);
      const closedClean =
        result.hasDiscrepancies === false ||
        (result.newStatus !== undefined && result.newStatus === SESSION_STATUS.Confirmed);
      if (closedClean) {
        message.success(
          result.message ??
            'Đã hoàn thành kiểm kê. Không có chênh lệch — phiên đã xử lý.',
        );
        navigate('/inventory');
      } else {
        message.success('Đã hoàn thành kiểm kê. Chuyển tới trang xử lý chênh lệch.');
        navigate(`/inventory-review/${sessionIdNum}`);
      }
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(
        axiosErr?.response?.data?.message ?? 'Không thể hoàn thành kiểm kê. Vui lòng thử lại.',
      );
    } finally {
      setCompleting(false);
    }
  };

  const handleCancelSession = async () => {
    setCancellingSession(true);
    try {
      await inventoryService.cancelSession(sessionIdNum, {
        reviewedBy: getCurrentUserId(),
        reviewerRoleId: 4,
        reviewNotes: cancelNote || undefined,
      });
      message.success('Phiên kiểm kê đã được hủy.');
      setCancelModalOpen(false);
      setCancelNote('');
      fetchSession();
      fetchAssets();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(axiosErr?.response?.data?.message ?? 'Hủy phiên thất bại. Vui lòng thử lại.');
    } finally {
      setCancellingSession(false);
    }
  };

  const isEditable = sessionDetail?.status === 1;
  const progressPct = sessionDetail?.progressPercent ?? 0;
  const canCompleteInventory = progressPct >= 100;

  const renderComparisonStatus = (row: { hasActual: boolean; isMatch: boolean }) => {
    if (row.hasActual && row.isMatch) {
      return <span className="exec-match">✓ Match</span>;
    }
    if (row.hasActual) {
      return <span className="exec-mismatch">✗ Mismatch</span>;
    }
    return <span className="exec-pending">— Chưa nhập</span>;
  };

  const renderRightPanelContent = () => {
    if (loadingDetail) {
      return <div className="exec-detail-loading"><Spin /></div>;
    }
    if (assetDetail == null) {
      return (
        <div className="exec-detail-empty">
          <p>Chọn tài sản để xem chi tiết và nhập thông tin kiểm kê</p>
        </div>
      );
    }
    const selectedLocation = assetDetail.locations.find((l) => l.id === actualLocationId);
    const locActual = selectedLocation?.name ?? '-';
    const locBook = assetDetail.bookLocationName || '-';

    const locRow = {
      hasActual: locActual !== '-',
      isMatch: locActual !== '-' && locActual === locBook,
    };
    const statusRow = {
      hasActual: true,
      isMatch: actualStatus === assetDetail.bookStatus,
    };

    return (
      <>
        <div className="exec-detail-content">
          {/* Asset metadata */}
          <div className="exec-asset-meta">
            <div className="exec-asset-meta__row">
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Mã tài sảnsản:</span>
                <span className="exec-asset-meta__value">{assetDetail.assetCode}</span>
              </div>
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Mã cá thể:</span>
                <span className="exec-asset-meta__value">{assetDetail.instanceCode}</span>
              </div>
            </div>
            <div className="exec-asset-meta__row">
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Nhóm tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.categoryName}</span>
              </div>
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Loại tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.typeName}</span>
              </div>
            </div>
            <div className="exec-asset-meta__row">
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Tên tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.assetName}</span>
              </div>
            </div>
          </div>

          <table className="exec-comparison-table">
            <thead>
              <tr>
                <th>Trường</th>
                <th>Số sách</th>
                <th>Thực tế</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Vị trí</td>
                <td>{locBook}</td>
                <td className="exec-comparison-table__actual-cell">
                  {isEditable ? (
                    <Select
                      id="exec-location-select"
                      className="exec-comparison-table__select"
                      value={actualLocationId}
                      onChange={setActualLocationId}
                      placeholder="Chọn vị trí"
                      options={assetDetail.locations.map((l) => ({
                        value: l.id,
                        label: l.name,
                      }))}
                    />
                  ) : (
                    locActual
                  )}
                </td>
                <td>{renderComparisonStatus(locRow)}</td>
              </tr>
              <tr>
                <td>Trạng thái</td>
                <td>{formatAssetStatusVi(assetDetail.bookStatus)}</td>
                <td className="exec-comparison-table__actual-cell">
                  {isEditable ? (
                    <Select
                      id="exec-actual-status"
                      className="exec-comparison-table__select"
                      value={actualStatus}
                      onChange={(v) => setActualStatus(v ?? assetDetail.bookStatus)}
                      placeholder="Chọn trạng thái kiểm kê"
                      options={INVENTORY_STATUS_SELECT_OPTIONS}
                    />
                  ) : (
                    formatAssetStatusVi(actualStatus)
                  )}
                </td>
                <td>{renderComparisonStatus(statusRow)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* Action bar – always pinned at bottom */}
        <div className="exec-action-bar">
          {isEditable ? (
            <>
              <Button onClick={handleReset} disabled={saving}>
                Làm mới
              </Button>
              <Button type="primary" onClick={handleSave} loading={saving}>
                Lưu
              </Button>
            </>
          )
           : (
            <>
              {/* <Button icon={<WarningOutlined />}>
                Xử lý chênh lệch
              </Button> */}
              <Button type="primary" icon={<PrinterOutlined />}>
                In biên bản
              </Button>
            </>
          )
          }
        </div>
      </>
    );
  };

  if (loadingSession && !sessionDetail) {
    return (
      <div className="exec-page-loading">
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div className="exec-page">
      {/* ── Header ── */}
      <div className="exec-header">
        <div className="exec-header__left">
          <button type="button" className="exec-back-btn" onClick={() => navigate('/inventory')}>
            <ArrowLeftOutlined />
          </button>
          <h1 className="exec-title">Kiểm kê định kỳ</h1>
          {sessionDetail && (
            <span className={`exec-badge ${getSessionBadgeClass(sessionDetail.status)}`}>
              {sessionDetail.statusName}
            </span>
          )}
        </div>
        {isEditable && (
          <div className="exec-header__actions">
            <Button
              danger
              loading={cancellingSession}
              onClick={() => setCancelModalOpen(true)}
            >
              Hủy phiên kiểm kê
            </Button>
            <Tooltip
              title={
                canCompleteInventory
                  ? undefined
                  : `Cần kiểm kê đủ 100% tài sản (hiện ${progressPct}%).`
              }
            >
              <span className="exec-header__complete-wrap">
                <Button
                  type="primary"
                  className="exec-complete-btn"
                  disabled={!canCompleteInventory}
                  onClick={handleComplete}
                  loading={completing}
                >
                  Hoàn thành kiểm kê
                </Button>
              </span>
            </Tooltip>
          </div>
        )}
      </div>

      {/* ── Main content ── */}
      <div className="exec-content">
        {/* Asset list (above detail) */}
        <div className="exec-left-panel">
          <div className="exec-left-search">
            <Input
              placeholder="Tìm kiếm theo tên tài sản, mã tài sản"
              prefix={<SearchOutlined className="exec-search-icon" />}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              allowClear
            />
          </div>
          <div className="exec-filter-tabs">
            {CHECK_STATUS_TABS.map((tab) => (
              <button
                key={String(tab.value ?? 'all')}
                type="button"
                className={`exec-filter-tab${checkStatusFilter === tab.value ? ' exec-filter-tab--active' : ''}`}
                onClick={() => setCheckStatusFilter(tab.value)}
              >
                {tab.label}
              </button>
            ))}
          </div>
          <div className="exec-asset-list-wrapper">
            {loadingAssets ? (
              <div className="exec-list-loading"><Spin /></div>
            ) : (
              <table className="exec-asset-table">
                <thead>
                  <tr>
                    <th>Mã tài sản</th>
                    <th>Mã cá thể</th>
                    <th>Tên tài sản</th>
                    <th>Phòng ban</th>
                    <th>Trạng thái (sổ)</th>
                    <th>Trạng thái (kiểm kê)</th>
                    <th>Khớp</th>
                  </tr>
                </thead>
                <tbody>
                  {assets.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="exec-asset-table__empty">
                        Không có tài sản
                      </td>
                    </tr>
                  ) : (
                    assets.map((asset) => {
                      /** Khớp: ✓ only when đã lưu và không có discrepancy (mọi loại lệch đều tạo bản ghi trên server). */
                      const rowMatch =
                        asset.checkStatus !== 2
                          ? null
                          : asset.hasDiscrepancy !== undefined
                            ? !asset.hasDiscrepancy
                            : asset.actualStatus === asset.bookStatus;
                      return (
                      <tr
                        key={asset.assetInstanceId}
                        className={`exec-asset-row${selectedAssetInstanceId === asset.assetInstanceId ? ' exec-asset-row--selected' : ''}`}
                        onClick={() => handleSelectAsset(asset.assetInstanceId)}
                      >
                        <td>{asset.assetCode}</td>
                        <td>{asset.instanceCode}</td>
                        <td>{asset.assetName}</td>
                        <td>{asset.departmentName}</td>
                        <td>{formatAssetStatusVi(asset.bookStatus)}</td>
                        <td>
                          {asset.actualStatus == null
                            ? '—'
                            : formatAssetStatusVi(asset.actualStatus)}
                        </td>
                        <td className={rowMatch === false ? 'exec-diff-cell' : ''}>
                          {formatStatusMatch(rowMatch)}
                        </td>
                      </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            )}
          </div>
        </div>

        {/* Asset detail form */}
        <div className="exec-right-panel">
          {renderRightPanelContent()}
        </div>
      </div>

      <Modal
        open={cancelModalOpen}
        title="Hủy phiên kiểm kê"
        okText="Xác nhận hủy"
        cancelText="Đóng"
        okButtonProps={{ danger: true, loading: cancellingSession }}
        onOk={handleCancelSession}
        onCancel={() => {
          setCancelModalOpen(false);
          setCancelNote('');
        }}
        centered
        width={440}
      >
        <p style={{ marginBottom: 12 }}>
          Bạn có chắc muốn kết thúc phiên này?
        </p>
      </Modal>

    </div>
  );
}
