import { useEffect, useState } from 'react';
import { Form, Input, DatePicker, Button, message, Spin } from 'antd';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { profileService } from '../services/profileService';
import './BasicInfoTab.css';

interface BasicInfoFormValues {
  email: string;
  fullName: string;
  birthday?: Dayjs;
  position: string;
  employeeCode?: string;
  department?: string;
  phone?: string;
  address?: string;
}

export function BasicInfoTab() {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [editing, setEditing] = useState(false);

  const loadProfile = async () => {
    try {
      setLoading(true);
      const profile = await profileService.getProfile();
      const values: Partial<BasicInfoFormValues> = {
        email: profile.email,
        fullName: profile.name,
        birthday: profile.dob ? dayjs(profile.dob) : undefined,
        position: profile.role,
        employeeCode: profile.employeeCode ?? undefined,
        department: profile.departmentName ?? undefined,
        phone: profile.phone ?? undefined,
        address: profile.address ?? undefined,
      };
      form.setFieldsValue(values);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Không thể tải thông tin hồ sơ.';
      message.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProfile();
  }, []);

  const handleUpdate = async (values: BasicInfoFormValues) => {
    try {
      setSubmitting(true);
      const payload = {
        name: values.fullName,
        phone: values.phone ?? null,
        address: values.address ?? null,
        dob: values.birthday ? values.birthday.format('YYYY-MM-DD') : null,
        gender: null,
        imageUrl: null,
      };

      const updated = await profileService.updateProfile(payload);

      form.setFieldsValue({
        email: updated.email,
        fullName: updated.name,
        birthday: updated.dob ? dayjs(updated.dob) : undefined,
        position: updated.role,
        employeeCode: updated.employeeCode ?? undefined,
        department: updated.departmentName ?? undefined,
        phone: updated.phone ?? undefined,
        address: updated.address ?? undefined,
      });

      message.success('Cập nhật hồ sơ thành công.');
      setEditing(false);
    } catch (error: any) {
      const errorMessage =
        error?.response?.data?.message ||
        'Cập nhật hồ sơ thất bại. Vui lòng thử lại.';
      message.error(errorMessage);
    } finally {
      setSubmitting(false);
    }
  };

  const isReadOnly = !editing;

  return (
    <div className="basic-info-tab">
      <div className="basic-info-tab__header">
        <Button
          type="primary"
          icon={<span>✏️</span>}
          className="basic-info-tab__edit-btn"
          onClick={() => setEditing((prev) => !prev)}
        >
          {editing ? 'Hủy' : 'Chỉnh sửa'}
        </Button>
      </div>

      <Spin spinning={loading || submitting}>
        <Form<BasicInfoFormValues>
          form={form}
          layout="vertical"
          onFinish={handleUpdate}
          className="basic-info-form"
        >
          <div className="basic-info-form__row">
            <Form.Item label="Email" name="email" className="basic-info-form__item">
              <Input placeholder="Nhập email" disabled />
            </Form.Item>
            <Form.Item label="Vị trí công việc" name="position" className="basic-info-form__item">
              <Input placeholder="Nhập vị trí công việc" disabled />
            </Form.Item>
          </div>

          <div className="basic-info-form__row">
            <Form.Item
              label="Tên đầy đủ"
              name="fullName"
              className="basic-info-form__item"
              rules={[{ required: true, message: 'Vui lòng nhập họ tên' }]}
            >
              <Input placeholder="Nhập tên đầy đủ" disabled={isReadOnly} />
            </Form.Item>
            <Form.Item label="Mã nhân viên" name="employeeCode" className="basic-info-form__item">
              <Input placeholder="Nhập mã nhân viên" disabled />
            </Form.Item>
          </div>

          <div className="basic-info-form__row">
            <Form.Item label="Ngày tháng năm sinh" name="birthday" className="basic-info-form__item">
              <DatePicker
                style={{ width: '100%' }}
                format="DD/MM/YYYY"
                placeholder="Chọn ngày sinh"
                disabled={isReadOnly}
              />
            </Form.Item>
            <Form.Item label="Phòng ban" name="department" className="basic-info-form__item">
              <Input placeholder="Nhập phòng ban" disabled />
            </Form.Item>
          </div>

          <div className="basic-info-form__row">
            <Form.Item
              label="Số điện thoại"
              name="phone"
              className="basic-info-form__item"
              rules={[
                {
                  pattern: /^[0-9+()\-.\s]*$/,
                  message: 'Số điện thoại không hợp lệ',
                },
              ]}
            >
              <Input placeholder="Nhập số điện thoại" disabled={isReadOnly} />
            </Form.Item>
            <Form.Item
              label="Địa chỉ"
              name="address"
              className="basic-info-form__item"
            >
              <Input placeholder="Nhập địa chỉ" disabled={isReadOnly} />
            </Form.Item>
          </div>

          {editing && (
            <Form.Item>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Lưu thay đổi
              </Button>
            </Form.Item>
          )}
        </Form>
      </Spin>
    </div>
  );
}
