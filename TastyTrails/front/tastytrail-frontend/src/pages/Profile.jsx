import { useState, useEffect, useRef } from 'react'; // <--- DODALI SMO useRef

export default function Profile() {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    // OVO SMO DODALI: State za prikaz slike profilu (lokalno, pre slanja na server)
    const [profileImage, setProfileImage] = useState(null);

    // OVO SMO DODALI: Referenca ka sakrivenom file inputu
    const fileInputRef = useRef(null);

    const { id } = useParams(); // Ovde si ga nazvao 'id'

    useEffect(() => {
        fetch(`https://localhost:7216/api/get/user/${id}`)
            .then(res => res.json())
            .then(data => {
                setUser(data);
                if (data.profileImageUrl) {
                    setProfileImage(data.profileImageUrl);
                }
                setLoading(false);
            })
            .catch(err => console.error(err));
    }, [id]);

    // OVO SMO DODALI: Funkcija koja se poziva kada se izabere fajl
    const handleFileChange = (event) => {
        const file = event.target.files[0];
        if (file && file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onloadend = () => {
                // Prikazujemo sliku lokalno (kao Preview)
                setProfileImage(reader.result);

                // --- OVDE TREBA DA POŠALJEŠ SLIKU NA SERVER ---
                // uploadImageToServer(file); 
            };
            reader.readAsDataURL(file);
        } else {
            alert("Molimo izaberite ispravan format slike.");
        }
    };

    // OVO SMO DODALI: Funkcija koja se poziva na klik kruga
    const handleAvatarClick = () => {
        // Ovo simulira klik na sakriveni file input
        fileInputRef.current.click();
    };

    if (loading) return <div style={{ textAlign: 'center', marginTop: '50px' }}>Učitavanje...</div>;

    return (
        <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>

            {/* OVO SMO DODALI: Sakriveni input za fajlove */}
            <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileChange}
                accept="image/*"
                style={{ display: 'none' }}
            />

            <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'flex-start',
                marginBottom: '40px',
                borderBottom: '1px solid #eee',
                paddingBottom: '20px'
            }}>

                {/* IZMENJEN KRUG: Dodat onClick i prikaz slike */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                    <div
                        onClick={handleAvatarClick} // <--- KLIK NA KRUG
                        style={{
                            width: '80px', height: '80px', borderRadius: '50%',
                            backgroundColor: '#ddd', display: 'flex', alignItems: 'center',
                            justifyContent: 'center', fontSize: '30px',
                            cursor: 'pointer', // <--- POKAZIVAČ RUKA
                            overflow: 'hidden', // <--- DA SLIKA NE IZLAZI VAN
                            border: '2px solid transparent',
                            transition: 'border-color 0.2s'
                        }}
                        onMouseOver={(e) => e.currentTarget.style.borderColor = '#007bff'}
                        onMouseOut={(e) => e.currentTarget.style.borderColor = 'transparent'}
                        title="Klikni da promeniš sliku"
                    >
                        {/* Prikazujemo sliku ako postoji, inače ikonicu */}
                        {profileImage ? (
                            <img src={profileImage} alt="Profil" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                        ) : (
                            "👤"
                        )}
                    </div>
                    <div>
                        <h2 style={{ margin: 0 }}>{user.username}</h2>
                        <p style={{ color: '#666', margin: '5px 0 0' }}>{user.bio || "Nema biografije"}</p>
                    </div>
                </div>

                {/* ... ostatak Followers/Following dela ostaje isti ... */}
                <div style={{ display: 'flex', gap: '30px', cursor: 'pointer', flex: 1, justifyContent: 'center' }}>
                    <div style={{ textAlign: 'center' }} onClick={() => alert("Prikazujem pratioce...")}>
                        <strong style={{ fontSize: '20px', display: 'block' }}>{user.followersCount || 0}</strong>
                        <span style={{ color: '#888' }}>Followers</span>
                    </div>
                    <div style={{ textAlign: 'center' }} onClick={() => alert("Prikazujem koga pratiš...")}>
                        <strong style={{ fontSize: '20px', display: 'block' }}>{user.followingCount || 0}</strong>
                        <span style={{ color: '#888' }}>Following</span>
                    </div>
                </div>

                <div style={{ width: '100px' }}></div>
            </div>

            {/* ... ostatak liste restorana ostaje isti ... */}
            <div style={{ maxWidth: '800px', margin: '0 auto' }}>
                <h3 style={{ textAlign: 'center', marginBottom: '20px' }}>Moje recenzije</h3>

                {user.visitedRestaurants && user.visitedRestaurants.length > 0 ? (
                    user.visitedRestaurants.map((restoran, index) => (
                        <div
                            key={index}
                            onClick={() => window.location.href = `/explore?lat=${restoran.lat}&lng=${restoran.lng}`}
                            style={{
                                padding: '15px',
                                border: '1px solid #ddd',
                                borderRadius: '8px',
                                marginBottom: '10px',
                                cursor: 'pointer',
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                                transition: 'background 0.2s'
                            }}
                            onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#f9f9f9'}
                            onMouseOut={(e) => e.currentTarget.style.backgroundColor = '#fff'}
                        >
                            <div>
                                <h4 style={{ margin: '0 0 5px' }}>{restoran.name}</h4>
                                <p style={{ margin: 0, color: '#ffcc00' }}>{"★".repeat(restoran.rating)}</p>
                            </div>
                            <span style={{ color: '#007bff' }}>Pogledaj na mapi →</span>
                        </div>
                    ))
                ) : (
                    <p style={{ textAlign: 'center', color: '#999' }}>Još uvek niste ostavili nijednu recenziju.</p>
                )}
            </div>
        </div>
    );
}