import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

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
                console.log("Šta je stiglo sa servera:", data); // Proveri ovo u F12 konzoli!

                // Proveravamo sve moguće varijante ključa
                const receivedId = data.userId || data.UserId || data.id || data.Id;
                const receivedToken = data.token || data.Token;

                if (receivedId) {
                    localStorage.setItem("authToken", receivedToken);
                    localStorage.setItem("userId", receivedId);

                    alert("Welcome back!");
                    navigate(`/profile/${receivedId}`);
                    window.location.reload();
                } else {
                    // Ako uđe ovde, pogledaj u F12 konzolu šta piše u "Šta je stiglo sa servera"
                    console.error("ID nije pronađen u objektu:", data);
                    alert("Server nije poslao ID korisnika. Pogledaj konzolu!");
                }
            }
            else {
                alert("Invalid username or password.");
            }
        } catch (err) {
            console.error(err);
            alert("Server error.");
        }
    };

    return (
        <div style={containerStyle}>
            <div style={formWrapperStyle}>
                <h2 style={logoStyle}>TastyTrail</h2>
                <h3 style={subtitleStyle}>MEMBER LOGIN</h3>

                <form onSubmit={handleSubmit} style={formStyle}>
                    <div>
                        <label style={labelStyle}>EMAIL</label>
                        <input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            placeholder="Enter your email"
                            required
                            style={inputStyle}
                        />
                    </div>

                    <div>
                        <label style={labelStyle}>PASSWORD</label>
                        <input
                            type="password"
                            name="password"
                            value={formData.password}
                            onChange={handleChange}
                            placeholder="Enter your password"
                            required
                            style={inputStyle}
                        />
                    </div>

                    <button
                        type="submit"
                        style={buttonStyle}
                        onMouseOver={(e) => e.target.style.backgroundColor = '#ccc'}
                        onMouseOut={(e) => e.target.style.backgroundColor = '#fff'}
                    >
                        SIGN IN
                    </button>
                </form>

                <p style={footerTextStyle}>
                    NOT A MEMBER? <span style={linkStyle} onClick={() => navigate('/register')}>REGISTER</span>
                </p>
            </div>
        </div>
    );
}

// STYLES (Isti kao za Registration za savršen sklad)
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