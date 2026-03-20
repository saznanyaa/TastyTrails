import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

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

        // Podaci moraju da se zovu isto kao u RegisterDto na backendu
        const registerData = {
            name: formData.name,
            username: formData.username,
            email: formData.email,
            password: formData.password
        };

        try {
            const response = await fetch('https://localhost:7216/api/post/Register', { // Proveri da li je putanja /api/post ili samo /post
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(registerData),
            });

            if (response.ok) {
                const data = await response.json();
                const token = data.token;

                if (token) {
                    localStorage.setItem("authToken", token);
                    alert("Registration successful!");
                    navigate('/home');
                } else {
                    alert("Registration successful, but no token received.");
                }
            } else {
                alert("Registration failed. Please try again.");
            }
        } catch (err) {
            console.error("Greška pri slanju:", err);
            alert("Server error.");
        }
    }; 

    return (
        <div style={containerStyle}>
            <div style={formWrapperStyle}>
                <h2 style={logoStyle}>TastyTrail</h2>
                <h3 style={subtitleStyle}>CREATE ACCOUNT</h3>

                <form onSubmit={handleSubmit} style={formStyle}>
                    <div>
                        <label style={labelStyle}>NAME</label>
                        <input type="text" name="name" value={formData.name} onChange={handleChange} placeholder="Enter your name" required style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>USERNAME</label>
                        <input type="text" name="username" value={formData.username} onChange={handleChange} placeholder="Enter your username" required style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>EMAIL</label>
                        <input type="email" name="email" value={formData.email} onChange={handleChange} placeholder="example@gmail.com" required style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>PASSWORD</label>
                        <input type="password" name="password" value={formData.password} onChange={handleChange} placeholder="Enter your password" required style={inputStyle} />
                    </div>

                    <button
                        type="submit"
                        style={buttonStyle}
                        onMouseOver={(e) => e.target.style.backgroundColor = '#ccc'}
                        onMouseOut={(e) => e.target.style.backgroundColor = '#fff'}
                    >
                        REGISTER
                    </button>
                </form>

                <p style={footerTextStyle}>
                    ALREADY A MEMBER? <span style={linkStyle} onClick={() => navigate('/login')}>LOGIN</span>
                </p>
            </div>
        </div>
    );
}

const containerStyle = { display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', backgroundColor: '#000', color: '#fff', fontFamily: 'sans-serif' };
const formWrapperStyle = { width: '100%', maxWidth: '400px', padding: '40px', border: '1px solid #222' };
const logoStyle = { textAlign: 'center', letterSpacing: '4px', textTransform: 'uppercase', marginBottom: '10px' };
const subtitleStyle = { textAlign: 'center', marginBottom: '40px', fontWeight: '300', color: '#666', fontSize: '12px', letterSpacing: '2px' };
const formStyle = { display: 'flex', flexDirection: 'column', gap: '25px' };
const labelStyle = { display: 'block', marginBottom: '8px', fontSize: '10px', color: '#888', letterSpacing: '1px' };
const inputStyle = { width: '100%', padding: '10px 0', border: 'none', borderBottom: '1px solid #333', backgroundColor: 'transparent', color: 'white', outline: 'none', fontSize: '14px' };
const buttonStyle = { padding: '12px', backgroundColor: '#fff', color: '#000', border: 'none', fontWeight: 'bold', fontSize: '13px', cursor: 'pointer', marginTop: '20px', letterSpacing: '1px', transition: '0.3s' };
const footerTextStyle = { textAlign: 'center', marginTop: '30px', fontSize: '11px', color: '#444', letterSpacing: '1px' };
const linkStyle = { color: '#fff', cursor: 'pointer', textDecoration: 'underline', marginLeft: '5px' };