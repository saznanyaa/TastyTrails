import { useNavigate } from "react-router-dom";

export default function Home() {
    const navigate = useNavigate();

    const cities = [
        { name: "Beograd", subtitle: "Capital city" },
        { name: "Nis", subtitle: "Southern Serbia" },
        { name: "Novi Sad", subtitle: "Northern Serbia" }
    ];

    return (
        <div
            style={{
                minHeight: "100vh",
                background: "#f7f7f7",
                display: "flex",
                justifyContent: "center",
                paddingTop: "140px",
                paddingLeft: "20px",
                paddingRight: "20px"
            }}
        >
            <div style={{ textAlign: "center" }}>
                {/* Header */}
                <div style={{ marginBottom: "28px" }}>
                    <h1 style={{ margin: 0, fontSize: "28px" }}>
                        Welcome to TastyTrail! 🍜
                    </h1>

                    <p
                        style={{
                            marginTop: "8px",
                            color: "#666",
                            fontSize: "15px"
                        }}
                    >
                        Select a city to explore food spots
                    </p>
                </div>

                {/* Cards */}
                <div
                    style={{
                        display: "flex",
                        gap: "16px",
                        justifyContent: "center",
                        flexWrap: "wrap"
                    }}
                >
                    {cities.map((city) => (
                        <div
                            key={city.name}
                            onClick={() => navigate(`/explore/${city.name}`)}
                            style={{
                                background: "white",
                                borderRadius: "14px",
                                boxShadow: "0 6px 18px rgba(0,0,0,0.12)",
                                padding: "18px 20px",
                                cursor: "pointer",
                                width: "200px",
                                textAlign: "left",
                                transition: "0.15s ease"
                            }}
                            onMouseEnter={(e) =>
                                (e.currentTarget.style.transform = "scale(1.04)")
                            }
                            onMouseLeave={(e) =>
                                (e.currentTarget.style.transform = "scale(1)")
                            }
                        >
                            <h3 style={{ margin: 0, fontSize: "18px" }}>
                                📌{city.name}
                            </h3>

                            <p
                                style={{
                                    margin: "8px 0 0",
                                    fontSize: "13px",
                                    color: "#666"
                                }}
                            >
                                {city.subtitle}
                            </p>

                            <div
                                style={{
                                    marginTop: "14px",
                                    fontSize: "13px",
                                    color: "#007bff",
                                    fontWeight: 500
                                }}
                            >
                                Explore →
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}