import { useState } from 'react';
import { Tabs } from 'antd';
import { BasicInfoTab } from '../components/BasicInfoTab';
import { PasswordTab } from '../components/PasswordTab';
import './ProfilePage.css';

export function ProfilePage() {
  const [activeTab, setActiveTab] = useState('basic');

  const items = [
    {
      key: 'basic',
      label: 'Thông tin cơ bản',
      children: <BasicInfoTab />,
    },
    {
      key: 'password',
      label: 'Đổi mật khẩu',
      children: <PasswordTab />,
    },
  ];

  return (
    <div className="profile-page">
      <div className="profile-page__header">
        <h1 className="profile-page__title">Hồ sơ cá nhân</h1>
      </div>
      
      <div className="profile-page__content">
        <Tabs 
          activeKey={activeTab} 
          onChange={setActiveTab}
          items={items}
          className="profile-page__tabs"
          type="card"
        />
      </div>
    </div>
  );
}
