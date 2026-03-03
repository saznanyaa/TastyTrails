import { Link } from "react-router-dom"
import { useState, useEffect } from "react";
import LoginDropdown from "./LoginDropdown";

const styles = {
  nav: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "10px 20px",
    backgroundColor: "#000",
    color: "white",
    position: "relative",
  },
  logo: { margin: 0 },
  link: { marginLeft: "15px", color: "white", textDecoration: "none", fontWeight: "bold" },
  loginDropdownWrapper: {
    position: "absolute",
    top: "60px",
    right: "20px",
    background: "#1a1a1a",
    padding: "20px",
    borderRadius: "8px",
    color: "white",
    boxShadow: "0 4px 8px rgba(0,0,0,0.2)",
    zIndex: 1000,
  },
};

export default function Navbar() {
  const [showLogin, setShowLogin] = useState(false);
  const [user, setUser] = useState(null); // track logged-in user

  // optional: load token/user from localStorage on mount
  useEffect(() => {
    const token = localStorage.getItem("authToken");
    if (token) {
      // you could decode JWT to get username here, for demo we'll just set a placeholder
      setUser({ name: "User" });
    }
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("authToken");
    setUser(null);
    setShowLogin(false);
  };

  const handleLoginSuccess = (token) => {
    console.log("Logged in with token:", token);
    //setIsLoggedIn(true);
  };

  return (
    <nav style={styles.nav}>
      <h1 style={styles.logo}>TastyTrail</h1>
      <div style={{ display: "flex", alignItems: "center" }}>
        <a href="/" style={styles.link}>Home</a>
        <a href="/explore" style={styles.link}>Explore</a>
        <a href="/profile" style={styles.link}>Profile</a>

        {!user && (
          <button onClick={() => setShowLogin(!showLogin)} style={{ marginLeft: "15px" }}>
            Login
          </button>
        )}

        {user && (
          <>
            <span style={{ marginLeft: "15px" }}>Welcome, {user.name}!</span>
            <button onClick={handleLogout} style={{ marginLeft: "10px" }}>Logout</button>
          </>
        )}
      </div>

      {!user && showLogin && (
        <div style={styles.loginDropdownWrapper}>
          <LoginDropdown onLoginSuccess={handleLoginSuccess} />
        </div>
      )}
    </nav>
  );
}