import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, DatePicker, Input, Select, message } from 'antd';
import { SearchOutlined, EyeOutlined, CheckOutlined } from '@ant-design/icons';
import { disposalRequestService } from '../../assets/services/disposalRequestService';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { accountantRequestService } from '../../requests/services/accountantRequestService';
import { LiquidationDisposalApproveModal } from '../components/LiquidationDisposalApproveModal';
import { LiquidationDisposalDetailModal } from '../components/LiquidationDisposalDetailModal';
import {
  filterDisposalListForDepartmentHead,
  isDepartmentHeadRoleCode,
} from '../../../shared/utils/departmentHeadRole';
import './LiquidationPage.css';
import '../../maintenance/pages/MaintenancePage.css';

const { Option } = Select;

const LIQUIDATION_STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ duyệt giám đốc', color: 'warning' },
  2: { label: 'Đã duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Đã thẩm định', color: 'processing' },
  5: { label: 'Đã thanh lý', color: 'success' },
};

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

export function LiquidationPage() {
  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);

  const [disposalRows, setDisposalRows] = useState<TransferRequestListItem[]>([]);
  const [disposalLoading, setDisposalLoading] = useState(false);

  const [reqSearch, setReqSearch] = useState('');
  const [reqStatusFilter, setReqStatusFilter] = useState<number | 'all'>('all');
  const [reqSentDate, setReqSentDate] = useState<string | null>(null);

  const [isLiquidationDetailOpen, setIsLiquidationDetailOpen] = useState(false);
  const [liquidationDetailRow, setLiquidationDetailRow] = useState<TransferRequestListItem | null>(null);
  const [isLiquidationApproveOpen, setIsLiquidationApproveOpen] = useState(false);
  const [selectedLiquidationItem, setSelectedLiquidationItem] = useState<TransferRequestListItem | null>(null);
  const [liquidationDecision, setLiquidationDecision] = useState<'approved' | 'rejected'>('approved');
  const [liquidationComment, setLiquidationComment] = useState('');
  const [liquidationDisposalMethod, setLiquidationDisposalMethod] = useState('');
  const [liquidationSubmitting, setLiquidationSubmitting] = useState(false);

  const normalizedRole = String(userProfile?.role ?? '').toUpperCase();
  const isAccountantRole = normalizedRole === 'ACCOUNTANT';
  const isDeptManager = isDepartmentHeadRoleCode(normalizedRole);

  const reloadDisposalRows = useCallback(async () => {
    if (!userProfile) {
      setDisposalRows([]);
      return;
    }
    setDisposalLoading(true);
    try {
      const list = await disposalRequestService.getList();
      const r = String(userProfile.role).toUpperCase();
      const isAcct = r === 'ACCOUNTANT';
      const isDept = isDepartmentHeadRoleCode(r);
      const filtered = isAcct
        ? list
        : isDept
          ? filterDisposalListForDepartmentHead(list, userProfile.id, userProfile.departmentId)
          : [];
      setDisposalRows(filtered);
    } catch {
      message.error('Không tải được danh sách yêu cầu thanh lý.');
      setDisposalRows([]);
    } finally {
      setDisposalLoading(false);
    }
  }, [userProfile]);

  useEffect(() => {
    (async () => {
      try {
        const p = await profileService.getProfile();
        setUserProfile(p);
      } catch {
        setUserProfile(null);
      }
    })();
  }, []);

  useEffect(() => {
    void reloadDisposalRows();
  }, [reloadDisposalRows]);

  const filteredDisposalRows = useMemo(() => {
    const keyword = reqSearch.trim().toLowerCase();
    return disposalRows.filter((row) => {
      const matchStatus = reqStatusFilter === 'all' || row.status === reqStatusFilter;
      let matchDate = true;
      if (reqSentDate) {
        try {
          const rowDate = new Date(row.transferDate).toISOString().slice(0, 10);
          matchDate = rowDate === reqSentDate;
        } catch {
          matchDate = true;
        }
      }
      const ic = (row.instanceCode ?? '').toLowerCase();
      const matchKeyword =
        !keyword ||
        (row.reason ?? '').toLowerCase().includes(keyword) ||
        row.code.toLowerCase().includes(keyword) ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword) ||
        ic.includes(keyword) ||
        `yc-${row.assetRequestId}`.includes(keyword);
      return matchStatus && matchDate && matchKeyword;
    });
  }, [disposalRows, reqSearch, reqStatusFilter, reqSentDate]);

  const [liqPage, setLiqPage] = useState(1);
  const [liqPageSize, setLiqPageSize] = useState(25);

  useEffect(() => {
    setLiqPage(1);
  }, [reqSearch, reqStatusFilter, reqSentDate]);

  const liqTotal = filteredDisposalRows.length;
  const liqTotalPages = Math.max(1, Math.ceil(liqTotal / liqPageSize));
  const safeLiqPage = Math.min(liqPage, liqTotalPages);

  useEffect(() => {
    setLiqPage((p) => Math.min(p, liqTotalPages));
  }, [liqTotalPages]);

  const liqRangeStart = liqTotal === 0 ? 0 : (safeLiqPage - 1) * liqPageSize + 1;
  const liqRangeEnd = Math.min(safeLiqPage * liqPageSize, liqTotal);

  const pagedDisposalRows = useMemo(
    () =>
      filteredDisposalRows.slice((safeLiqPage - 1) * liqPageSize, safeLiqPage * liqPageSize),
    [filteredDisposalRows, safeLiqPage, liqPageSize],
  );

  const showRequestTable = isAccountantRole || isDeptManager;

  const statusPillClass = (color: string) => {
    if (color === 'success') return 'asset-status-pill asset-status-pill--active';
    if (color === 'default') return 'asset-status-pill asset-status-pill--inactive';
    if (color === 'processing') return 'asset-status-pill asset-status-pill--processing';
    if (color === 'warning') return 'asset-status-pill asset-status-pill--warning';
    if (color === 'error') return 'asset-status-pill asset-status-pill--danger';
    return 'asset-status-pill';
  };

  return (
    <div className="liquidation-page">
      <h1 className="liquidation-page__title">Thanh lý</h1>

      {showRequestTable && (
        <div className="liquidation-card" style={{ marginBottom: 8 }}>
          <div className="liquidation-card__header">
            <div className="liquidation-filters liquidation-filters--single-row">
              <Input
                placeholder="Tìm kiếm"
                prefix={<SearchOutlined />}
                className="liquidation-search"
                value={reqSearch}
                onChange={(e) => setReqSearch(e.target.value)}
              />
              <Select
                placeholder="Trạng thái"
                className="liquidation-select"
                value={reqStatusFilter}
                onChange={(v) => setReqStatusFilter(v)}
              >
                <Option value="all">Tất cả</Option>
                {Object.entries(LIQUIDATION_STATUS_MAP).map(([k, v]) => (
                  <Option key={k} value={Number(k)}>
                    {v.label}
                  </Option>
                ))}
              </Select>
              <DatePicker
                placeholder="Ngày gửi"
                className="liquidation-select liquidation-date-picker"
                onChange={(_, dateString) => setReqSentDate(dateString || null)}
              />
            </div>
          </div>

          <div className="asset-table-wrapper maintenance-table-wrapper liquidation-table-wrapper">
            <table className="asset-table liquidation-table">
              <thead>
                <tr>
                  <th>MÃ YÊU CẦU</th>
                  <th>NGÀY GỬI</th>
                  <th>MÃ TÀI SẢN</th>
                  <th>MÃ CÁ THỂ</th>
                  <th>TÊN TÀI SẢN</th>
                  <th>PHÒNG BAN</th>
                  <th>NỘI DUNG</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {disposalLoading ? (
                  <tr>
                    <td colSpan={9} className="liquidation-table-empty">
                      Đang tải...
                    </td>
                  </tr>
                ) : pagedDisposalRows.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="liquidation-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  pagedDisposalRows.map((row) => {
                    const config = LIQUIDATION_STATUS_MAP[row.status] ?? LIQUIDATION_STATUS_MAP[0];
                    return (
                      <tr key={row.assetRequestId} className="asset-row">
                        <td>YC-{row.assetRequestId}</td>
                        <td>{formatDate(row.transferDate)}</td>
                        <td>{row.assetCode ?? '—'}</td>
                        <td>{row.instanceCode?.trim() || '—'}</td>
                        <td>{row.assetName ?? '—'}</td>
                        <td>{row.fromDepartment ?? '—'}</td>
                        <td>{row.reason?.trim() || '—'}</td>
                        <td>
                          <span className={statusPillClass(config.color)}>{config.label}</span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            size="small"
                            onClick={() => {
                              setLiquidationDetailRow(row);
                              setIsLiquidationDetailOpen(true);
                            }}
                          >
                            Xem
                          </Button>
                          {isAccountantRole && row.status === 0 && (
                            <Button
                              type="text"
                              icon={<CheckOutlined />}
                              size="small"
                              onClick={() => {
                                setSelectedLiquidationItem(row);
                                setLiquidationDecision('approved');
                                setLiquidationComment('');
                                setLiquidationDisposalMethod('');
                                setIsLiquidationApproveOpen(true);
                              }}
                            >
                              Phê duyệt
                            </Button>
                          )}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          <div className="maintenance-card__footer">
            <div className="maintenance-footer__left">
              Số lượng trên trang:
              <select
                className="maintenance-footer__select"
                value={liqPageSize}
                onChange={(e) => {
                  setLiqPageSize(Number(e.target.value));
                  setLiqPage(1);
                }}
              >
                <option value={10}>10</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
            <div className="maintenance-footer__center">
              {liqTotal === 0 ? '0-0 trên 0' : `${liqRangeStart}-${liqRangeEnd} trên ${liqTotal}`}
            </div>
            <div className="maintenance-footer__right">
              <button
                className="maintenance-footer__pager"
                type="button"
                disabled={safeLiqPage <= 1}
                onClick={() => setLiqPage((p) => Math.max(1, p - 1))}
              >
                ⟨
              </button>
              <button
                className="maintenance-footer__pager maintenance-footer__pager--active"
                type="button"
              >
                {safeLiqPage}
              </button>
              <button
                className="maintenance-footer__pager"
                type="button"
                disabled={safeLiqPage >= liqTotalPages}
                onClick={() => setLiqPage((p) => Math.min(liqTotalPages, p + 1))}
              >
                ⟩
              </button>
            </div>
          </div>
        </div>
      )}

      <LiquidationDisposalDetailModal
        open={isLiquidationDetailOpen}
        onClose={() => {
          setIsLiquidationDetailOpen(false);
          setLiquidationDetailRow(null);
        }}
        row={liquidationDetailRow}
        showAccountantExtras={isAccountantRole}
      />

      {isLiquidationApproveOpen && isAccountantRole && selectedLiquidationItem && (
        <LiquidationDisposalApproveModal
          open
          onClose={() => {
            setIsLiquidationApproveOpen(false);
            setSelectedLiquidationItem(null);
          }}
          row={selectedLiquidationItem}
          decision={liquidationDecision}
          onDecisionChange={setLiquidationDecision}
          comment={liquidationComment}
          onCommentChange={setLiquidationComment}
          disposalMethod={liquidationDisposalMethod}
          onDisposalMethodChange={setLiquidationDisposalMethod}
          submitting={liquidationSubmitting}
          onConfirm={async () => {
            if (!selectedLiquidationItem || !userProfile?.id) return;
            
            if (liquidationDecision === 'approved' && !liquidationDisposalMethod.trim()) {
              message.error('Vui lòng nhập phương án thanh lý.');
              return;
            }
            
            setLiquidationSubmitting(true);
            try {
              const payload = {
                approvedBy: userProfile.id,
                comment: liquidationComment.trim() || null,
              };
              if (liquidationDecision === 'approved') {
                await accountantRequestService.approve(selectedLiquidationItem.assetRequestId, payload);
                message.success('Đã phê duyệt yêu cầu thanh lý.');
              } else {
                await accountantRequestService.reject(selectedLiquidationItem.assetRequestId, payload);
                message.success('Đã từ chối yêu cầu thanh lý.');
              }
              setIsLiquidationApproveOpen(false);
              setSelectedLiquidationItem(null);
              await reloadDisposalRows();
            } catch (e: unknown) {
              const err = e as { response?: { data?: string } };
              message.error(err?.response?.data ?? 'Thao tác duyệt thanh lý thất bại.');
            } finally {
              setLiquidationSubmitting(false);
            }
          }}
        />
      )}

    </div>
  );
}
