import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Input, Select, Button, Spin, message, Modal } from 'antd';
import {
  SearchOutlined,
  ArrowLeftOutlined,
  CheckCircleOutlined,
  PrinterOutlined,
} from '@ant-design/icons';
import {
  inventoryService,
  getCurrentUserId,
  type SessionDetail,
  type SessionAssetCheckItem,
  type AssetInventoryDetail,
  type CompleteSessionResult,
} from '../services/inventoryService';
import './PeriodicInventoryExecutionPage.css';

const CHECK_STATUS_TABS: { label: string; value: number | undefined }[] = [
  { label: 'Tất cả', value: undefined },
  { label: 'Chưa kiểm kê', value: 0 },
  { label: 'Đang kiểm kê', value: 1 },
  { label: 'Hoàn tất', value: 2 },
];

function getSessionBadgeClass(status: number): string {
  if (status === 1) return 'exec-badge--in-progress';
  if (status === 2 || status === 4) return 'exec-badge--completed';
  if (status === 3) return 'exec-badge--cancelled';
  return 'exec-badge--default';
}

function formatStillInUse(v: boolean | null | undefined): string {
  if (v === true) return 'Có';
  if (v === false) return 'Không';
  return '—';
}

function formatInUseMatch(useMatch: boolean | null): string {
  if (useMatch === null) return '—';
  return useMatch ? '✓' : '✗';
}

export function PeriodicInventoryExecutionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const sessionIdNum = Number(sessionId);

  const [sessionDetail, setSessionDetail] = useState<SessionDetail | null>(null);
  const [assets, setAssets] = useState<SessionAssetCheckItem[]>([]);
  const [selectedAssetInstanceId, setSelectedAssetInstanceId] = useState<number | null>(null);
  const [assetDetail, setAssetDetail] = useState<AssetInventoryDetail | null>(null);

  const [stillInUse, setStillInUse] = useState(false);
  const [actualLocationId, setActualLocationId] = useState<number | null>(null);
  const [actualManagerId, setActualManagerId] = useState<number | null>(null);
  const [actualCondition, setActualCondition] = useState<string>('');

  const [searchText, setSearchText] = useState('');
  const [checkStatusFilter, setCheckStatusFilter] = useState<number | undefined>(undefined);

  const [loadingSession, setLoadingSession] = useState(false);
  const [loadingAssets, setLoadingAssets] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [saving, setSaving] = useState(false);
  const [completing, setCompleting] = useState(false);

  const [isCompleteModalOpen, setIsCompleteModalOpen] = useState(false);
  const [completeResult, setCompleteResult] = useState<CompleteSessionResult | null>(null);

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
      setStillInUse(detail.actualStillInUse ?? detail.bookStillInUse);
      setActualLocationId(detail.actualLocationId);
      setActualManagerId(detail.actualManagerId);
      setActualCondition(detail.actualCondition ?? '');
    } catch {
      message.error('Không thể tải chi tiết tài sản.');
    } finally {
      setLoadingDetail(false);
    }
  }, [sessionIdNum]);

  const handleReset = () => {
    if (!assetDetail) return;
    setStillInUse(assetDetail.actualStillInUse ?? assetDetail.bookStillInUse);
    setActualLocationId(assetDetail.actualLocationId);
    setActualManagerId(assetDetail.actualManagerId);
    setActualCondition(assetDetail.actualCondition ?? '');
  };

  const handleSave = async () => {
    if (!assetDetail) return;
    setSaving(true);
    try {
      await inventoryService.saveAssetInventory(sessionIdNum, {
        assetInstanceId: assetDetail.assetInstanceId,
        stillInUse,
        actualCondition: actualCondition.trim(),
        actualLocationId,
        actualManagerId,
        checkedBy: getCurrentUserId(),
      });
      message.success('Đã lưu thông tin kiểm kê.');
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
      setCompleteResult(result);
      setIsCompleteModalOpen(true);
      fetchSession();
      fetchAssets();
    } catch {
      message.error('Không thể hoàn thành kiểm kê. Vui lòng thử lại.');
    } finally {
      setCompleting(false);
    }
  };

  const getComparisonRows = () => {
    if (!assetDetail) return [];
    const selectedLocation = assetDetail.locations.find((l) => l.id === actualLocationId);
    const selectedManager = assetDetail.managers.find((m) => m.id === actualManagerId);
    const locActual = selectedLocation?.name ?? '-';
    const locBook = assetDetail.bookLocationName || '-';
    const mgrActual = selectedManager?.name ?? '-';
    const mgrBook = assetDetail.bookManagerName || '-';
    return [
      {
        field: 'Vị trí',
        book: locBook,
        actual: locActual,
        isMatch: locActual !== '-' && locActual === locBook,
        hasActual: locActual !== '-',
      },
      {
        field: 'Người quản lý',
        book: mgrBook,
        actual: mgrActual,
        isMatch: mgrActual !== '-' && mgrActual === mgrBook,
        hasActual: mgrActual !== '-',
      },
      {
        field: 'Đang sử dụng',
        book: formatStillInUse(assetDetail.bookStillInUse),
        actual: formatStillInUse(stillInUse),
        isMatch: stillInUse === assetDetail.bookStillInUse,
        hasActual: true,
      },
    ];
  };

  const isEditable = sessionDetail?.status === 1;
  const comparisonRows = getComparisonRows();

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
    return (
      <>
        <div className="exec-detail-content">
          {/* Asset metadata */}
          <div className="exec-asset-meta">
            <div className="exec-asset-meta__row">
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Mã danh mục:</span>
                <span className="exec-asset-meta__value">{assetDetail.assetCode}</span>
              </div>
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Mã thể hiện:</span>
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

          {/* Per-instance: still in use + status clarification (ActualCondition) */}
          <div className="exec-instance-check">
            <div className="exec-instance-check__head">
              <span className="exec-instance-check__title">Xác nhận thể hiện tài sản</span>
              <span className="exec-instance-check__hint">
                Sổ sách: {formatStillInUse(assetDetail.bookStillInUse)}
              </span>
            </div>
            <div className="exec-instance-use-row">
              <label className="exec-still-in-use-label">
                <input
                  type="checkbox"
                  className="exec-still-in-use-checkbox"
                  checked={stillInUse}
                  onChange={(e) => setStillInUse(e.target.checked)}
                  disabled={!isEditable}
                />
                <span>Còn đang sử dụng</span>
              </label>
              <div className="exec-condition-field">
                <label htmlFor="exec-actual-condition" className="exec-condition-field__label">
                  Tình trạng / ghi chú thực tế
                </label>
                {isEditable ? (
                  <input
                    id="exec-actual-condition"
                    type="text"
                    className="exec-condition-field__input"
                    value={actualCondition}
                    onChange={(e) => setActualCondition(e.target.value)}
                    placeholder="Làm rõ tình trạng thể hiện (lưu vào biên bản kiểm kê)"
                  />
                ) : (
                  <span className="exec-condition-field__readonly">
                    {actualCondition || '—'}
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* Location and Manager */}
          <div className="exec-loc-mgr">
            <div className="exec-loc-mgr__col">
              <label htmlFor="exec-location-select" className="exec-loc-mgr__label">
                Vị trí tài sản
              </label>
              <Select
                id="exec-location-select"
                className="exec-loc-mgr__select"
                value={actualLocationId}
                onChange={setActualLocationId}
                placeholder="Chọn vị trí"
                disabled={!isEditable}
                options={assetDetail.locations.map((l) => ({
                  value: l.id,
                  label: l.name,
                }))}
              />
              <span className="exec-loc-mgr__book">
                Số sách: {assetDetail.bookLocationName || '-'}
              </span>
            </div>
            <div className="exec-loc-mgr__col">
              <label htmlFor="exec-manager-select" className="exec-loc-mgr__label">
                Người quản lý
              </label>
              <Select
                id="exec-manager-select"
                className="exec-loc-mgr__select"
                value={actualManagerId}
                onChange={setActualManagerId}
                placeholder="Chọn người quản lý"
                disabled={!isEditable}
                options={assetDetail.managers.map((m) => ({
                  value: m.id,
                  label: m.name,
                }))}
              />
              <span className="exec-loc-mgr__book">
                Số sách: {assetDetail.bookManagerName || '-'}
              </span>
            </div>
          </div>

          {/* Comparison table */}
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
              {comparisonRows.map((row) => (
                <tr key={row.field}>
                  <td>{row.field}</td>
                  <td>{row.book}</td>
                  <td>{row.actual}</td>
                  <td>{renderComparisonStatus(row)}</td>
                </tr>
              ))}
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
          <Button
            className="exec-complete-btn"
            onClick={handleComplete}
            loading={completing}
          >
            Hoàn thành kiểm kê
          </Button>
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
                    <th>Mã danh mục</th>
                    <th>Mã thể hiện</th>
                    <th>Tên tài sản</th>
                    <th>Phòng ban</th>
                    <th>Sổ sách</th>
                    <th>Thực tế</th>
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
                      const useMatch =
                        asset.actualStillInUse == null
                          ? null
                          : asset.actualStillInUse === asset.bookStillInUse;
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
                        <td>{formatStillInUse(asset.bookStillInUse)}</td>
                        <td>{formatStillInUse(asset.actualStillInUse)}</td>
                        <td className={useMatch === false ? 'exec-diff-cell' : ''}>
                          {formatInUseMatch(useMatch)}
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

      {/* Completion summary modal */}
      <Modal
        open={isCompleteModalOpen}
        footer={null}
        closable
        onCancel={() => setIsCompleteModalOpen(false)}
        centered
        width={400}
      >
        <div className="exec-complete-modal">
          <div className="exec-complete-modal__icon">
            <CheckCircleOutlined />
          </div>
          <h2 className="exec-complete-modal__title">Hoàn thành kiểm kê</h2>
          <div className="exec-complete-modal__stats">
            <p>
              Chênh lệch số lượng:{' '}
              <strong>{completeResult?.quantityDiffCount ?? 0} tài sản</strong>
            </p>
            <p>
              Thay đổi vị trí tài sản:{' '}
              <strong>{completeResult?.locationChangeCount ?? 0} tài sản</strong>
            </p>
            <p>
              Thay đổi phòng ban quản lý:{' '}
              <strong>{completeResult?.departmentChangeCount ?? 0} tài sản</strong>
            </p>
            <p>
              Thay đổi tình trạng:{' '}
              <strong>{completeResult?.conditionChangeCount ?? 0} tài sản</strong>
            </p>
          </div>
          <p className="exec-complete-modal__notify">
            Hệ thống đã gửi thông báo tới Kế toán (xử lý chênh lệch) và Giám đốc (xem báo cáo).
          </p>
          <div className="exec-complete-modal__footer">
            <Button onClick={() => setIsCompleteModalOpen(false)}>Đóng</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
