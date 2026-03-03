import { Link } from "react-router-dom"

export default function Navbar() {
  return (
    <nav style={styles.nav}>
      <h1 style={styles.logo}>TastyTrails 🍜</h1>
      <div>
        <Link to="/" style={styles.link}>Home</Link>
        <Link to="/explore" style={styles.link}>Explore</Link>
        <Link to="/profile" style={styles.link}>Profile</Link>
        <Link to="/login" style={styles.link}>Login</Link>
      </div>
    </nav>
  )
}

const styles = {
  nav: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "10px 20px",
    backgroundColor: "#000",
    color: "white",
  },
  logo: { margin: 0 },
  link: { marginLeft: "15px", color: "white", textDecoration: "none", fontWeight: "bold" },
};