import { Routes, Route } from "react-router-dom"
import Navbar from "./pages/Navbar.jsx"
import Home from "./pages/Home.jsx"
import Explore from "./pages/Explore.jsx"
import Profile from "./pages/Profile.jsx"
import Login from "./pages/Login.jsx"
import Registration from "./pages/Registration.jsx"

function App() {
  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100vh" }}>
      <Navbar />
          <Routes>
              <Route path="/home" element={<Home />} />
              <Route path="/explore" element={<Explore />} />
              <Route path="/profile/:id" element={<Profile />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Registration />} />
      </Routes>
    </div>
  )
}

export default App