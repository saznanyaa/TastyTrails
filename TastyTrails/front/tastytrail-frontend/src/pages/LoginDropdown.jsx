import { useState } from "react";
import axios from "axios";

export default function LoginDropdown({onLoginSuccess}) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    try{
        const response = await axios.post(`http://localhost:5146/api/auth/login`, { email, password });
        const { token } = response.data;
        localStorage.setItem("token", token);
        if(onLoginSuccess) onLoginSuccess(token);
        console.log("Login successful, token:", token);
    }
    catch(err){
        console.error("Login failed:", err);
        setError("Invalid email or password");
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
      <input 
        type="email" 
        placeholder="Email" 
        value={email} 
        onChange={(e) => setEmail(e.target.value)} 
        style={{ padding: "8px", borderRadius: "4px", border: "1px solid #555", background: "#222", color: "white" }}
      />
      <input 
        type="password" 
        placeholder="Password" 
        value={password} 
        onChange={(e) => setPassword(e.target.value)} 
        style={{ padding: "8px", borderRadius: "4px", border: "1px solid #555", background: "#222", color: "white" }}
      />
      <button type="submit" style={{ padding: "8px", borderRadius: "4px", background: "#646cff", color: "white", fontWeight: "bold" }}>
        Login
      </button>
    </form>
  );
}