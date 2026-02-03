import { ForgotPasswordForm } from '../components/ForgotPasswordForm';
import backgroundImg from '/images/background-login.jpg';
import './ForgotPasswordPage.css';

export const ForgotPasswordPage = () => {
  return (
    <div className="forgot-password-page">
      <img src={backgroundImg} alt="Background" className="forgot-password-background-img" />
      <div className="forgot-password-background-overlay"></div>
      <div className="forgot-password-content">
        <ForgotPasswordForm />
      </div>
    </div>
  );
};
