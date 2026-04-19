import { Link, useNavigate } from "react-router-dom";
import { useState, useEffect } from "react";

const styles = {
    nav: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        padding: "15px 30px",
        backgroundColor: "#000",
        color: "white",
        position: "relative",
        borderBottom: "1px solid #222"
    },
    logo: {
        margin: 0,
        color: "white",
        letterSpacing: "2px",
        textTransform: "uppercase"
    },
    link: {
        marginLeft: "20px",
        color: "white",
        textDecoration: "none",
        fontWeight: "bold",
        fontSize: "14px",
        textTransform: "uppercase"
    },
    button: {
        marginLeft: "20px",
        padding: "8px 16px",
        backgroundColor: "white",
        color: "black",
        border: "none",
        fontWeight: "bold",
        cursor: "pointer",
        textTransform: "uppercase",
        fontSize: "12px",
        transition: "0.3s"
    }
};

export default function Navbar() {
    const [userToken, setUserToken] = useState(null);
    const [userId, setUserId] = useState(null);
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem("authToken");
        const storedUserId = localStorage.getItem("userId");

        setUserToken(token);
        setUserId(storedUserId);
    }, []);

    const handleLogout = () => {
        localStorage.removeItem("authToken");
        localStorage.removeItem("userId");
        localStorage.removeItem("username");
        setUserToken(null);
        setUserId(null);
        navigate("/home");
        window.location.reload();
    };

    return (
        <nav style={styles.nav}>
            <Link to="/home" style={{ textDecoration: "none" }}>
                <h1 style={styles.logo}>TastyTrail</h1>
            </Link>

            <div style={{ display: "flex", alignItems: "center" }}>
                <Link to="/home" style={styles.link}>Home</Link>
                <Link to="/explore" style={styles.link}>Explore</Link>

                {userToken && userId ? (
                    <Link to={`/profile/${userId}`} style={styles.link}>Profile</Link>
                ) : (
                    <Link to="/register" style={styles.link}>Profile</Link>
                )}

                {!userToken ? (
                    <Link to="/login">
                        <button
                            style={styles.button}
                            onMouseOver={(e) => e.target.style.backgroundColor = "#ccc"}
                            onMouseOut={(e) => e.target.style.backgroundColor = "white"}
                        >
                            Login
                        </button>
                    </Link>
                ) : (
                    <button
                        onClick={handleLogout}
                        style={{ ...styles.button, backgroundColor: "transparent", color: "white", border: "1px solid white" }}
                        onMouseOver={(e) => { e.target.style.backgroundColor = "white"; e.target.style.color = "black"; }}
                        onMouseOut={(e) => { e.target.style.backgroundColor = "transparent"; e.target.style.color = "white"; }}
                    >
                        Logout
                    </button>
                )}
            </div>
        </nav>
    );
}