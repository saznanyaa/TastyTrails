import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { useEffect, useState } from "react";
import axios, { all } from "axios";
import L from "leaflet";

export default function Explore() {
    const [restaurants, setRestaurants] = useState([]);
    const [reviews, setReviews] = useState([]);
    const [selectedRestaurant, setSelectedRestaurant] = useState(null);
    const [allRestaurants, setAllRestaurants] = useState([]);
    const [trending, setTrending] = useState([]);
    const [recommended, setRecommended] = useState([]);
    const [savedRestaurants, setSavedRestaurants] = useState([]);

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
            const res = await axios.get(
            `http://localhost:5146/api/get/restaurants/${restaurant.id}/mngReviews`
            );
            setReviews(res.data);

            await axios.post(`http://localhost:5146/api/post/${restaurant.id}/view`);
        } catch (err) {
            console.error(err);
        }
    };

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
        const userId = localStorage.getItem("userId");
        const token = localStorage.getItem("authToken");

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
        const userId = localStorage.getItem("userId");
        const token = localStorage.getItem("authToken");

        const fetchData = async () => {
            try {
                const allPromise = axios.get("http://localhost:5146/api/get/mongoRestaurants");
                const trendingPromise = axios.get("http://localhost:5146/api/get/restaurants/trending/Beograd");
                const savedPromise = axios.get(
                    `http://localhost:5146/api/user/${userId}/saved`,
                    { headers: { Authorization: `Bearer ${token}` } 
                });

                let recommendedPromise = null;

                if (userId && token) {
                    recommendedPromise = axios.get(
                        `http://localhost:5146/api/user/${userId}/recommendations/Beograd`,
                        { headers: { Authorization: `Bearer ${token}` } }
                    );
                }

                const [allRes, trendingRes, recommendedRes, savedRes] = await Promise.all([
                    allPromise,
                    trendingPromise,
                    recommendedPromise,
                    savedPromise
                ]);

                const restaurantMap = new Map();

                allRes.data.forEach(r => restaurantMap.set(r.id, { ...r, type: "default" }));
                trendingRes.data.forEach(r => restaurantMap.set(r.id, { ...r, type: "trending" }));

                if (recommendedRes) {
                    recommendedRes.data.forEach(r =>
                        restaurantMap.set(r.id, { ...r, type: "recommended" })
                    );
                    setRecommended(recommendedRes.data);
                }

                setAllRestaurants(allRes.data);
                setTrending(trendingRes.data);
                setRestaurants(Array.from(restaurantMap.values()));
                setSavedRestaurants(savedRes.data.map(r => r.id));

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
            <div style={{ position: "relative", minWidth: "200px", maxHeight: "200px", overflowY: "auto" }}>
                <button
                onClick={() => toggleSave(r.id)}
                style={{
                    position: "absolute",
                    top: "5px",
                    right: "5px",
                    background: "transparent",
                    border: "none",
                    cursor: "pointer",
                    fontSize: "18px"
                }}
                >
                <img
                src={isSaved(r.id) ? "/icons/bookmark (1).png" : "/icons/bookmark.png"}
                alt="save"
                style={{width: "20px", height: "20px"}}
                />
                </button>
                <h4 style={{ margin: 0 }}>{r.name}</h4>
                <p style={{ margin: "5px 0" }}>
                ⭐ {(r.averageRating ?? 0).toFixed(1)} ({r.totalReviews ?? 0})
                </p>

                <hr />

                {selectedRestaurant?.id === r.id ? (
                reviews.length > 0 ? (
                    reviews.map((rev) => (
                    <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                        <img
                            src={rev.profilePicture || "/icons/default-avatar.png"}
                            style={{
                            width: "30px",
                            height: "30px",
                            borderRadius: "50%"
                            }}
                        />

                    <div>
                        <strong>{rev.name}</strong>
                        <div>{rev.rating}⭐</div>
                        <div>{rev.comment}</div>
                    </div>
                    </div>
                    ))
                ) : (
                    <p>No reviews yet.</p>
                )) : (
                <p>Click marker to load reviews...</p>
                )}
            </div>
            </Popup>
        </Marker>
        ))}

      </MapContainer>
    </div>
  );
}