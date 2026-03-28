import { Routes, Route, Navigate } from "react-router-dom"
import Navbar from "./pages/Navbar.jsx"
import Home from "./pages/Home.jsx"
import Explore from "./pages/Explore.jsx"
import Profile from "./pages/Profile.jsx"
import Login from "./pages/Login.jsx"
import Registration from "./pages/Registration.jsx"

const ProtectedRoute = ({ children }) => {
    const token = localStorage.getItem("authToken");

    if (!token) {
        return <Navigate to="/register" replace />;
    }

    return children;
};

function App() {
    return (
        <div style={{ display: "flex", flexDirection: "column", height: "100vh" }}>
            <Navbar />
            <Routes>
                <Route path="/home" element={<Home />} />
                <Route path="/explore" element={<Explore />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Registration />} />
                <Route
                    path="/profile/:id"
                    element={
                        <ProtectedRoute>
                            <Profile />
                        </ProtectedRoute>
                    }
                />

                <Route path="/" element={<Navigate to="/home" />} />
            </Routes>
        </div>
    )
}

export default App