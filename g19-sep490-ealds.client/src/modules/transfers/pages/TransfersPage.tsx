import { useEffect, useMemo, useRef, useState } from 'react';
import { Button, Input, Select, message, Tabs, Popconfirm } from 'antd';
import {
  SearchOutlined,
  FilterOutlined,
  SettingOutlined,
  EyeOutlined,
  DeleteOutlined,
  EditOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import {
  transferRequestService,
  TRANSFER_REQUEST_TYPE_ID,
  type TransferRequestListItem,
} from '../../assets/services/transferRequestService';
import { allocationRequestApiErrorMessage } from '../../allocations/services/allocationRequestService';
import {
  TransferAssetModal,
  type TransferEditDraft,
} from '../../assets/components/TransferAssetModal';
import { TransferRequestDetailModal } from '../components/TransferRequestDetailModal';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import './TransfersPage.css';
import '../../assets/components/MarkDamagedAssetModal.css';

const { Option } = Select;

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã gửi', color: 'processing' },
  2: { label: 'Chờ phê duyệt', color: 'warning' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Phê duyệt', color: 'success' },
};

interface TableRow extends TransferRequestListItem {
  key: string;
  transferDateText: string;
}

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function formatDateTime(iso?: string | null): string {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleString('vi-VN');
  } catch {
    return iso;
  }
}

