import { Routes, Route } from "react-router-dom"
import Navbar from "./pages/Navbar.jsx"
import Explore from "./pages/Explore.jsx"


function App() {
  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100vh" }}>
      <Navbar />
      <Routes>
        <Route path="/explore" element={<Explore />} />
      </Routes>
    </div>
  )
}

export default App