import { useEffect, useState } from 'react';
import { Modal, Form, Input, DatePicker, Button, Select } from 'antd';
import dayjs from 'dayjs';
import { transferRequestService, type AssetLocationOption } from '../services/transferRequestService';
import './TransferAssetModal.css';

const { TextArea } = Input;

interface AssetInfo {
  code: string;
  name: string;
  type: string;
  specification: string;
  purchaseDate: string;
  warrantyExpiry: string;
  currentValue: string;
  remainingValue: string;
  location: string;
  status: string;
  admissionDate: string;
  department: string;
}

interface TransferAssetModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: any) => void;
  assetInfo: AssetInfo | null;
  fromDepartmentId?: number | null;
}

export function TransferAssetModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
  fromDepartmentId,
}: TransferAssetModalProps) {
  const [form] = Form.useForm();
  const [locations, setLocations] = useState<AssetLocationOption[]>([]);
  const [locationsLoading, setLocationsLoading] = useState(false);

  useEffect(() => {
    if (open) {
      setLocationsLoading(true);
      transferRequestService
        .getAssetLocations()
        .then(setLocations)
        .catch(() => setLocations([]))
        .finally(() => setLocationsLoading(false));
    }
  }, [open]);

  useEffect(() => {
    if (open && fromDepartmentId) {
      form.setFieldsValue({ fromLocationId: fromDepartmentId });
    }
  }, [open, fromDepartmentId, form]);

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      onSubmit(values);
      form.resetFields();
      onClose();
    });
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={900}
      centered
      className="transfer-modal"
      closeIcon={<span className="transfer-modal__close">×</span>}
    >
      <div className="transfer-modal__header">
        <h2 className="transfer-modal__title">Yêu cầu điều chuyển</h2>
      </div>

      <div className="transfer-modal__content">
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            transferDate: dayjs(),
          }}
        >
          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Thông tin chung</h3>

            <div className="transfer-form__row">
              <Form.Item label="Số biên bản" className="transfer-form__item">
                <Input defaultValue="-" disabled className="transfer-input--disabled" />
              </Form.Item>
              <Form.Item
                label="Ngày điều chuyển"
                name="transferDate"
                className="transfer-form__item"
                rules={[{ required: true, message: 'Vui lòng chọn ngày điều chuyển' }]}
              >
                <DatePicker
                  style={{ width: '100%' }}
                  placeholder="dd/mm/yyyy"
                  format="DD/MM/YYYY"
                />
              </Form.Item>
            </div>

            <Form.Item
              label="Lý do điều chuyển"
              name="reason"
            >
              <TextArea rows={3} placeholder="Nhập lý do điều chuyển" />
            </Form.Item>
          </div>

          <div className="transfer-form__section transfer-form__section--locations">
            <h3 className="transfer-section-title">Điều chuyển</h3>
            <div className="transfer-form__row">
              <Form.Item
                label="Từ vị trí"
                name="fromLocationId"
                className="transfer-form__item"
                rules={[{ required: true, message: 'Chọn vị trí nguồn' }]}
              >
                <Select
                  placeholder="Chọn vị trí hiện tại"
                  loading={locationsLoading}
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={locations.map((loc) => ({ value: loc.locationId, label: loc.displayName }))}
                />
              </Form.Item>
              <Form.Item
                label="Đến vị trí"
                name="toLocationId"
                className="transfer-form__item"
                rules={[{ required: true, message: 'Chọn vị trí đích' }]}
              >
                <Select
                  placeholder="Chọn vị trí chuyển đến"
                  loading={locationsLoading}
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={locations.map((loc) => ({ value: loc.locationId, label: loc.displayName }))}
                />
              </Form.Item>
            </div>
          </div>

          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Tài sản được chuyển</h3>
            {assetInfo ? (
              <div className="transfer-asset-table">
                <table>
                  <thead>
                    <tr>
                      <th>STT</th>
                      <th>Mã tài sản</th>
                      <th>Tài sản</th>
                      <th>Vị trí tài sản</th>
                      <th>Tình trạng</th>
                      <th>Số lượng</th>
                      <th>Phòng ban sử dụng</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>1</td>
                      <td>{assetInfo.code}</td>
                      <td>{assetInfo.name}</td>
                      <td>{assetInfo.location}</td>
                      <td>{assetInfo.status}</td>
                      <td>1</td>
                      <td>{assetInfo.department}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="transfer-asset-placeholder">
                Vui lòng chọn tài sản từ danh sách tài sản để hiển thị thông tin chi tiết.
              </p>
            )}
          </div>

          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Tài liệu đính kèm</h3>
            <div className="transfer-attachments">
              <div className="transfer-attachment-item">
                <span>#1</span>
                <span>Thông tin máy</span>
              </div>
              <div className="transfer-attachment-item">
                <span>#2</span>
                <span>Thông tin nhà cung cấp</span>
              </div>
            </div>
          </div>

          <div className="transfer-modal__footer">
            <Button
              type="primary"
              onClick={handleSubmit}
              className="transfer-btn-submit"
            >
              Gửi yêu cầu
            </Button>
            <Button
              onClick={onClose}
              className="transfer-btn-cancel"
            >
              Nháp
            </Button>
          </div>
        </Form>
      </div>
    </Modal>
  );
}

