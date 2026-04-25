import { useEffect, useRef, useState } from 'react';
import { Form, Input, Button, Table, InputNumber, Select, DatePicker, Modal, message } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import { assetService, type AssetTypeItem } from '../../assets/services/assetService';
import { assetTypeService } from '../../admin/services/assetTypeService';
import { assetCategoryService, type AssetCategoryItem } from '../../admin/services/assetCategoryService';
import './CreatePurchaseOrderModal.css';

const { TextArea } = Input;

export interface CreatePurchaseFormValues {
  title: string;
  description?: string;
  reason?: string;
  needDate?: Dayjs;
  equipment?: {
    assetTypeId?: string;
    quantity?: number;
  }[];
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

  // --- Create asset type inline ---
  const [addTypeOpen, setAddTypeOpen] = useState(false);
  const [addTypeForm] = Form.useForm<{ name: string; categoryId: number }>();
  const [categories, setCategories] = useState<AssetCategoryItem[]>([]);
  const [addingType, setAddingType] = useState(false);
  const pendingRowIndexRef = useRef<number | null>(null);

  useEffect(() => {
    if (!open) return;
    assetService
      .getAssetTypes()
      .then((types) => setAssetTypes(types))
      .catch(() => setAssetTypes([]));
    assetCategoryService
      .getAll()
      .then((cats) => setCategories(cats))
      .catch(() => setCategories([]));
  }, [open]);

  useEffect(() => {
    if (open) {
      form.resetFields();
      if (initialValues) {
        const mappedEquipment =
          initialValues.equipment?.map((line) => {
            const currentId = String(line.assetTypeId ?? '').trim();
            if (currentId) return line;
            const rawName = String((line as { assetTypeName?: string }).assetTypeName ?? '').trim().toLowerCase();
            if (!rawName) return line;
            const found = assetTypes.find((t) => t.name.trim().toLowerCase() === rawName);
            if (!found) return line;
            return { ...line, assetTypeId: String(found.assetTypeId) };
          }) ?? initialValues.equipment;
        form.setFieldsValue({
          ...initialValues,
          equipment: mappedEquipment,
        });
      } else {
        form.setFieldsValue({
          equipment: [
            { assetTypeId: undefined, quantity: 1 },
          ],
        });
      }
    }
  }, [open, form, initialValues, assetTypes]);

  const buildProposedData = (values: CreatePurchaseFormValues): string | undefined => {
    const equipment = values.equipment?.filter(
      (e) => e?.assetTypeId != null && String(e.assetTypeId).trim() !== ''
    );
    if (!equipment?.length) return undefined;
    const rows = equipment.map((e) => {
      const q = Number(e.quantity) || 1;
      const typeId = Number(e.assetTypeId);
      const type = assetTypes.find((t) => t.assetTypeId === typeId);
      return {
        assetTypeId: Number.isFinite(typeId) ? typeId : null,
        assetTypeName: type?.name ?? null,
        quantity: q,
      };
    });

    return JSON.stringify({
      equipment: rows,
    });
  };

  const handleOpenAddType = (rowIndex: number) => {
    pendingRowIndexRef.current = rowIndex;
    addTypeForm.resetFields();
    setAddTypeOpen(true);
  };

  const handleAddTypeSubmit = async () => {
    try {
      const vals = await addTypeForm.validateFields();
      setAddingType(true);
      const created = await assetTypeService.create({ name: vals.name.trim(), categoryId: vals.categoryId });
      const newItem: AssetTypeItem = { assetTypeId: created.assetTypeId, name: created.name };
      setAssetTypes((prev) => [...prev, newItem]);
      if (pendingRowIndexRef.current !== null) {
        const equipment = form.getFieldValue('equipment') as { assetTypeId?: string; quantity?: number }[];
        const updated = equipment.map((row, idx) =>
          idx === pendingRowIndexRef.current ? { ...row, assetTypeId: String(created.assetTypeId) } : row,
        );
        form.setFieldsValue({ equipment: updated });
      }
      message.success(`Đã tạo loại tài sản "${created.name}"`);
      setAddTypeOpen(false);
    } catch {
      // validation error or API error already shown
    } finally {
      setAddingType(false);
    }
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

      const description = [
        values.reason && `Lý do: ${values.reason}`,
        needDateText && `Thời gian cần: ${needDateText}`,
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

            <div className="create-purchase-form__row create-purchase-form__row--single">
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

            <div className="create-purchase-form__section">
              <h3 className="create-purchase-form__section-title">Danh mục loại tài sản đề xuất</h3>
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
                          title: 'Loại tài sản',
                          key: 'assetTypeId',
                          width: 220,
                          render: (_, __, i) => (
                            <Form.Item
                              name={[i, 'assetTypeId']}
                              noStyle
                              rules={[{ required: true, message: 'Chọn loại tài sản' }]}
                            >
                              <Select
                                showSearch
                                optionFilterProp="label"
                                placeholder="Chọn loại tài sản"
                                options={assetTypes.map((t) => ({
                                  value: String(t.assetTypeId),
                                  label: t.name,
                                }))}
                                dropdownRender={(menu) => (
                                  <>
                                    {menu}
                                    <div style={{ padding: '4px 8px', borderTop: '1px solid #f0f0f0' }}>
                                      <Button
                                        type="link"
                                        icon={<PlusOutlined />}
                                        size="small"
                                        style={{ padding: 0 }}
                                        onMouseDown={(e) => e.preventDefault()}
                                        onClick={() => handleOpenAddType(i)}
                                      >
                                        Tạo loại tài sản mới
                                      </Button>
                                    </div>
                                  </>
                                )}
                              />
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
                          assetTypeId: undefined,
                          quantity: 1,
                        })
                      }
                      className="create-purchase-btn-add-equipment"
                    >
                      Thêm dòng
                    </Button>
                  </>
                )}
              </Form.List>
            </div>

            <div className="create-purchase-form__section">
              <Form.Item
                label="Lý do đề nghị"
                name="reason"
                className="create-purchase-form__item-full"
              >
                <TextArea rows={3} placeholder="Nhập lý do đề nghị" />
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

      <Modal
        title="Tạo loại tài sản mới"
        open={addTypeOpen}
        onCancel={() => setAddTypeOpen(false)}
        onOk={handleAddTypeSubmit}
        okText="Tạo"
        cancelText="Hủy"
        confirmLoading={addingType}
        width={400}
      >
        <Form form={addTypeForm} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item
            label="Tên loại tài sản"
            name="name"
            rules={[{ required: true, message: 'Vui lòng nhập tên loại tài sản' }]}
          >
            <Input placeholder="VD: Máy cắt sắt" />
          </Form.Item>
          <Form.Item
            label="Danh mục"
            name="categoryId"
            rules={[{ required: true, message: 'Vui lòng chọn danh mục' }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Chọn danh mục"
              options={categories.map((c) => ({ value: c.categoryId, label: c.name }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
