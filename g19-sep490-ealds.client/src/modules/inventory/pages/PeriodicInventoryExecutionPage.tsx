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

export function PeriodicInventoryExecutionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const sessionIdNum = Number(sessionId);

  const [sessionDetail, setSessionDetail] = useState<SessionDetail | null>(null);
  const [assets, setAssets] = useState<SessionAssetCheckItem[]>([]);
  const [selectedAssetId, setSelectedAssetId] = useState<number | null>(null);
  const [assetDetail, setAssetDetail] = useState<AssetInventoryDetail | null>(null);

  const [actualQties, setActualQties] = useState<Record<string, number | null>>({});
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

  const handleSelectAsset = useCallback(async (assetId: number) => {
    setSelectedAssetId(assetId);
    setAssetDetail(null);
    setLoadingDetail(true);
    try {
      const detail = await inventoryService.getAssetInventoryDetail(sessionIdNum, assetId);
      setAssetDetail(detail);
      const qties: Record<string, number | null> = {};
      detail.statusEntries.forEach((e) => { qties[e.statusKey] = e.actualQty; });
      setActualQties(qties);
      setActualLocationId(detail.actualLocationId);
      setActualManagerId(detail.actualManagerId);
      setActualCondition('');
    } catch {
      message.error('Không thể tải chi tiết tài sản.');
    } finally {
      setLoadingDetail(false);
    }
  }, [sessionIdNum]);

  const handleReset = () => {
    if (!assetDetail) return;
    const qties: Record<string, number | null> = {};
    assetDetail.statusEntries.forEach((e) => { qties[e.statusKey] = e.actualQty; });
    setActualQties(qties);
    setActualLocationId(assetDetail.actualLocationId);
    setActualManagerId(assetDetail.actualManagerId);
    setActualCondition('');
  };

  const handleSave = async () => {
    if (!assetDetail) return;
    setSaving(true);
    try {
      await inventoryService.saveAssetInventory(sessionIdNum, {
        assetId: assetDetail.assetId,
        statusEntries: assetDetail.statusEntries.map((e) => ({
          statusKey: e.statusKey,
          actualQty: actualQties[e.statusKey] ?? 0,
        })),
        actualLocationId,
        actualManagerId,
        checkedBy: getCurrentUserId(),
        actualCondition: actualCondition.trim() || undefined,
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

  const getTotals = () => {
    if (!assetDetail) return { bookTotal: 0, actualTotal: 0, diffTotal: 0 };
    const bookTotal = assetDetail.statusEntries.reduce((sum, e) => sum + e.bookQty, 0);
    const actualTotal = assetDetail.statusEntries.reduce(
      (sum, e) => sum + (actualQties[e.statusKey] ?? 0),
      0,
    );
    return { bookTotal, actualTotal, diffTotal: actualTotal - bookTotal };
  };

  const getComparisonRows = () => {
    if (!assetDetail) return [];
    const inUseEntry = assetDetail.statusEntries.find((e) => e.statusKey === 'in_use');
    const bookInUse = inUseEntry?.bookQty ?? 0;
    const actualInUse = actualQties['in_use'] ?? null;
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
        book: String(bookInUse),
        actual: actualInUse?.toString() ?? '-',
        isMatch: actualInUse != null && actualInUse === bookInUse,
        hasActual: actualInUse != null,
      },
    ];
  };

  const handleQtyChange = useCallback(
    (statusKey: string, rawValue: string) => {
      const val = rawValue === '' ? null : Number(rawValue);
      setActualQties((prev) => ({ ...prev, [statusKey]: val }));
    },
    [],
  );

  const isEditable = sessionDetail?.status === 1;
  const { bookTotal, actualTotal, diffTotal } = getTotals();
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
                <span className="exec-asset-meta__label">Mã tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.assetCode}</span>
              </div>
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Nhóm tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.categoryName}</span>
              </div>
            </div>
            <div className="exec-asset-meta__row">
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Tên tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.assetName}</span>
              </div>
              <div className="exec-asset-meta__item">
                <span className="exec-asset-meta__label">Loại tài sản:</span>
                <span className="exec-asset-meta__value">{assetDetail.typeName}</span>
              </div>
            </div>
          </div>

          {/* Status quantity table */}
          <table className="exec-status-table">
            <thead>
              <tr>
                <th>Tình trạng</th>
                <th>Số sách</th>
                <th>Thực tế</th>
                <th>Chênh lệch</th>
              </tr>
            </thead>
            <tbody>
              {assetDetail.statusEntries.map((entry, idx) => {
                const actual = actualQties[entry.statusKey];
                const diff =
                  actual == null ? null : actual - entry.bookQty;
                const diffClass = diff != null && diff !== 0 ? 'exec-diff-cell' : '';
                return (
                  <tr
                    key={entry.statusKey}
                    className={idx % 2 === 0 ? 'exec-status-row--even' : ''}
                  >
                    <td>{entry.statusLabel}</td>
                    <td>{entry.bookQty}</td>
                    <td>
                      {isEditable ? (
                        <input
                          type="number"
                          min={0}
                          className="exec-qty-input"
                          value={actual ?? ''}
                          onChange={(e) =>
                            handleQtyChange(entry.statusKey, e.target.value)
                          }
                        />
                      ) : (
                        <span>{actual ?? '-'}</span>
                      )}
                    </td>
                    <td className={diffClass}>{diff ?? '-'}</td>
                  </tr>
                );
              })}
              <tr className="exec-status-row--total">
                <td><strong>Tổng</strong></td>
                <td><strong>{bookTotal}</strong></td>
                <td><strong>{actualTotal}</strong></td>
                <td className={diffTotal === 0 ? '' : 'exec-diff-cell'}>
                  <strong>{diffTotal}</strong>
                </td>
              </tr>
              <tr className="exec-status-row--note">
                <td colSpan={4} className="exec-note-cell">
                  <label htmlFor="exec-actual-condition" className="exec-note-label">
                    Ghi chú tình trạng
                  </label>
                  {isEditable ? (
                    <input
                      id="exec-actual-condition"
                      type="text"
                      className="exec-note-input"
                      value={actualCondition}
                      onChange={(e) => setActualCondition(e.target.value)}
                      placeholder="Nhập ghi chú tình trạng tài sản (tùy chọn)"
                    />
                  ) : (
                    <span className="exec-note-value">
                      {actualCondition || '-'}
                    </span>
                  )}
                </td>
              </tr>
            </tbody>
          </table>

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
                    <th>Mã tài sản</th>
                    <th>Tên tài sản</th>
                    <th>Phòng ban</th>
                    <th>Số sách</th>
                    <th>Thực tế</th>
                    <th>Chênh lệch</th>
                  </tr>
                </thead>
                <tbody>
                  {assets.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="exec-asset-table__empty">
                        Không có tài sản
                      </td>
                    </tr>
                  ) : (
                    assets.map((asset) => (
                      <tr
                        key={asset.assetId}
                        className={`exec-asset-row${selectedAssetId === asset.assetId ? ' exec-asset-row--selected' : ''}`}
                        onClick={() => handleSelectAsset(asset.assetId)}
                      >
                        <td>{asset.assetCode}</td>
                        <td>{asset.assetName}</td>
                        <td>{asset.departmentName}</td>
                        <td>{asset.bookQty}</td>
                        <td>{asset.actualQty ?? '-'}</td>
                        <td className={asset.difference !== null && asset.difference !== 0 ? 'exec-diff-cell' : ''}>
                          {asset.difference ?? '-'}
                        </td>
                      </tr>
                    ))
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
