import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, DatePicker, Input, Select, message } from 'antd';
import { SearchOutlined, EyeOutlined, CheckOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { disposalRequestService } from '../../assets/services/disposalRequestService';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import { accountantRequestService } from '../services/accountantRequestService';
import { LiquidationDisposalApproveModal } from '../../liquidation/components/LiquidationDisposalApproveModal';
import { LiquidationDisposalDetailModal } from '../../liquidation/components/LiquidationDisposalDetailModal';
import { LiquidationAppraisalModal } from '../../liquidation/components/LiquidationAppraisalModal';
import { LiquidationExecutionModal } from '../../liquidation/components/LiquidationExecutionModal';
import '../pages/RequestsPage.css';

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

interface LiquidationRequestsTabProps {
  userId: number | undefined;
  isAccountantRole: boolean;
}

export function LiquidationRequestsTab({ userId, isAccountantRole }: LiquidationRequestsTabProps) {
  const [liquidationRows, setLiquidationRows] = useState<TransferRequestListItem[]>([]);
  const [liquidationLoading, setLiquidationLoading] = useState(false);

  const [reqSearch, setReqSearch] = useState('');
  const [reqStatusFilter, setReqStatusFilter] = useState<number | 'all'>('all');
  const [reqSentDate, setReqSentDate] = useState<Dayjs | null>(null);

  const [isLiquidationDetailOpen, setIsLiquidationDetailOpen] = useState(false);
  const [liquidationDetailRow, setLiquidationDetailRow] = useState<TransferRequestListItem | null>(null);
  const [isLiquidationApproveOpen, setIsLiquidationApproveOpen] = useState(false);
  const [selectedLiquidationItem, setSelectedLiquidationItem] = useState<TransferRequestListItem | null>(null);
  const [liquidationDecision, setLiquidationDecision] = useState<'approved' | 'rejected'>('approved');
  const [liquidationComment, setLiquidationComment] = useState('');
  const [liquidationSubmitting, setLiquidationSubmitting] = useState(false);
  const [liquidationModalType, setLiquidationModalType] = useState<'appraisal' | 'execution' | null>(null);
  const [liquidationModalRequestId, setLiquidationModalRequestId] = useState<number | null>(null);
  const [liquidationModalCode, setLiquidationModalCode] = useState('');
  const [liquidationModalAssetName, setLiquidationModalAssetName] = useState('');

  const reloadLiquidationRows = useCallback(async () => {
    if (!isAccountantRole) {
      setLiquidationRows([]);
      return;
    }
    setLiquidationLoading(true);
    try {
      const list = await disposalRequestService.getList();
      setLiquidationRows(list);
    } catch {
      message.error('Không tải được danh sách yêu cầu thanh lý.');
      setLiquidationRows([]);
    } finally {
      setLiquidationLoading(false);
    }
  }, [isAccountantRole]);

  useEffect(() => {
    void reloadLiquidationRows();
  }, [reloadLiquidationRows]);

  const filteredLiquidationRows = useMemo(() => {
    const keyword = reqSearch.trim().toLowerCase();
    return liquidationRows.filter((row) => {
      const matchStatus = reqStatusFilter === 'all' || row.status === reqStatusFilter;
      let matchDate = true;
      if (reqSentDate) {
        try {
          const rowDate = new Date(row.transferDate).toISOString().slice(0, 10);
          const filterDate = reqSentDate.format('YYYY-MM-DD');
          matchDate = rowDate === filterDate;
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
  }, [liquidationRows, reqSearch, reqStatusFilter, reqSentDate]);

  const statusPillClass = (color: string) => {
    if (color === 'success') return 'asset-status-pill asset-status-pill--active';
    if (color === 'default') return 'asset-status-pill asset-status-pill--inactive';
    if (color === 'processing') return 'asset-status-pill asset-status-pill--processing';
    if (color === 'warning') return 'asset-status-pill asset-status-pill--warning';
    if (color === 'error') return 'asset-status-pill asset-status-pill--danger';
    return 'asset-status-pill';
  };

  if (!isAccountantRole) {
    return (
      <div style={{ padding: 24, textAlign: 'center' }}>
        <p>Bạn không có quyền xem tab này.</p>
      </div>
    );
  }

  return (
    <>
      <div className="requests-filters">
        <Input
          placeholder="Tìm kiếm"
          prefix={<SearchOutlined />}
          className="requests-search"
          value={reqSearch}
          onChange={(e) => setReqSearch(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          className="requests-select"
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
          className="requests-select requests-date-picker"
          value={reqSentDate}
          onChange={(date) => setReqSentDate(date)}
        />
      </div>

      <div className="asset-table-wrapper requests-table-wrapper">
        <table className="asset-table requests-table">
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
            {liquidationLoading ? (
              <tr>
                <td colSpan={9} className="requests-table-empty">
                  Đang tải...
                </td>
              </tr>
            ) : filteredLiquidationRows.length === 0 ? (
              <tr>
                <td colSpan={9} className="requests-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              filteredLiquidationRows.map((row) => {
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
                      <div style={{ display: 'flex', gap: 4 }}>
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
                        {row.status === 0 && (
                          <Button
                            type="text"
                            icon={<CheckOutlined />}
                            size="small"
                            onClick={() => {
                              setSelectedLiquidationItem(row);
                              setLiquidationDecision('approved');
                              setLiquidationComment('');
                              setIsLiquidationApproveOpen(true);
                            }}
                          >
                            Phê duyệt
                          </Button>
                        )}
                        {row.status === 2 && (
                          <Button
                            type="text"
                            size="small"
                            onClick={() => {
                              setLiquidationModalType('appraisal');
                              setLiquidationModalRequestId(row.assetRequestId);
                              setLiquidationModalCode(row.code ?? `YC-${row.assetRequestId}`);
                              setLiquidationModalAssetName(row.assetName ?? '');
                            }}
                          >
                            Ghi nhận biên bản thẩm định
                          </Button>
                        )}
                        {row.status === 4 && (
                          <Button
                            type="text"
                            size="small"
                            onClick={() => {
                              setLiquidationModalType('execution');
                              setLiquidationModalRequestId(row.assetRequestId);
                              setLiquidationModalCode(row.code ?? `YC-${row.assetRequestId}`);
                              setLiquidationModalAssetName(row.assetName ?? '');
                            }}
                          >
                            Ghi nhận biên bản thanh lý
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <LiquidationDisposalDetailModal
        open={isLiquidationDetailOpen}
        onClose={() => {
          setIsLiquidationDetailOpen(false);
          setLiquidationDetailRow(null);
        }}
        row={liquidationDetailRow}
        showAccountantExtras={isAccountantRole}
      />

      {isLiquidationApproveOpen && selectedLiquidationItem && (
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
          submitting={liquidationSubmitting}
          onConfirm={async () => {
            if (!selectedLiquidationItem || !userId) return;
            setLiquidationSubmitting(true);
            try {
              const payload = {
                approvedBy: userId,
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
              await reloadLiquidationRows();
            } catch (e: unknown) {
              const err = e as { response?: { data?: string } };
              message.error(err?.response?.data ?? 'Thao tác duyệt thanh lý thất bại.');
            } finally {
              setLiquidationSubmitting(false);
            }
          }}
        />
      )}

      {liquidationModalType === 'appraisal' && (
        <LiquidationAppraisalModal
          open
          assetRequestId={liquidationModalRequestId}
          requestCode={liquidationModalCode}
          assetName={liquidationModalAssetName}
          userId={userId}
          onClose={() => {
            setLiquidationModalType(null);
            setLiquidationModalRequestId(null);
            setLiquidationModalCode('');
            setLiquidationModalAssetName('');
          }}
          onSuccess={async () => {
            await reloadLiquidationRows();
          }}
        />
      )}

      {liquidationModalType === 'execution' && (
        <LiquidationExecutionModal
          open
          assetRequestId={liquidationModalRequestId}
          requestCode={liquidationModalCode}
          userId={userId}
          onClose={() => {
            setLiquidationModalType(null);
            setLiquidationModalRequestId(null);
            setLiquidationModalCode('');
          }}
          onSuccess={async () => {
            await reloadLiquidationRows();
          }}
        />
      )}
    </>
  );
}