export function TransfersPage() {
  const [data, setData] = useState<TableRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [searchText, setSearchText] = useState('');
  const [activeTab, setActiveTab] = useState<'outgoing' | 'incoming'>('outgoing');
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [transferEditDraft, setTransferEditDraft] = useState<TransferEditDraft | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [selectedRequest, setSelectedRequest] = useState<TableRow | null>(null);
  const [confirmModal, setConfirmModal] = useState<{ type: 'send' | 'receive'; row: TableRow } | null>(
    null,
  );
  const [handoverNote, setHandoverNote] = useState('');
  const [confirmSubmitting, setConfirmSubmitting] = useState(false);
  const tableHostRef = useRef<HTMLDivElement | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [profile, setProfile] = useState<UserProfile | null>(null);

  useEffect(() => {
    profileService
      .getProfile()
      .then((p) => setProfile(p))
      .catch(() => setProfile(null));
  }, []);

  const loadList = async () => {
    setLoading(true);
    try {
      const list = await transferRequestService.getList();
      const rows: TableRow[] = list.map((item) => ({
        ...item,
        key: `ar-${item.assetRequestId}`,
        transferDateText: formatDate(item.transferDate),
      }));
      setData(rows);
    } catch {
      message.error('Không tải được danh sách yêu cầu điều chuyển.');
      setData([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadList();
  }, []);

  useEffect(() => {
    if (confirmModal) setHandoverNote('');
  }, [confirmModal]);

  const isAccountant = (profile?.role ?? '').toUpperCase() === 'ACCOUNTANT';

  const filteredData = useMemo(() => {
    return data.filter((row) => {
      if (row.isIncompleteProposedDraft) {
        if (isAccountant) {
          // include on both tabs; optional nuance: restrict by tab is unnecessary for accountants
        } else {
          if (profile?.id == null || row.createdBy !== profile.id) return false;
          if (activeTab !== 'outgoing') return false;
        }
      } else if (activeTab === 'outgoing') {
        if (!isAccountant) {
          const fromMyDept =
            profile?.departmentId != null && row.fromDepartmentId === profile.departmentId;
          if (!fromMyDept) return false;
        }
      } else if (!isAccountant) {
        const toMyDept =
          profile?.departmentId != null && row.toDepartmentId === profile.departmentId;
        if (!toMyDept) return false;
      }

      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const kw = searchText.trim().toLowerCase();
      const matchSearch =
        !kw ||
        row.code.toLowerCase().includes(kw) ||
        (row.instanceCode ?? '').toLowerCase().includes(kw) ||
        row.assetCode.toLowerCase().includes(kw) ||
        row.assetName.toLowerCase().includes(kw);
      return matchStatus && matchSearch;
    });
  }, [data, activeTab, isAccountant, profile?.departmentId, profile?.id, statusFilter, searchText]);

  useEffect(() => {
    // Reset to first page when filters change
    setPage(1);
  }, [activeTab, statusFilter, searchText]);

  const total = filteredData.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedData = filteredData.slice((safePage - 1) * pageSize, safePage * pageSize);

  return (
    <div className="transfers-page">
      <div className="transfers-header">
        <h1 className="transfers-title">Điều chuyển</h1>
        <Button
          type="primary"
          danger
          className="transfers-btn-add"
          onClick={() => {
            setTransferEditDraft(null);
            setIsTransferModalOpen(true);
          }}
        >
          + Tạo yêu cầu điều chuyển
        </Button>
      </div>

      <div className="transfers-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'outgoing' | 'incoming')}
          className="transfers-tabs"
          items={[
            { key: 'outgoing', label: 'Đơn đi' },
            { key: 'incoming', label: 'Đơn đến' },
          ]}
        />

        <div className="transfers-filters">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="transfers-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
          />
          <Select
            placeholder="Trạng thái"
            className="transfers-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
          >
            <Option value="all">Tất cả</Option>
            {Object.entries(STATUS_MAP).map(([k, v]) => (
              <Option key={k} value={Number(k)}>
                {v.label}
              </Option>
            ))}
          </Select>
          <Button
            icon={<FilterOutlined />}
            className="transfers-filter-advanced"
            onClick={() => {
              setSearchText('');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="transfers-settings" />
        </div>

        <div className="asset-table-wrapper transfers-table-wrapper" ref={tableHostRef}>
          {loading ? (
            <div className="transfers-table-loading">Đang tải danh sách yêu cầu điều chuyển...</div>
          ) : (
            <table className="asset-table transfers-table">
              <thead>
                <tr>
                  <th>STT</th>
                  <th>SỐ BIÊN BẢN</th>
                  <th>NGÀY ĐIỀU CHUYỂN</th>
                  <th>ĐIỀU CHUYỂN TỪ</th>
                  <th>ĐIỀU CHUYỂN ĐẾN</th>
                  <th>SỐ LƯỢNG</th>
                  <th>TRẠNG THÁI</th>
                  <th>BÀN GIAO</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedData.length === 0 ? (
                  <tr>
                    <td colSpan={10} className="transfers-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  pagedData.map((row, idx) => {
                    const stt = (safePage - 1) * pageSize + idx + 1;
                    const config = STATUS_MAP[row.status] ?? STATUS_MAP[0];
                    return (
                      <tr key={row.key} className="asset-row">
                        <td className="asset-align-right">{stt}</td>
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              if (row.isIncompleteProposedDraft) {
                                message.info(
                                  'Bản nháp chưa gửi: hoàn tất thông tin rồi lưu hoặc gửi yêu cầu để xem chi tiết biên bản.',
                                );
                                return;
                              }
                              setSelectedRequest(row);
                              setIsDetailModalOpen(true);
                            }}
                          >
                            {row.code}
                          </button>
                        </td>
                        <td>{row.transferDateText}</td>
                        <td>{row.fromDepartment}</td>
                        <td>{row.toDepartment}</td>
                        <td className="asset-align-right">{row.quantity}</td>
                        <td>
                          <span
                            className={
                              config.color === 'success'
                                ? 'asset-status-pill asset-status-pill--active'
                                : config.color === 'default'
                                ? 'asset-status-pill asset-status-pill--inactive'
                                  : config.color === 'processing'
                                    ? 'asset-status-pill asset-status-pill--processing'
                                    : config.color === 'warning'
                                      ? 'asset-status-pill asset-status-pill--warning'
                                      : config.color === 'error'
                                        ? 'asset-status-pill asset-status-pill--danger'
                                : 'asset-status-pill'
                            }
                          >
                            {config.label}
                          </span>
                        </td>
                        <td>
                          {activeTab === 'outgoing' ? (
                            row.status === 4 ? (
                              row.isSenderConfirmed ? (
                                <span className="transfers-handover-tag transfers-handover-tag--done">Đã gửi</span>
                              ) : (
                                <Button type="link" size="small" onClick={() => setConfirmModal({ type: 'send', row })}>
                                  Xác nhận đã gửi
                                </Button>
                              )
                            ) : (
                              '—'
                            )
                          ) : row.status === 4 ? (
                            row.isReceiverConfirmed ? (
                              <span className="transfers-handover-tag transfers-handover-tag--done">Đã nhận</span>
                            ) : !row.isSenderConfirmed ? (
                              <span className="transfers-handover-tag transfers-handover-tag--waiting">
                                Chờ bên gửi xác nhận
                              </span>
                            ) : (
                              <Button type="link" size="small" onClick={() => setConfirmModal({ type: 'receive', row })}>
                                Xác nhận đã nhận
                              </Button>
                            )
                          ) : (
                            '—'
                          )}
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 4 }}>
                            {row.isIncompleteProposedDraft &&
                              profile?.id != null &&
                              row.createdBy === profile.id &&
                              typeof row.draftFormJson === 'string' && (
                                <Button
                                  type="text"
                                  title="Sửa bản nháp"
                                  icon={<EditOutlined />}
                                  onClick={() => {
                                    setTransferEditDraft({
                                      assetRequestId: row.assetRequestId,
                                      draftFormJson: row.draftFormJson!,
                                    });
                                    setIsTransferModalOpen(true);
                                  }}
                                />
                              )}
                            <Button
                              type="text"
                              icon={<EyeOutlined />}
                              onClick={() => {
                                if (row.isIncompleteProposedDraft) {
                                  message.info(
                                    'Bản nháp chưa gửi: hoàn tất thông tin rồi lưu hoặc gửi yêu cầu để xem chi tiết biên bản.',
                                  );
                                  return;
                                }
                                setSelectedRequest(row);
                                setIsDetailModalOpen(true);
                              }}
                            />
                            {row.status === 0 &&
                              profile?.id != null &&
                              row.createdBy === profile.id && (
                              <Popconfirm
                                title="Xóa yêu cầu điều chuyển?"
                                description="Thao tác này sẽ xóa yêu cầu và hoàn tác phòng ban/vị trí hiện tại theo dữ liệu điều chuyển."
                                okText="Xóa"
                                cancelText="Hủy"
                                onConfirm={async () => {
                                  try {
                                    await transferRequestService.delete(row.assetRequestId);
                                    message.success('Đã xóa yêu cầu điều chuyển.');
                                    await loadList();
                                  } catch (e: unknown) {
                                    const fromApi =
                                      axios.isAxiosError(e) && e.response?.data != null
                                        ? allocationRequestApiErrorMessage(e.response.data)
                                        : null;
                                    message.error(fromApi ?? 'Xóa yêu cầu thất bại.');
                                  }
                                }}
                              >
                                <Button type="text" danger icon={<DeleteOutlined />} />
                              </Popconfirm>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          )}
        </div>

        <div className="transfers-card__footer">
          <div className="transfers-footer__left">
            Số lượng trên trang:
            <select
              className="transfers-footer__select"
              value={pageSize}
              onChange={(e) => {
                const next = Number(e.target.value);
                setPageSize(next);
                setPage(1);
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="transfers-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="transfers-footer__right">
            <button
              className="transfers-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="transfers-footer__pager transfers-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="transfers-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <TransferAssetModal
        open={isTransferModalOpen}
        onClose={() => {
          setIsTransferModalOpen(false);
          setTransferEditDraft(null);
        }}
        mode="department"
        currentUserDepartmentId={profile?.departmentId ?? null}
        editDraft={transferEditDraft}
        onSubmit={async (values: any) => {
          if (!profile) {
            message.error('Không lấy được thông tin người dùng. Vui lòng đăng nhập lại.');
            return;
          }
          if (values.incompleteDraft === true) {
            if (typeof values.draftFormJson !== 'string') {
              message.error('Không lưu được bản nháp.');
              return;
            }
            const editId =
              typeof values.editingAssetRequestId === 'number' && values.editingAssetRequestId > 0
                ? values.editingAssetRequestId
                : null;
            try {
              if (editId != null) {
                await transferRequestService.updateIncompleteDraft(editId, values.draftFormJson);
                message.success('Đã cập nhật bản nháp.');
              } else {
                await transferRequestService.create({
                  assetInstanceId: 0,
                  requestTypeId: TRANSFER_REQUEST_TYPE_ID,
                  fromLocationId: 0,
                  toLocationId: 0,
                  executeBy: profile.id,
                  createdBy: profile.id,
                  incompleteDraft: true,
                  saveAsDraft: true,
                  draftFormJson: values.draftFormJson,
                });
                message.success('Đã lưu bản nháp (chưa hoàn tất thông tin).');
              }
              setIsTransferModalOpen(false);
              setTransferEditDraft(null);
              await loadList();
            } catch (e: unknown) {
              const fromApi =
                axios.isAxiosError(e) && e.response?.data != null
                  ? allocationRequestApiErrorMessage(e.response.data)
                  : null;
              message.error(fromApi ?? 'Lưu bản nháp thất bại.');
            }
            return;
          }
          const assetIds: number[] =
            Array.isArray(values.assetIds) && values.assetIds.length > 0
              ? values.assetIds.map((x: any) => Number(x)).filter((n: number) => Number.isFinite(n) && n > 0)
              : [];
          if (assetIds.length === 0) {
            message.error('Vui lòng chọn ít nhất 1 tài sản.');
            return;
          }

          const fromLocationId = Number(values.fromLocationId);
          const toLocationId = Number(values.toLocationId);
          if (!fromLocationId || !toLocationId) {
            message.error('Vui lòng chọn phòng ban/vị trí hợp lệ.');
            return;
          }

          const transferDateValue = values.transferDate;
          const transferDate =
            transferDateValue && typeof transferDateValue.toISOString === 'function'
              ? transferDateValue.toISOString()
              : undefined;

          const saveAsDraft = values.saveAsDraft === true;
          const nRep = Number(values.replaceIncompleteAssetRequestId);
          const replaceProposedId =
            Number.isFinite(nRep) && nRep > 0 ? nRep : null;
          try {
            for (let i = 0; i < assetIds.length; i++) {
              const assetInstanceId = assetIds[i]!;
              await transferRequestService.create({
                assetInstanceId,
                requestTypeId: TRANSFER_REQUEST_TYPE_ID,
                fromLocationId,
                toLocationId,
                fromUserId: profile.id,
                toUserId: null,
                transferDate,
                executeBy: profile.id,
                createdBy: profile.id,
                title: values.reason ? `Điều chuyển: ${values.reason}` : `Yêu cầu điều chuyển tài sản ${assetInstanceId}`,
                description: values.reason ?? undefined,
                saveAsDraft,
                incompleteDraft: false,
                replaceIncompleteAssetRequestId:
                  i === 0 && replaceProposedId != null ? replaceProposedId : undefined,
              });
            }
            message.success(
              saveAsDraft ? 'Đã lưu bản nháp điều chuyển.' : 'Gửi yêu cầu điều chuyển thành công.',
            );
            setIsTransferModalOpen(false);
            setTransferEditDraft(null);
            await loadList();
          } catch (e: unknown) {
            const fromApi =
              axios.isAxiosError(e) && e.response?.data != null
                ? allocationRequestApiErrorMessage(e.response.data)
                : null;
            message.error(fromApi ?? 'Gửi yêu cầu điều chuyển thất bại.');
          }
        }}
        assetInfo={null}
      />

      <TransferRequestDetailModal
        open={isDetailModalOpen}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedRequest(null);
        }}
        request={selectedRequest}
      />

      {confirmModal && (
        <div className="mark-damaged-modal-overlay" role="dialog" aria-modal="true">
          <div className="mark-damaged-modal">
            <button
              type="button"
              className="mark-damaged-modal__close-btn"
              onClick={() => !confirmSubmitting && setConfirmModal(null)}
              aria-label="Đóng"
            >
              <span className="mark-damaged-modal__close">×</span>
            </button>

            <div className="mark-damaged-modal__header">
              <h2 className="mark-damaged-modal__title">
                {confirmModal.type === 'send' ? 'Xác nhận bàn giao tài sản' : 'Xác nhận tiếp nhận tài sản'}
              </h2>
            </div>

            <div className="mark-damaged-modal__body">
              <div className="mark-damaged-modal__content">
                <div className="mark-damaged-form__item">
                  <label>Số biên bản</label>
                  <input
                    type="text"
                    value={confirmModal.row.code || `YC-${confirmModal.row.assetRequestId}`}
                    readOnly
                    className="mark-damaged-input--disabled"
                  />
                </div>

                <div className="mark-damaged-info-section">
                  <h3 className="mark-damaged-section-title">Thông tin yêu cầu điều chuyển</h3>
                  <div className="mark-damaged-info-grid">
                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item">
                        <label>Mã yêu cầu</label>
                        <div className="mark-damaged-info-value">YC-{confirmModal.row.assetRequestId}</div>
                      </div>
                      <div className="mark-damaged-info-item">
                        <label>Ngày điều chuyển</label>
                        <div className="mark-damaged-info-value">{formatDate(confirmModal.row.transferDate)}</div>
                      </div>
                    </div>

                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item">
                        <label>Điều chuyển từ</label>
                        <div className="mark-damaged-info-value">{confirmModal.row.fromDepartment}</div>
                      </div>
                      <div className="mark-damaged-info-item">
                        <label>Điều chuyển đến</label>
                        <div className="mark-damaged-info-value">{confirmModal.row.toDepartment}</div>
                      </div>
                    </div>

                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item">
                        <label>Mã cá thể / mã tài sản</label>
                        <div className="mark-damaged-info-value">
                          {confirmModal.row.instanceCode || confirmModal.row.assetCode}
                        </div>
                      </div>
                      <div className="mark-damaged-info-item">
                        <label>Tên tài sản</label>
                        <div className="mark-damaged-info-value">{confirmModal.row.assetName}</div>
                      </div>
                    </div>

                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item">
                        <label>Trạng thái xác nhận bên gửi</label>
                        <div className="mark-damaged-info-value">
                          {confirmModal.row.isSenderConfirmed ? 'Đã xác nhận' : 'Chưa xác nhận'}
                        </div>
                      </div>
                      <div className="mark-damaged-info-item">
                        <label>Trạng thái xác nhận bên nhận</label>
                        <div className="mark-damaged-info-value">
                          {confirmModal.row.isReceiverConfirmed ? 'Đã xác nhận' : 'Chưa xác nhận'}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="mark-damaged-form-section">
                  <h3 className="mark-damaged-section-title">Ý kiến kế toán</h3>
                  <div className="mark-damaged-info-value mark-damaged-info-value--multiline">
                    {confirmModal.row.accountantComment?.trim() || '—'}
                  </div>
                </div>
                <div className="mark-damaged-form-section">
                  <h3 className="mark-damaged-section-title">Ý kiến giám đốc</h3>
                  <div className="mark-damaged-info-value mark-damaged-info-value--multiline">
                    {confirmModal.row.directorComment?.trim() || '—'}
                  </div>
                </div>

                <div className="mark-damaged-form-section">
                  <h3 className="mark-damaged-section-title">
                    {confirmModal.type === 'send' ? 'Nội dung xác nhận bàn giao' : 'Nội dung xác nhận tiếp nhận'}
                  </h3>
                  <div className="mark-damaged-info-value">
                    {confirmModal.type === 'send'
                      ? `Xác nhận bên gửi đã bàn giao tài sản ${confirmModal.row.instanceCode || confirmModal.row.assetCode} — ${confirmModal.row.assetName} từ ${confirmModal.row.fromDepartment} đến ${confirmModal.row.toDepartment}.`
                      : `Xác nhận bên nhận đã tiếp nhận tài sản ${confirmModal.row.instanceCode || confirmModal.row.assetCode} — ${confirmModal.row.assetName} tại ${confirmModal.row.toDepartment}.`}
                  </div>
                  <div className="mark-damaged-info-value" style={{ marginTop: 12 }}>
                    Thời điểm xác nhận: {formatDateTime(new Date().toISOString())}
                  </div>
                </div>

                <div className="mark-damaged-form__item" style={{ marginTop: 16 }}>
                  <label htmlFor="transfer-handover-note">Ghi chú biên bản (tuỳ chọn)</label>
                  <textarea
                    id="transfer-handover-note"
                    className="mark-damaged-textarea"
                    rows={3}
                    maxLength={2000}
                    value={handoverNote}
                    onChange={(e) => setHandoverNote(e.target.value)}
                    placeholder="Ví dụ: tình trạng tài sản khi bàn giao, số seal, người chứng kiến…"
                    disabled={confirmSubmitting}
                  />
                </div>
              </div>
            </div>

            <div className="mark-damaged-modal__footer">
              <button
                type="button"
                onClick={async () => {
                  if (!confirmModal) return;
                  if (confirmModal.type === 'receive' && !confirmModal.row.isSenderConfirmed) {
                    message.warning('Bên gửi chưa xác nhận bàn giao, chưa thể xác nhận đã nhận.');
                    return;
                  }
                  setConfirmSubmitting(true);
                  const notePayload = handoverNote.trim() ? handoverNote.trim() : undefined;
                  try {
                    if (confirmModal.type === 'send') {
                      const res = await transferRequestService.confirmSend(confirmModal.row.assetRequestId, {
                        note: notePayload,
                      });
                      message.success(res.message);
                      if (res.isReady) {
                        message.info('Đã hoàn tất bàn giao: vị trí tài sản đã được cập nhật.');
                      }
                    } else {
                      const res = await transferRequestService.confirmReceive(confirmModal.row.assetRequestId, {
                        note: notePayload,
                      });
                      message.success(res.message);
                      if (res.isReady) {
                        message.info('Đã hoàn tất bàn giao: vị trí tài sản đã được cập nhật.');
                      }
                    }
                    setConfirmModal(null);
                    await loadList();
                  } catch (e: unknown) {
                    const fromApi =
                      axios.isAxiosError(e) && e.response?.data != null
                        ? allocationRequestApiErrorMessage(e.response.data)
                        : null;
                    message.error(
                      fromApi ??
                        (confirmModal.type === 'send' ? 'Xác nhận gửi thất bại.' : 'Xác nhận nhận thất bại.'),
                    );
                  } finally {
                    setConfirmSubmitting(false);
                  }
                }}
                className="mark-damaged-btn-submit"
                disabled={confirmSubmitting}
              >
                {confirmSubmitting ? 'Đang xử lý...' : 'Xác nhận'}
              </button>
              <button
                type="button"
                onClick={() => !confirmSubmitting && setConfirmModal(null)}
                className="mark-damaged-btn-draft"
                disabled={confirmSubmitting}
              >
                Quay lại
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

