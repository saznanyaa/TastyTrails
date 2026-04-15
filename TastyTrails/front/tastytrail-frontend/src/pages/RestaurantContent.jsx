import { useParams, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import axios from "axios";
import { LineChart, Line, XAxis, YAxis, Tooltip, CartesianGrid, ResponsiveContainer } from "recharts";

export default function RestaurantContent() {
    const { id } = useParams();
    const navigate = useNavigate();

    const [restaurant, setRestaurant] = useState(null);
    const [analytics, setAnalytics] = useState(null);
    const [reviews, setReviews] = useState([]);
    const [recentReviews, setRecentReviews] = useState([]);
    const [recommendations, setRecommendations] = useState([]);

    const fillMissingDays = (data) => {
        const result = [];
        const today = new Date();

        for (let i = 6; i >= 0; i--) {
            const d = new Date();
            d.setDate(today.getDate() - i);

            const dateStr = d.toISOString().split("T")[0];

            const found = data.find(x => x.date === dateStr);

            result.push({
                date: d.toLocaleDateString("en-GB", { weekday: "short" }),
                views: found ? found.count : 0
            });
        }

        return result;
    };

    const chartData = Array.isArray(analytics?.viewsByDay) ? fillMissingDays(analytics.viewsByDay) : [];

    useEffect(() => {
        const safeArray = (data) => Array.isArray(data) ? data : [];
        const fetchData = async () => {
            try {
                const [restRes, analyticsRes, reviewsRes, recentRes] =
                    await Promise.all([
                        axios.get(`http://localhost:5146/api/get/mongoRest/${id}`),
                        axios.get(`http://localhost:5146/api/get/${id}/analytics/weekly`),
                        axios.get(`http://localhost:5146/api/get/restaurants/${id}/mngReviews`),
                        axios.get(`http://localhost:5146/api/get/${id}/reviews/recent`),
                    ]);

                const recRes = await axios.get(`http://localhost:5146/api/get/similar/${id}`);

                setRestaurant(restRes.data);
                setAnalytics(analyticsRes.data);
                setReviews(safeArray(reviewsRes.data));
                setRecentReviews(safeArray(recentRes.data));
                setRecommendations(recRes.data);
            } catch (err) {
                console.error(err);
            }
        };

        fetchData();
    }, [id]);

    console.log("IS ARRAY:", Array.isArray(recentReviews));
    console.log("RECENT:", recentReviews);
    console.log("ALL:", reviews);
    console.log("ANALYTICS:", analytics);
    console.log("RECOMMENDATIONS:", recommendations);

    if (!restaurant || !analytics) return <p>Loading...</p>;

    return (
        <div style={{ padding: "20px", maxWidth: "1100px", margin: "0 auto" }}>
            <div style={{
                background: "white",
                padding: "20px",
                borderRadius: "12px",
                boxShadow: "0 2px 10px rgba(0,0,0,0.08)",
                marginBottom: "20px"
            }}>
                <h2 style={{ margin: 0 }}>{restaurant.name}</h2>

                <p style={{ fontSize: "16px", marginTop: "10px" }}>
                    ⭐ {analytics?.averageRating?.toFixed(1)} ({analytics?.reviewsCount} reviews)
                </p>
            </div>
                
            <div style={{
                display: "grid",
                gridTemplateColumns: "repeat(4, 1fr)",
                gap: "15px",
                marginBottom: "20px"
            }}>
                {[
                    { label: "Views", value: analytics?.viewsCount, color: "#ff7a00" },
                    { label: "Check-ins", value: analytics?.checkinsCount, color: "#00a86b" },
                    { label: "Reviews", value: analytics?.reviewsCount, color: "#007bff" },
                    { label: "Rating", value: analytics?.averageRating?.toFixed(1), color: "#ff3d71" }
                ].map((item, i) => (
                    <div key={i} style={{
                        background: "white",
                        padding: "15px",
                        borderRadius: "12px",
                        boxShadow: "0 2px 8px rgba(0,0,0,0.06)"
                    }}>
                        <p style={{ margin: 0, color: "#666" }}>{item.label}</p>
                        <h3 style={{ margin: "5px 0", color: item.color }}>
                            {item.value}
                        </h3>
                    </div>
                ))}
            </div>    

            <div style={{
                background: "white",
                padding: "20px",
                borderRadius: "12px",
                boxShadow: "0 2px 10px rgba(0,0,0,0.08)",
                marginBottom: "20px"
            }}>
                <h3>📈 Weekly Popularity</h3>

                {chartData.length > 0 ? (
                    <div style={{ width: "100%", height: 300 }}>
                        <ResponsiveContainer>
                            <LineChart data={chartData}>
                                <CartesianGrid strokeDasharray="3 3" />
                                <XAxis dataKey="date" />
                                <YAxis />
                                <Tooltip />
                                <Line
                                    type="monotone"
                                    dataKey="views"
                                    stroke="#ff7a00"
                                    strokeWidth={3}
                                />
                            </LineChart>
                        </ResponsiveContainer>
                    </div>
                ) : (
                    <p>No data available</p>
                )}
            </div>

            <div style={{
                display: "grid",
                gridTemplateColumns: "2fr 1fr",
                gap: "20px"
            }}>
                <div style={{
                    background: "white",
                    padding: "20px",
                    borderRadius: "12px",
                    boxShadow: "0 2px 10px rgba(0,0,0,0.08)"
                }}>
                    <h3>📋 Reviews</h3>

                    {reviews.length > 0 ? reviews.map((r, i) => (
                        <div key={i} style={{ marginBottom: "10px" }}>
                            <strong>{r.username}</strong>
                            <div>⭐ {r.rating}</div>
                            <p>{r.comment}</p>
                            <hr />
                        </div>
                    )) : <p>No reviews yet</p>}
                </div>
                <div style={{
                    background: "white",
                    padding: "20px",
                    borderRadius: "12px",
                    boxShadow: "0 2px 10px rgba(0,0,0,0.08)",
                    height: "fit-content",
                    position: "sticky",
                    top: "20px"
                }}>
                    <h3>🧠 Recommended for You</h3>

                    {recommendations.length > 0 ? (
                        recommendations.map((r) => (
                            <div key={r.id}
                                onClick={() => navigate(`/restaurant/${r.id}`)} 
                                style={{
                                padding: "12px",
                                borderBottom: "1px solid #eee",
                                cursor: "pointer"
                            }}>
                                <strong>{r.name}</strong>

                                <p style={{
                                    margin: "5px 0",
                                    fontSize: "12px",
                                    color: "#666"
                                }}>
                                    ⭐ {r.averageRating?.toFixed(1)} ({r.totalReviews} reviews)
                                </p>

                                <p style={{
                                    margin: 0,
                                    fontSize: "12px",
                                    color: "#999"
                                }}>
                                    🧠 Based on similar user preferences
                                </p>
                            </div>
                        ))
                    ) : (
                        <p>No recommendations yet</p>
                    )}
                </div>
            </div>    
        </div>
    );
}