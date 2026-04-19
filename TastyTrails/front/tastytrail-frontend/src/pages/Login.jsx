import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../Login.css';

export default function Login() {
    const [formData, setFormData] = useState({
        email: '',
        password: ''
    });

    const navigate = useNavigate();

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            const response = await fetch('http://localhost:5146/api/auth/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData),
            });

            if (response.ok) {
                const data = await response.json();

                const receivedId = data.userId || data.UserId || data.id || data.Id;
                const receivedToken = data.token || data.Token;

                if (receivedId) {
                    localStorage.setItem("authToken", receivedToken);
                    localStorage.setItem("userId", receivedId);

                    //alert("Welcome back!");
                    navigate(`/profile/${receivedId}`);
                    window.location.reload();
                } else {
                    console.error("ID not found:", data);
                    //alert("Server error: no user ID.");
                }
            } else {
                //alert("Invalid email or password.");
            }
        } catch (err) {
            console.error(err);
            alert("Server error.");
        }
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <h2 className="login-logo">TastyTrail</h2>
                <p className="login-subtitle">MEMBER LOGIN</p>

                <form onSubmit={handleSubmit} className="login-form">
                    <div className="input-group">
                        <label>EMAIL</label>
                        <input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            placeholder="Enter your email"
                            required
                        />
                    </div>

                    <div className="input-group">
                        <label>PASSWORD</label>
                        <input
                            type="password"
                            name="password"
                            value={formData.password}
                            onChange={handleChange}
                            placeholder="Enter your password"
                            required
                        />
                    </div>

                    <button type="submit" className="login-btn">
                        SIGN IN
                    </button>
                </form>

                <p className="login-footer">
                    NOT A MEMBER?
                    <span onClick={() => navigate('/register')}>
                        REGISTER
                    </span>
                </p>
            </div>
        </div>
    );
}