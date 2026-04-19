import { Routes, Route, Navigate, useLocation } from "react-router-dom"
import Navbar from "./pages/Navbar.jsx"
import Home from "./pages/Home.jsx"
import Explore from "./pages/Explore.jsx"
import Profile from "./pages/Profile.jsx"
import Login from "./pages/Login.jsx"
import Registration from "./pages/Registration.jsx"
import Restaurant from "./pages/Restaurant.jsx"

const ProtectedRoute = ({ children }) => {
    const token = localStorage.getItem("authToken");

    if (!token) {
        return <Navigate to="/register" replace />;
    }

    return children;
};

function App() {
    const location = useLocation();
    const state = location.state;

    return (
        <div style={{ display: "flex", flexDirection: "column", height: "100vh" }}>
            <Navbar />
            <Routes location={state?.backgroundLocation || location}>
                <Route path="/home" element={<Home />} />
                <Route path="/explore" element={<Home />} />
                <Route path="/explore/:city" element={<Explore />} />
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
                <Route path="/restaurant/:id" element={<Restaurant />} />

                <Route path="/" element={<Navigate to="/home" />} />
            </Routes>

            {state?.backgroundLocation && (
                <Routes>
                    <Route path="/restaurant/:id" element={<Restaurant modal />} />
                </Routes>
            )}
        </div>
    )
}

export default App