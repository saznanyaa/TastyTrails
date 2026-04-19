import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../Login.css';

export default function Registration() {
    const [formData, setFormData] = useState({
        name: '',
        username: '',
        email: '',
        password: '',
    });

    const navigate = useNavigate();

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            const response = await fetch('http://localhost:5146/api/post/Register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData),
            });

            if (response.ok) {
                const data = await response.json();
                const token = data.token;

                if (token) {
                    localStorage.setItem("authToken", token);
                    alert("Registration successful!");
                    navigate('/home');
                } else {
                    alert("Registered, but no token received.");
                }
            } else {
                alert("Registration failed.");
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
                <p className="login-subtitle">CREATE ACCOUNT</p>

                <form onSubmit={handleSubmit} className="login-form">

                    <div className="input-group">
                        <label>NAME</label>
                        <input
                            type="text"
                            name="name"
                            value={formData.name}
                            onChange={handleChange}
                            placeholder="Enter your name"
                            required
                        />
                    </div>

                    <div className="input-group">
                        <label>USERNAME</label>
                        <input
                            type="text"
                            name="username"
                            value={formData.username}
                            onChange={handleChange}
                            placeholder="Enter your username"
                            required
                        />
                    </div>

                    <div className="input-group">
                        <label>EMAIL</label>
                        <input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            placeholder="example@gmail.com"
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
                        REGISTER
                    </button>
                </form>

                <p className="login-footer">
                    ALREADY A MEMBER?
                    <span onClick={() => navigate('/login')}>
                        LOGIN
                    </span>
                </p>
            </div>
        </div>
    );
}