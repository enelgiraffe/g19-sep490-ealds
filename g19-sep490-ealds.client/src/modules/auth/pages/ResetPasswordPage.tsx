import { ResetPasswordForm } from '../components/ResetPasswordForm';
import backgroundImg from '/images/background-login.jpg';
import './ResetPasswordPage.css';

export const ResetPasswordPage = () => {
  return (
    <div className="reset-password-page">
      <img src={backgroundImg} alt="Background" className="reset-password-background-img" />
      <div className="reset-password-background-overlay" />
      <div className="reset-password-content">
        <ResetPasswordForm />
      </div>
    </div>
  );
};
