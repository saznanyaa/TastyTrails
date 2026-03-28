import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { useState, useEffect } from "react";

// Fix za ikonice
import markerIcon2x from "leaflet/dist/images/marker-icon-2x.png";
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";

delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: markerIcon2x,
    iconUrl: markerIcon,
    shadowUrl: markerShadow,
});

export default function Explore() {
    const [restaurants, setRestaurants] = useState([]);
    const [loading, setLoading] = useState(true);

    // Koordinate centra Niša (kao u tvom hardkodu)
    const NIS_LAT = 43.3217;
    const NIS_LNG = 21.8958;

    useEffect(() => {
        const fetchRestaurants = async () => {
            try {
                // Pozivamo tvoj endpoint iz GetController-a
                // Koristimo 'nearme' sa radijusom da pokrijemo grad
                const response = await fetch(
                    `https://localhost:7216/api/get/restaurants/nearme?lat=${NIS_LAT}&lng=${NIS_LNG}&radius=0.05`
                );

                if (response.ok) {
                    const data = await response.json();
                    setRestaurants(data);
                }
            } catch (err) {
                console.error("Greška pri učitavanju restorana:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchRestaurants();
    }, []);

    const handleSave = (restaurant) => {
        // Ovde bi mogao da otvoriš onaj tvoj Modal sa Profile stranice
        // Ili da direktno pošalješ POST ako već imaš ocenu
        alert(`Restoran ${restaurant.name} izabran! Idi na profil da ostaviš recenziju.`);
    };

    if (loading) return <div style={{ padding: "20px", color: "white", background: "black" }}>Učitavanje mape...</div>;

    return (
        <div style={{ flex: 1, display: "flex", height: "100vh" }}>
            <MapContainer
                center={[NIS_LAT, NIS_LNG]}
                zoom={14}
                style={{ flex: 1 }}
            >
                <TileLayer
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                    attribution='&copy; OpenStreetMap'
                />

                {restaurants.map((r) => (
                    <Marker key={r.id} position={[r.latitude, r.longitude]}>
                        <Popup>
                            <div className="map-popup">
                                <h3>{r.name}</h3>
                                <p><strong>Kuhinja:</strong> {r.cuisine}</p>
                                <p><strong>Prosečna ocena:</strong> ⭐ {r.averageRating || "Nema ocena"}</p>
                                <hr />
                                {/* Ovde možeš dodati informaciju ko je ostavio recenziju ako tvoj API to šalje */}
                                <p><small>Poslednju recenziju ostavio: {r.lastUser || "Anonimno"}</small></p>
                            </div>
                        </Popup>
                    </Marker>
                ))}
            </MapContainer>
        </div>
    );
}