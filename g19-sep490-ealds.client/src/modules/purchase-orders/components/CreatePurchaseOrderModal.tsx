import { useEffect, useState } from 'react';
import { Form, Input, Button, Table, InputNumber, Select, DatePicker } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import { assetService, type AssetTypeItem } from '../../assets/services/assetService';
import './CreatePurchaseOrderModal.css';

const { TextArea } = Input;
const { Option } = Select;

export interface CreatePurchaseFormValues {
  title: string;
  description?: string;
  reason?: string;
  needDate?: Dayjs;
  supplier?: string;
  assetType?: string;
  purpose?: string;
  equipment?: {
    name?: string;
    quantity?: number;
    modelCode?: string;
    unit?: string;
    estimatedPrice?: string;
  }[];
}

function parseNumberInput(value: string): string {
  const normalized = value.replace(/[^\d]/g, '');
  return normalized;
}

function formatNumberInput(value?: string): string {
  const raw = String(value ?? '').trim();
  if (!raw) return '';
  const normalized = parseNumberInput(raw);
  if (!normalized) return '';
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed.toLocaleString('en-US') : '';
}

interface CreatePurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: {
    title: string;
    description?: string;
    proposedData?: string;
    status?: number;
  }) => void;
  creatorName?: string | null;
  initialValues?: Partial<CreatePurchaseFormValues>;
  mode?: 'create' | 'edit';
}

