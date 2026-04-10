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

    useEffect(() => {
        const userId = localStorage.getItem("userId");
        const token = localStorage.getItem("authToken");

        const fetchData = async () => {
            try {
                const allPromise = axios.get("http://localhost:5146/api/get/mongoRestaurants");
                const trendingPromise = axios.get("http://localhost:5146/api/get/restaurants/trending/Beograd");

                let recommendedPromise = null;

                if (userId && token) {
                    recommendedPromise = axios.get(
                        `http://localhost:5146/api/user/${userId}/recommendations/Beograd`,
                        { headers: { Authorization: `Bearer ${token}` } }
                    );
                }

                const [allRes, trendingRes, recommendedRes] = await Promise.all([
                    allPromise,
                    trendingPromise,
                    recommendedPromise
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

            } catch (err) {
                console.error("Error fetching restaurants:", err.response || err.message);
            }
        };

        fetchData();
    }, []);

    console.log("ALL:", allRestaurants.length);
    console.log("TRENDING:", trending.length);
    console.log("RECOMMENDED:", recommended.length);
  
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
            <div style={{ minWidth: "200px", maxHeight: "200px", overflowY: "auto" }}>
                <h4 style={{ margin: 0 }}>{r.name}</h4>
                <p style={{ margin: "5px 0" }}>
                ⭐ {(r.averageRating ?? 0).toFixed(1)} ({r.reviewCount ?? 0})
                </p>

                <hr />

                {selectedRestaurant?.id === r.id ? (
                reviews.length > 0 ? (
                    reviews.map((rev) => (
                    <div key={rev.id} style={{ marginBottom: "8px" }}>
                        <strong>{rev.rating}⭐</strong>
                        <br />
                        <span>{rev.comment}</span>
                    </div>
                    ))
                ) : (
                    <p>No reviews yet</p>
                )
                ) : (
                <p>Click to load reviews...</p>
                )}
            </div>
            </Popup>
        </Marker>
        ))}

      </MapContainer>
    </div>
  );
}