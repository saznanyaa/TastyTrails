import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { useEffect, useState } from "react";
import axios from "axios";

export default function Explore() {
    const [restaurants, setRestaurants] = useState([]);
    const [reviews, setReviews] = useState([]);
    const [selectedRestaurant, setSelectedRestaurant] = useState(null);

    const handleMarkerClick = async (restaurant) => {
        setSelectedRestaurant(restaurant);

        try {
            const res = await axios.get(
            `http://localhost:5146/api/get/restaurants/${restaurant.id}/mngReviews`
            );
            setReviews(res.data);
        } catch (err) {
            console.error(err);
        }
        };

    useEffect(() => {
        axios.get(`http://localhost:5146/api/get/restaurants/trending/Beograd`)
        .then((response) => {
            console.log("Fetched restaurants:", response.data);
            setRestaurants(response.data);
        })
        .catch((error) => console.error("Error fetching restaurants:", error));
    }, []);
  
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
        eventHandlers={{
            click: () => handleMarkerClick(r),
        }}
        >
            <Popup>
            <div style={{ minWidth: "200px", maxHeight: "200px", overflowY: "auto" }}>
                <h4 style={{ margin: 0 }}>{r.name}</h4>
                <p style={{ margin: "5px 0" }}>
                ⭐ {r.averageRating.toFixed(1)} ({r.reviewCount})
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