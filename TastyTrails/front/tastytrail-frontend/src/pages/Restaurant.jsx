import { useParams, useNavigate } from "react-router-dom";
import RestaurantContent from "./RestaurantContent";

export default function Restaurant({ modal }) {
    const navigate = useNavigate();

    const overlayStyle = {
        position: "fixed",
        top: 0,
        left: 0,
        width: "100vw",
        height: "100vh",
        background: "rgba(0,0,0,0.5)",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        zIndex: 9999
    };

    const modalStyle = {
        background: "white",
        width: "80%",
        height: "90%",
        borderRadius: "12px",
        padding: "20px",
        overflowY: "auto"
    };

    if (modal) {
        return (
            <div style={overlayStyle}>
                <div style={modalStyle}>
                    <button onClick={() => navigate(-1)}>✖</button>
                    <RestaurantContent />
                </div>
            </div>
        );
    }

    return (
        <div style={{ padding: "20px" }}>
            <RestaurantContent />
        </div>
    );
}