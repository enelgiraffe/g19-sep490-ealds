import { LoginForm } from '../components/LoginForm';
import backgroundImg from '/images/background-login.jpg';
import './LoginPage.css';

export const LoginPage = () => {
  return (
    <div className="login-page">
      <img src={backgroundImg} alt="Background" className="login-background-img" />
      <div className="login-background-overlay"></div>
      <div className="login-content">
        <LoginForm />
      </div>
    </div>
  );
};
