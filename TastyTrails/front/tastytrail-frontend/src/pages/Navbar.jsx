import { Link } from "react-router-dom";
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
        letterSpacing: "2px"
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
    const [user, setUser] = useState(null);

    useEffect(() => {
        const token = localStorage.getItem("authToken");
        const storedUserId = localStorage.getItem("userId");
        if (token && storedUserId) {
            setUser({ name: "David", id: storedUserId });
        }
    }, []);

    const handleLogout = () => {
        localStorage.removeItem("authToken");
        localStorage.removeItem("userId");
        setUser(null);
        window.location.href = "/home";
    };

    return (
        <nav style={styles.nav}>
            <Link to="/home" style={{ textDecoration: "none" }}>
                <h1 style={styles.logo}>TastyTrail</h1>
            </Link>

            <div style={{ display: "flex", alignItems: "center" }}>
                <Link to="/home" style={styles.link}>Home</Link>
                <Link to="/explore" style={styles.link}>Explore</Link>

                {user ? (
                    <Link to={`/profile/${user.id}`} style={styles.link}>Profile</Link>
                ) : (
                    <Link to="/register" style={styles.link}>Profile</Link>
                )}

                {!user && (
                    <Link to="/login">
                        <button
                            style={styles.button}
                            onMouseOver={(e) => e.target.style.backgroundColor = "#ccc"}
                            onMouseOut={(e) => e.target.style.backgroundColor = "white"}
                        >
                            Login
                        </button>
                    </Link>
                )}

                {user && (
                    <>
                        <button
                            onClick={handleLogout}
                            style={{ ...styles.button, backgroundColor: "transparent", color: "white", border: "1px solid white" }}
                            onMouseOver={(e) => { e.target.style.backgroundColor = "white"; e.target.style.color = "black"; }}
                            onMouseOut={(e) => { e.target.style.backgroundColor = "transparent"; e.target.style.color = "white"; }}
                        >
                            Logout
                        </button>
                    </>
                )}
            </div>
        </nav>
    );
}