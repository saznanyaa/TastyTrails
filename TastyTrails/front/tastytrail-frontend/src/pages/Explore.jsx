import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { useEffect, useState } from "react";
import axios, { all } from "axios";
import L from "leaflet";
import { useNavigate, useLocation } from "react-router-dom";

export default function Explore() {
    const [restaurants, setRestaurants] = useState([]);
    const [reviews, setReviews] = useState([]);
    const [selectedRestaurant, setSelectedRestaurant] = useState(null);
    const [allRestaurants, setAllRestaurants] = useState([]);
    const [trending, setTrending] = useState([]);
    const [recommended, setRecommended] = useState([]);
    const [savedRestaurants, setSavedRestaurants] = useState([]);
    const [recentReviews, setRecentReviews] = useState([]);
    const [newReview, setNewReview] = useState({
        rating: 5,
        comment: ""
    });
    const [showReviewModal, setShowReviewModal] = useState(false);
    
    const userId = localStorage.getItem("userId");
    const token = localStorage.getItem("authToken");
    const isLoggedIn = !!userId;
    
    const navigate = useNavigate();
    const location = useLocation();

    const defaultIcon = L.icon({
        iconUrl:"/icons/restaurant.png",
        iconSize:[25,41],
    });
    const trendingIcon = new L.Icon({
        iconUrl: "/icons/star (1).png",
        iconSize: [30, 30],
    });
    const recommendedIcon = new L.Icon({
        iconUrl: "/icons/recommendation.png",
        iconSize: [35, 35],
    });

    const handleMarkerClick = async (restaurant) => {
        setSelectedRestaurant(restaurant);

        try {
            const [reviewRes, recentRes] = await Promise.all([
                axios.get(`http://localhost:5146/api/get/restaurants/${restaurant.id}/mngReviews`),
                axios.get(`http://localhost:5146/api/get/${restaurant.id}/reviews/recent`)
                ]);
             
            
            setReviews(reviewRes.data);
            setRecentReviews(recentRes.data);

            console.log("REVIEWS:", reviewRes.data);
            console.log("RECENT REVIEWS:", recentRes.data);

            await axios.post(`http://localhost:5146/api/post/${restaurant.id}/view`);
        } catch (err) {
            console.error(err);
        }
    };

    const handleAddReview = async (restaurantId) => {
        try{
            await axios.post(
                `http://localhost:5146/api/user/${userId}/review/${restaurantId}`,
                { rating: newReview.rating,
                  comment: newReview.comment
                },
                { headers: { Authorization: `Bearer ${token}` } }
            );
            setShowReviewModal(false);
            setNewReview({ rating: 5, comment: "" });
            const res = await axios.get(`http://localhost:5146/api/get/${restaurant.id}/reviews/recent`);
            setRecentReviews(res.data);
        }
        catch(err) {
            console.error(err);
        }
    }

    const getIcon = (type) => {
        switch(type) {
            case "trending": return trendingIcon;
            case "recommended": return recommendedIcon;
            default: return defaultIcon;
        }
    };

    const isSaved = (restaurantId) => {
        return savedRestaurants.includes(restaurantId);
    };

    const toggleSave = async (restaurantId) => {
        try {
            if (isSaved(restaurantId)) {
            await axios.delete(
                `http://localhost:5146/api/user/${userId}/saved/${restaurantId}`,
                { headers: { Authorization: `Bearer ${token}` } }
            );

            setSavedRestaurants(prev =>
                prev.filter(id => id !== restaurantId)
            );
            } else {
            await axios.post(
                `http://localhost:5146/api/user/${userId}/saved/${restaurantId}`,
                {},
                { headers: { Authorization: `Bearer ${token}` } }
            );

            setSavedRestaurants(prev => [...prev, restaurantId]);
            }
        } catch (err) {
            console.error(err);
        }
    };

    useEffect(() => {
        if (showReviewModal) {
            setNewReview({ rating: 5, comment: "" });
        }
    }, [showReviewModal]);


    useEffect(() => {
        const fetchData = async () => {
            try {
                const [allRes, trendingRes] = await Promise.all([
                    axios.get("http://localhost:5146/api/get/mongoRestaurants"),
                    axios.get("http://localhost:5146/api/get/restaurants/trending/Beograd")
                ]);

                let recommendedData = [];
                let savedIds = [];

                if (userId && token) {
                    try {
                        const [savedRes, recommendedRes] = await Promise.all([
                            axios.get(
                                `http://localhost:5146/api/user/${userId}/saved`,
                                { headers: { Authorization: `Bearer ${token}` } }
                            ),
                            axios.get(
                                `http://localhost:5146/api/user/${userId}/recommendations/Beograd`,
                                { headers: { Authorization: `Bearer ${token}` } }
                            )
                        ]);

                        savedIds = savedRes.data.map(r => r.id);
                        recommendedData = recommendedRes.data;

                    } catch (err) {
                        console.error("Auth fetch failed:", err.response || err.message);
                    }
                }

                const restaurantMap = new Map();

                allRes.data.forEach(r =>
                    restaurantMap.set(r.id, { ...r, type: "default" })
                );

                trendingRes.data.forEach(r =>
                    restaurantMap.set(r.id, { ...r, type: "trending" })
                );

                recommendedData.forEach(r =>
                    restaurantMap.set(r.id, { ...r, type: "recommended" })
                );

                setAllRestaurants(allRes.data);
                setTrending(trendingRes.data);
                setRecommended(recommendedData);
                setSavedRestaurants(savedIds);
                setRestaurants(Array.from(restaurantMap.values()));

            } catch (err) {
                console.error("Error fetching restaurants:", err.response || err.message);
            }
        };

        fetchData();
    }, []);

    console.log("ALL:", allRestaurants.length);
    console.log("TRENDING:", trending.length);
    console.log("RECOMMENDED:", recommended.length);
    console.log("SAVED:", savedRestaurants.length);
  
    return (
    <div style={{ flex: 1, display: "flex" }}>
      <MapContainer
        center={[44.7866, 20.4489]}
        zoom={15}
        style={{ flex: 1 }}
      >
        <TileLayer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {restaurants.map((r) => (
        <Marker
            key={r.id}
            position={[r.latitude, r.longitude]}
            icon={getIcon(r.type)}
            eventHandlers={{
                click: () => handleMarkerClick(r),
            }}
        >
            <Popup>
                <div
                    style={{
                    position: "relative",
                    minWidth: "240px",
                    maxWidth: "260px",
                    maxHeight: "260px",
                    overflowY: "auto",
                    background: "white",
                    borderRadius: "12px",
                    boxShadow: "0 4px 15px rgba(0,0,0,0.12)",
                    padding: "12px"
                    }}
                >
                    {/* Save button */}
                    <button
                    onClick={() => toggleSave(r.id)}
                    style={{
                        position: "absolute",
                        top: "10px",
                        right: "10px",
                        background: "#f5f5f5",
                        border: "none",
                        borderRadius: "50%",
                        width: "32px",
                        height: "32px",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        cursor: "pointer"
                    }}
                    >
                    <img
                        src={isSaved(r.id) ? "/icons/bookmark (1).png" : "/icons/bookmark.png"}
                        alt="save"
                        style={{ width: "16px", height: "16px" }}
                    />
                    </button>

                    {/* Header */}
                    <div style={{ marginBottom: "8px" }}>
                    <h4 style={{ margin: 0, fontSize: "16px" }}>{r.name}</h4>
                    <p style={{ margin: "4px 0", fontSize: "13px", color: "#666" }}>
                        ⭐ {(r.averageRating ?? 0).toFixed(1)} ({r.totalReviews ?? 0})
                    </p>
                    </div>

                    {/* View details button */}
                    <div
                    onClick={() =>
                        navigate(`/restaurant/${r.id}`, {
                        state: { backgroundLocation: location }
                        })
                    }
                    style={{
                        fontSize: "13px",
                        color: "#007bff",
                        cursor: "pointer",
                        marginBottom: "10px"
                    }}
                    >
                    View details →
                    </div>

                    <div style={{ height: "1px", background: "#eee", margin: "8px 0" }} />

                    <p style={{ fontWeight: "600", margin: 0, fontSize: "13px" }}>
                    Recent reviews
                    </p>
                    <p style={{ fontSize: "11px", color: "#888", marginBottom: "8px" }}>
                    last three days
                    </p>

                    {selectedRestaurant?.id === r.id ? (
      <>
        {recentReviews.length > 0 ? (
          recentReviews.map((rev, idx) => (
            <div
              key={idx}
              style={{
                display: "flex",
                gap: "8px",
                marginBottom: "8px"
              }}
            >
              <img
                src={rev.profilePicture || "/icons/default-avatar.png"}
                style={{
                  width: "28px",
                  height: "28px",
                  borderRadius: "50%"
                }}
              />

              <div style={{ fontSize: "12px" }}>
                <strong>{rev.username || "User"}</strong>
                <div style={{ color: "#ff7a00" }}>{rev.rating}⭐</div>
                <div style={{ color: "#666" }}>{rev.comment}</div>
              </div>
            </div>
          ))
        ) : (
          <p style={{ fontSize: "12px", color: "#888" }}>
            No recent reviews.
          </p>
        )}

        {isLoggedIn && (
          <div
            onClick={() => setShowReviewModal(true)}
            style={{
              marginTop: "6px",
              padding: "6px",
              borderRadius: "8px",
              background: "#f5f5f5",
              textAlign: "center",
              fontSize: "12px",
              cursor: "pointer"
            }}
          >
            ✍️ Write a review
          </div>
        )}
      </>
    ) : (
      <p style={{ fontSize: "12px", color: "#888" }}>
        Click marker to load activity...
      </p>
    )}
                </div>
                </Popup>
        </Marker>
        ))}

      </MapContainer>
      {showReviewModal && selectedRestaurant && (
        <div
            style={{
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
            }}
        >
            <div
                style={{
                    background: "white",
                    padding: "20px",
                    borderRadius: "10px",
                    width: "300px"
                }}
            >
                <h3>Review {selectedRestaurant.name}</h3>

                <select
                    value={newReview.rating}
                    onChange={(e) =>
                        setNewReview({ ...newReview, rating: Number(e.target.value) })
                    }
                >
                    {[1,2,3,4,5].map(n => (
                        <option key={n} value={n}>{n}⭐</option>
                    ))}
                </select>

                <textarea
                    placeholder="Write your review..."
                    value={newReview.comment}
                    onChange={(e) =>
                        setNewReview({ ...newReview, comment: e.target.value })
                    }
                    style={{ width: "100%", marginTop: "10px" }}
                />

                <div style={{ display: "flex", justifyContent: "right", marginTop: "10px" }}>
                    <button
                        onClick={() => setShowReviewModal(false)}
                        style={{
                            background: "transparent",
                            border: "none",
                            cursor: "pointer"
                        }}
                        title="Cancel"
                    >
                        <img
                            src="/icons/close.png"
                            alt="cancel"
                            style={{ width: "22px", height: "22px" }}
                        />
                    </button>

                    <button
                        onClick={() => handleAddReview(selectedRestaurant.id)}
                        style={{
                            background: "transparent",
                            border: "none",
                            cursor: "pointer"
                        }}
                        title="Submit review"
                    >
                        <img
                            src="/icons/check.png"
                            alt="submit"
                            style={{ width: "25px", height: "25px" }}
                        />
                    </button>

                </div>
            </div>
        </div>
    )}
    </div>
  );
}