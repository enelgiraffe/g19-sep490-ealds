import { VerifyOTPForm } from '../components/VerifyOTPForm';
import backgroundImg from '/images/background-login.jpg';
import './VerifyOTPPage.css';

export const VerifyOTPPage = () => {
  return (
    <div className="verify-otp-page">
      <img src={backgroundImg} alt="Background" className="verify-otp-background-img" />
      <div className="verify-otp-background-overlay"></div>
      <div className="verify-otp-content">
        <VerifyOTPForm />
      </div>
    </div>
  );
};
