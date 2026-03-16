import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import L from "leaflet";

// Fix for default marker icons in Leaflet
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
  //hardcoded restaurants for rn
  const restaurants = [
    { id: 1, name: "Pizza Place", lat: 43.3217, lng: 21.8958 },
    { id: 2, name: "Sushi Spot", lat: 43.3225, lng: 21.8965 },
    { id: 3, name: "Burger Joint", lat: 43.3232, lng: 21.8949 },
  ];

  // Handlers for buttons
  const handleSave = (restaurant) => {
    console.log(`Saved ${restaurant.name}`);
    // TODO: send to backend
  };

  const handleCheckIn = (restaurant) => {
    console.log(`Checked in at ${restaurant.name}`);
    // TODO: send to backend
  };

  return (
    <div style={{ flex: 1, display: "flex" }}>
      <MapContainer
        center={[43.3217, 21.8958]}
        zoom={15}
        style={{ flex: 1 }}
      >
        <TileLayer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        />
        {restaurants.map((r) => (
          <Marker key={r.id} position={[r.lat, r.lng]}>
            <Popup>
              <div>
                <strong>{r.name}</strong>
                <div style={{ marginTop: "5px" }}>
                  <button onClick={() => handleSave(r)} style={{ marginRight: "5px" }}>
                    Save
                  </button>
                  <button onClick={() => handleCheckIn(r)}>
                    Check-in
                  </button>
                </div>
              </div>
            </Popup>
          </Marker>
        ))}
      </MapContainer>
    </div>
  );
}