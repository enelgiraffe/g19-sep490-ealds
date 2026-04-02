import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, DatePicker, Input, Select, message } from 'antd';
import { SearchOutlined, EyeOutlined, CheckOutlined } from '@ant-design/icons';
import { disposalRequestService } from '../../assets/services/disposalRequestService';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { accountantRequestService } from '../../requests/services/accountantRequestService';
import {
  disposalAppraisalService,
  type DisposalAppraisalListItem,
} from '../../requests/services/disposalAppraisalService';
import { DisposalAppraisalDetailModal } from '../../requests/components/DisposalAppraisalDetailModal';
import { LiquidationDisposalApproveModal } from '../components/LiquidationDisposalApproveModal';
import { LiquidationDisposalDetailModal } from '../components/LiquidationDisposalDetailModal';
import { LiquidationExecutionModal } from '../components/LiquidationExecutionModal';
import {
  filterDisposalListForDepartmentHead,
  isDepartmentHeadRoleCode,
} from '../../../shared/utils/departmentHeadRole';
import './LiquidationPage.css';

const { Option } = Select;

type MainPill = 'requests' | 'appraisals';

const LIQUIDATION_STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ phê duyệt', color: 'warning' },
  2: { label: 'Phê duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Đang thực hiện', color: 'processing' },
  5: { label: 'Hoàn thành', color: 'success' },
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

  const [mainPill, setMainPill] = useState<MainPill>('requests');
  const [disposalRows, setDisposalRows] = useState<TransferRequestListItem[]>([]);
  const [disposalLoading, setDisposalLoading] = useState(false);
  const [appraisalRows, setAppraisalRows] = useState<DisposalAppraisalListItem[]>([]);
  const [appraisalLoading, setAppraisalLoading] = useState(false);

  const [reqSearch, setReqSearch] = useState('');
  const [reqStatusFilter, setReqStatusFilter] = useState<number | 'all'>('all');
  const [reqSentDate, setReqSentDate] = useState<string | null>(null);

  const [isLiquidationDetailOpen, setIsLiquidationDetailOpen] = useState(false);
  const [liquidationDetailRow, setLiquidationDetailRow] = useState<TransferRequestListItem | null>(null);
  const [isLiquidationApproveOpen, setIsLiquidationApproveOpen] = useState(false);
  const [selectedLiquidationItem, setSelectedLiquidationItem] = useState<TransferRequestListItem | null>(null);
  const [liquidationDecision, setLiquidationDecision] = useState<'approved' | 'rejected'>('approved');
  const [liquidationComment, setLiquidationComment] = useState('');
  const [liquidationSubmitting, setLiquidationSubmitting] = useState(false);
  const [isLiquidationExecutionOpen, setIsLiquidationExecutionOpen] = useState(false);
  const [liquidationExecutionRequestId, setLiquidationExecutionRequestId] = useState<number | null>(null);
  const [liquidationExecutionCode, setLiquidationExecutionCode] = useState('');

  const [isAppraisalDetailOpen, setIsAppraisalDetailOpen] = useState(false);
  const [viewAppraisalId, setViewAppraisalId] = useState<number | null>(null);

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

  const reloadAppraisalList = useCallback(async () => {
    if (!userProfile?.id) return;
    setAppraisalLoading(true);
    try {
      const rows = await disposalAppraisalService.getMyAppraisals(userProfile.id);
      setAppraisalRows(rows);
    } catch {
      message.error('Không tải được danh sách thẩm định thanh lý.');
      setAppraisalRows([]);
    } finally {
      setAppraisalLoading(false);
    }
  }, [userProfile?.id]);

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

  useEffect(() => {
    if (mainPill !== 'appraisals' || !userProfile?.id) return;
    void reloadAppraisalList();
  }, [mainPill, userProfile?.id, reloadAppraisalList]);

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

  const filteredAppraisalRows = useMemo(() => {
    const keyword = reqSearch.trim().toLowerCase();
    return appraisalRows.filter((row) => {
      let matchDate = true;
      if (reqSentDate && row.scheduledAt) {
        try {
          const rowDate = new Date(row.scheduledAt).toISOString().slice(0, 10);
          matchDate = rowDate === reqSentDate;
        } catch {
          matchDate = true;
        }
      }
      const matchKeyword =
        !keyword ||
        row.requestTitle.toLowerCase().includes(keyword) ||
        `yc-${row.assetRequestId}`.includes(keyword);
      return matchDate && matchKeyword;
    });
  }, [appraisalRows, reqSearch, reqSentDate]);

  const showRequestTabs = isAccountantRole || isDeptManager;

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

      {showRequestTabs && (
        <div style={{ display: 'flex', gap: 8, marginBottom: 4 }}>
          <Button type={mainPill === 'requests' ? 'primary' : 'default'} onClick={() => setMainPill('requests')}>
            Yêu cầu thanh lý
          </Button>
          <Button
            type={mainPill === 'appraisals' ? 'primary' : 'default'}
            onClick={() => setMainPill('appraisals')}
          >
            Thẩm định tài sản
          </Button>
        </div>
      )}

      {showRequestTabs && (mainPill === 'requests' || mainPill === 'appraisals') && (
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
              {mainPill === 'requests' && (
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
              )}
              <DatePicker
                placeholder={mainPill === 'appraisals' ? 'Ngày hẹn thẩm định' : 'Ngày gửi'}
                className="liquidation-select liquidation-date-picker"
                onChange={(_, dateString) => setReqSentDate(dateString || null)}
              />
            </div>
          </div>

          <div className="asset-table-wrapper liquidation-table-wrapper">
            <table className="asset-table liquidation-table">
              <thead>
                <tr>
                  {mainPill === 'appraisals' ? (
                    <>
                      <th>MÃ YÊU CẦU</th>
                      <th>NGÀY HẸN THẨM ĐỊNH</th>
                      <th>THÔNG TIN THẨM ĐỊNH</th>
                      <th>TRẠNG THÁI</th>
                      <th className="asset-table__cell asset-table__cell--actions" />
                    </>
                  ) : (
                    <>
                      <th>MÃ YÊU CẦU</th>
                      <th>NGÀY GỬI</th>
                      <th>MÃ TÀI SẢN</th>
                      <th>MÃ CÁ THỂ</th>
                      <th>TÊN TÀI SẢN</th>
                      <th>PHÒNG BAN</th>
                      <th>NỘI DUNG</th>
                      <th>TRẠNG THÁI</th>
                      <th className="asset-table__cell asset-table__cell--actions" />
                    </>
                  )}
                </tr>
              </thead>
              <tbody>
                {mainPill === 'requests' ? (
                  disposalLoading ? (
                    <tr>
                      <td colSpan={9} className="liquidation-table-empty">
                        Đang tải...
                      </td>
                    </tr>
                  ) : filteredDisposalRows.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="liquidation-table-empty">
                        Không có dữ liệu.
                      </td>
                    </tr>
                  ) : (
                    filteredDisposalRows.map((row) => {
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
                                  setIsLiquidationApproveOpen(true);
                                }}
                              >
                                Phê duyệt
                              </Button>
                            )}
                            {isAccountantRole && row.status === 2 && (
                              <Button
                                type="text"
                                size="small"
                                onClick={() => {
                                  setLiquidationExecutionRequestId(row.assetRequestId);
                                  setLiquidationExecutionCode(row.code ?? `YC-${row.assetRequestId}`);
                                  setIsLiquidationExecutionOpen(true);
                                }}
                              >
                                Thực hiện thanh lý
                              </Button>
                            )}
                          </td>
                        </tr>
                      );
                    })
                  )
                ) : appraisalLoading ? (
                  <tr>
                    <td colSpan={5} className="liquidation-table-empty">
                      Đang tải...
                    </td>
                  </tr>
                ) : filteredAppraisalRows.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="liquidation-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  filteredAppraisalRows.map((row) => {
                    const statusText =
                      row.status === 4
                        ? 'Đã xác nhận hội đồng'
                        : row.status === 2
                          ? 'Đã có biên bản'
                          : row.status === 1
                            ? 'Đã lên lịch'
                            : 'Đang xử lý';
                    return (
                      <tr key={row.appraisalId} className="asset-row">
                        <td>YC-{row.assetRequestId}</td>
                        <td>{row.scheduledAt ? formatDate(row.scheduledAt) : '—'}</td>
                        <td>
                          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                            <span>{row.requestTitle || '—'}</span>
                            <span style={{ color: '#6b7280', fontSize: 12 }}>
                              {row.isReporter ? 'Bạn là người nhập biên bản' : 'Bạn là thành viên liên quan'}
                              {row.meetingDepartmentName || row.meetingLocation
                                ? ` - ${row.meetingDepartmentName ?? row.meetingLocation}`
                                : ''}
                            </span>
                          </div>
                        </td>
                        <td>
                          <span className="asset-status-pill asset-status-pill--processing">{statusText}</span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            size="small"
                            onClick={() => {
                              setViewAppraisalId(row.appraisalId);
                              setIsAppraisalDetailOpen(true);
                            }}
                          >
                            Xem
                          </Button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
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
          submitting={liquidationSubmitting}
          onConfirm={async () => {
            if (!selectedLiquidationItem || !userProfile?.id) return;
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

      {isLiquidationExecutionOpen && isAccountantRole && (
        <LiquidationExecutionModal
          open
          assetRequestId={liquidationExecutionRequestId}
          requestCode={liquidationExecutionCode}
          userId={userProfile?.id}
          onClose={() => {
            setIsLiquidationExecutionOpen(false);
            setLiquidationExecutionRequestId(null);
            setLiquidationExecutionCode('');
          }}
          onSuccess={async () => {
            await reloadDisposalRows();
          }}
        />
      )}

      <DisposalAppraisalDetailModal
        open={isAppraisalDetailOpen}
        appraisalId={viewAppraisalId}
        userId={userProfile?.id}
        onClose={() => {
          setIsAppraisalDetailOpen(false);
          setViewAppraisalId(null);
        }}
        onRefreshList={reloadAppraisalList}
      />
    </div>
  );
}