export function CreatePurchaseOrderModal({
  open,
  onClose,
  onSubmit,
  creatorName,
  initialValues,
  mode = 'create',
}: CreatePurchaseOrderModalProps) {
  const [form] = Form.useForm<CreatePurchaseFormValues>();
  const [assetTypes, setAssetTypes] = useState<AssetTypeItem[]>([]);

  useEffect(() => {
    if (!open) return;
    assetService
      .getAssetTypes()
      .then((types) => setAssetTypes(types))
      .catch(() => setAssetTypes([]));
  }, [open]);

  useEffect(() => {
    if (open) {
      form.resetFields();
      if (initialValues) {
        const rawAssetType = String(initialValues.assetType ?? '').trim();
        const mappedAssetTypeId = rawAssetType
          ? assetTypes.find((t) => {
              const a = t.name.trim().toLowerCase();
              const b = rawAssetType.toLowerCase();
              return a === b;
            })?.assetTypeId
          : undefined;
        form.setFieldsValue({
          equipment: [
            { name: '', quantity: 1, modelCode: '', unit: 'Cái', estimatedPrice: '' },
          ],
          ...initialValues,
          assetType: mappedAssetTypeId != null ? String(mappedAssetTypeId) : initialValues.assetType,
        });
      } else {
        form.setFieldsValue({
          equipment: [
            { name: '', quantity: 1, modelCode: '', unit: 'Cái', estimatedPrice: '' },
          ],
        });
      }
    }
  }, [open, form, initialValues, assetTypes]);

  const buildProposedData = (values: CreatePurchaseFormValues): string | undefined => {
    const equipment = values.equipment?.filter(
      (e) => e?.name != null && String(e.name).trim() !== ''
    );
    if (!equipment?.length) return undefined;
    let total = 0;
    const rows = equipment.map((e) => {
      const q = Number(e.quantity) || 1;
      const price = parseFloat(String(e.estimatedPrice || '0').replace(/[^\d.-]/g, '')) || 0;
      const rowTotal = q * price;
      total += rowTotal;
      return {
        name: e.name,
        quantity: q,
        modelCode: e.modelCode ?? '',
        unit: e.unit ?? 'Cái',
        estimatedPrice: e.estimatedPrice ?? '0',
      };
    });
    const selectedAssetTypeId = values.assetType ? Number(values.assetType) : null;
    const selectedAssetType =
      selectedAssetTypeId != null && !Number.isNaN(selectedAssetTypeId)
        ? assetTypes.find((t) => t.assetTypeId === selectedAssetTypeId)
        : null;

    return JSON.stringify({
      assetTypeId: selectedAssetType?.assetTypeId ?? null,
      assetTypeName: selectedAssetType?.name ?? null,
      equipment: rows,
      totalPrice: total.toLocaleString('vi-VN') + 'đ',
    });
  };

  const handleSubmitWithStatus = (status: number) => {
    form.validateFields().then((values) => {
      const title = values.title?.trim();
      if (!title) {
        form.setFields([{ name: 'title', errors: ['Vui lòng nhập tiêu đề'] }]);
        return;
      }
      const needDateText =
        values.needDate && typeof (values.needDate as any).format === 'function'
          ? (values.needDate as Dayjs).format('DD/MM/YYYY')
          : undefined;
      const selectedAssetTypeId = values.assetType ? Number(values.assetType) : null;
      const selectedAssetTypeName =
        selectedAssetTypeId != null && !Number.isNaN(selectedAssetTypeId)
          ? assetTypes.find((t) => t.assetTypeId === selectedAssetTypeId)?.name
          : undefined;

      const description = [
        values.reason && `Lý do: ${values.reason}`,
        needDateText && `Thời gian cần: ${needDateText}`,
        values.supplier && `Nhà cung cấp đề xuất: ${values.supplier}`,
        selectedAssetTypeName && `Loại tài sản: ${selectedAssetTypeName}`,
        values.purpose && `Mục đích: ${values.purpose}`,
      ]
        .filter(Boolean)
        .join('\n');
      const proposedData = buildProposedData(values);
      onSubmit({
        title,
        description: description || undefined,
        proposedData: proposedData || undefined,
        status,
      });
      form.resetFields();
      onClose();
    });
  };

  if (!open) return null;

  const titleText = mode === 'edit' ? 'Sửa yêu cầu mua sắm' : 'Tạo yêu cầu mua sắm';

  return (
    <div className="create-purchase-modal-overlay" role="dialog" aria-modal="true">
      <div className="create-purchase-modal">
        <button
          type="button"
          className="create-purchase-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="create-purchase-modal__close">×</span>
        </button>

        <div className="create-purchase-modal__header">
          <h2 className="create-purchase-modal__title">{titleText}</h2>
        </div>

        <div className="create-purchase-modal__body">
          <Form
            form={form}
            layout="vertical"
            className="create-purchase-form"
          >
            {creatorName != null && (
              <div className="create-purchase-form__row create-purchase-form__row--single">
                <Form.Item label="Người gửi" className="create-purchase-form__item">
                  <Input value={creatorName} disabled />
                </Form.Item>
              </div>
            )}

            <div className="create-purchase-form__row create-purchase-form__row--single">
              <Form.Item
                label="Tiêu đề"
                name="title"
                className="create-purchase-form__item"
                rules={[{ required: true, message: 'Vui lòng nhập tiêu đề' }]}
              >
                <Input placeholder="VD: Yêu cầu mua máy cắt sắt phục vụ sản xuất" />
              </Form.Item>
            </div>

            <div className="create-purchase-form__row">
              <Form.Item
                label="Lý do đề nghị"
                name="reason"
                className="create-purchase-form__item"
              >
                <Input placeholder="Nhập lý do" />
              </Form.Item>
              <Form.Item
                label="Thời gian cần vật tư"
                name="needDate"
                className="create-purchase-form__item"
              >
                <DatePicker
                  format="DD/MM/YYYY"
                  style={{ width: '100%' }}
                  placeholder="Chọn ngày"
                />
              </Form.Item>
            </div>

            <div className="create-purchase-form__row">
              <Form.Item
                label="Nhà cung cấp đề xuất"
                name="supplier"
                className="create-purchase-form__item"
              >
                <Input placeholder="Nhập nhà cung cấp" />
              </Form.Item>
              <Form.Item
                label="Loại tài sản"
                name="assetType"
                className="create-purchase-form__item"
              >
                <Select placeholder="Chọn loại tài sản" allowClear>
                  {assetTypes.map((t) => (
                    <Option key={t.assetTypeId} value={String(t.assetTypeId)}>
                      {t.name}
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </div>

            <div className="create-purchase-form__section">
              <h3 className="create-purchase-form__section-title">Danh mục vật tư</h3>
              <Form.List name="equipment">
                {(fields, { add, remove }) => (
                  <>
                    <Table
                      dataSource={fields}
                      pagination={false}
                      rowKey="key"
                      className="create-purchase-equipment-table"
                      columns={[
                        {
                          title: 'STT',
                          key: 'stt',
                          width: 60,
                          align: 'center',
                          render: (_, __, i) => i + 1,
                        },
                        {
                          title: 'Tên vật tư',
                          key: 'name',
                          width: 180,
                          render: (_, __, i) => (
                            <Form.Item name={[i, 'name']} noStyle>
                              <Input placeholder="Nhập tên vật tư" />
                            </Form.Item>
                          ),
                        },
                        {
                          title: 'Số lượng',
                          key: 'quantity',
                          width: 90,
                          render: (_, __, i) => (
                            <Form.Item name={[i, 'quantity']} noStyle initialValue={1}>
                              <InputNumber min={1} style={{ width: '100%' }} />
                            </Form.Item>
                          ),
                        },
                        {
                          title: 'Mã model',
                          key: 'modelCode',
                          width: 110,
                          render: (_, __, i) => (
                            <Form.Item name={[i, 'modelCode']} noStyle>
                              <Input placeholder="Mã model" />
                            </Form.Item>
                          ),
                        },
                        {
                          title: 'Đơn vị',
                          key: 'unit',
                          width: 100,
                          render: (_, __, i) => (
                            <Form.Item name={[i, 'unit']} noStyle initialValue="Cái">
                              <Select style={{ width: '100%' }}>
                                <Option value="Cái">Cái</Option>
                                <Option value="Chiếc">Chiếc</Option>
                                <Option value="Bộ">Bộ</Option>
                                <Option value="Kg">Kg</Option>
                              </Select>
                            </Form.Item>
                          ),
                        },
                        {
                          title: 'Đơn giá dự tính',
                          key: 'estimatedPrice',
                          width: 140,
                          render: (_, __, i) => (
                            <Form.Item
                              name={[i, 'estimatedPrice']}
                              noStyle
                              getValueFromEvent={(e) => parseNumberInput(e?.target?.value ?? '')}
                              getValueProps={(value) => ({ value: formatNumberInput(value) })}
                            >
                              <Input inputMode="numeric" placeholder="VD: 1000000" addonAfter="đ" />
                            </Form.Item>
                          ),
                        },
                        {
                          title: '',
                          key: 'action',
                          width: 50,
                          render: (_, __, i) =>
                            fields.length > 1 ? (
                              <Button
                                type="text"
                                icon={<DeleteOutlined />}
                                danger
                                onClick={() => remove(i)}
                              />
                            ) : null,
                        },
                      ]}
                    />
                    <Button
                      type="dashed"
                      icon={<PlusOutlined />}
                      onClick={() =>
                        add({
                          name: '',
                          quantity: 1,
                          modelCode: '',
                          unit: 'Cái',
                          estimatedPrice: '',
                        })
                      }
                      className="create-purchase-btn-add-equipment"
                    >
                      Thêm vật tư
                    </Button>
                  </>
                )}
              </Form.List>
            </div>

            <div className="create-purchase-form__section">
              <Form.Item
                label="Mục đích sử dụng"
                name="purpose"
                className="create-purchase-form__item-full"
              >
                <TextArea rows={3} placeholder="Mô tả mục đích sử dụng vật tư" />
              </Form.Item>
            </div>
          </Form>
        </div>

        <div className="create-purchase-modal__footer">
          {mode === 'edit' ? (
            <>
              <button
                type="button"
                onClick={() => handleSubmitWithStatus(-1)}
                className="create-purchase-btn-cancel"
              >
                Lưu
              </button>
              <button type="button" onClick={onClose} className="create-purchase-btn-cancel">
                Hủy
              </button>
              <button
                type="button"
                onClick={() => handleSubmitWithStatus(0)}
                className="create-purchase-btn-submit"
              >
                Gửi đi
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={() => handleSubmitWithStatus(0)}
                className="create-purchase-btn-submit"
              >
                Gửi yêu cầu
              </button>
              <button
                type="button"
                onClick={() => handleSubmitWithStatus(-1)}
                className="create-purchase-btn-cancel"
              >
                Nháp
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
