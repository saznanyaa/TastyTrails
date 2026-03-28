import { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import '../Profile.css';

export default function Profile() {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [profileImage, setProfileImage] = useState(null);
    const [isModalOpen, setIsModalOpen] = useState(false);

    // Lista recenzija (inicijalno prazna, puni se sa backend-a ili nakon Save)
    const [reviews, setReviews] = useState([]);

    const [newReview, setNewReview] = useState({
        name: '',
        location: '',
        cuisine: '',
        rating: '',
        comment: ''
    });

    const fileInputRef = useRef(null);
    const { id } = useParams();

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            const token = localStorage.getItem("token");

            try {
                // 1. Prvo uzimamo podatke o korisniku
                const userRes = await fetch('https://localhost:7216/api/get/user/${id}', {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (userRes.ok) {
                    const userData = await userRes.json();
                    setUser(userData);
                }

                // 2. OVO JE KLJUČNO: Uzimamo njegove recenzije iz Monga
                console.log("Provera ID-a:", id); // Da vidimo da li id uopšte postoji
                const url = `https://localhost:7216/api/get/reviews/${id}/mongouser/reviews`;
                console.log("Finalni URL koji šaljem:", url);
                const reviewsRes = await fetch(url, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });
                if (reviewsRes.ok) {
                    const reviewsData = await reviewsRes.json();
                    // Mapiramo podatke ako se imena polja razlikuju (npr. Comment -> comment)
                    setReviews(reviewsData);
                }

            } catch (err) {
                console.error("Greška pri učitavanju:", err);
            } finally {
                setLoading(false);
            }
        };

        if (id) fetchData();
    }, [id]);

    const handleRatingChange = (e) => {
        let val = e.target.value;

        // Ako je polje prazno, dozvoli brisanje
        if (val === '') {
            setNewReview({ ...newReview, rating: '' });
            return;
        }

        // Pretvaramo u ceo broj (integer)
        const numericVal = parseInt(val, 10);

        // Validacija opsega 1-5
        if (numericVal > 5) val = '5';
        else if (numericVal < 1) val = '1';
        else val = numericVal.toString();

        setNewReview({ ...newReview, rating: val });
    };

    const handleSaveReview = async () => {
        try {
            const token = localStorage.getItem("token");
            const generatedId = crypto.randomUUID(); // ISTI ID za sve baze

            console.log("!!! GENERISANI ID ZA SVE BAZE !!! ->", generatedId);

            // 1. KREIRAMO RESTORAN (Neo4j)
            const resResponse = await fetch(`https://localhost:7216/api/post/restaurant`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    Id: generatedId,
                    Name: newReview.name,
                    Location: newReview.location, // Grad
                    Cuisine: newReview.cuisine
                })
            });

            if (!resResponse.ok) throw new Error("Neo4j greška.");

            // 2. POPUNJAVAMO CASSANDRA LOOKUP (Da backend ne bi bacio Exception)
            await fetch(`https://localhost:7216/api/post/cassandra/lookup`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    Id: generatedId,
                    Name: newReview.name,
                    City: newReview.location,
                    Cuisine: newReview.cuisine,
                    Latitude: 0,
                    Longitude: 0
                })
            });

            // 3. ŠALJEMO RECENZIJU (Sada će GetCityAndCuisine pronaći podatke!)
            const reviewResponse = await fetch(`https://localhost:7216/api/post/restaurants/${generatedId}/review?userId=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    rating: parseInt(newReview.rating, 10),
                    comment: newReview.comment
                })
            });

            if (reviewResponse.ok) {
                alert("Uspešno sačuvano u sve tri baze!");
                // ... resetuj formu kao i ranije
            }

        } catch (err) {
            alert("Greška: " + err.message);
        }
    };

    if (loading) return <div className="profile-page"><h2>Učitavanje...</h2></div>;

    return (
        <div className="profile-page">
            <input type="file" ref={fileInputRef} onChange={(e) => {
                const file = e.target.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onloadend = () => setProfileImage(reader.result);
                    reader.readAsDataURL(file);
                }
            }} accept="image/*" style={{ display: 'none' }} />

            {/* GORNJI DEO PROFILA */}
            <div className="top-section-container">
                <div className="left-info-box">
                    <div className="avatar-circle" onClick={() => fileInputRef.current.click()}>
                        {profileImage ? <img src={profileImage} className="profile-img-element" /> : <span style={{ fontSize: '50px' }}>👤</span>}
                    </div>
                    <div className="user-text-container">
                        <h2 className="username-text">{(user?.username || "USER").toUpperCase()}</h2>
                        <p className="email-text">{user?.email}</p>
                    </div>
                </div>
                <div className="center-stats-box">
                    <div className="stat-item"><strong className="stat-number">{reviews.length}</strong><span className="stat-label">Recenzije</span></div>
                    <div className="stat-item"><strong className="stat-number">{user?.followersCount || 0}</strong><span className="stat-label">Followers</span></div>
                </div>
                <div className="right-balance-box"></div>
            </div>

            {/* SEKCIJA RECENZIJA */}
            <div className="content-section">
                <div className="horizontal-divider"></div>
                <h3 className="section-title">MOJE RECENZIJE</h3>
                <button className="add-review-btn" onClick={() => setIsModalOpen(true)}>+ DODAJ RECENZIJU</button>

                {/* PRIKAZ KARTICA RECENZIJA */}
                <div className="reviews-grid">
                    {reviews.map((rev) => (
                        <div key={rev.id} className="review-card">
                            <div className="review-rating">
                                ⭐ {rev.rating}
                            </div>
                            <h4>{rev.name}</h4>
                            <p className="review-cuisine">{rev.cuisine || 'Vrsta hrane nije navedena'}</p>
                            <p className="review-comment">{rev.comment}</p>
                        </div>
                    ))}
                </div>
            </div>

            {/* MODAL */}
            {isModalOpen && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <button className="close-modal" onClick={() => setIsModalOpen(false)}>&times;</button>
                        <h2>NOVA RECENZIJA</h2>
                        <input className="modal-input" placeholder="Ime restorana..." value={newReview.name} onChange={(e) => setNewReview({ ...newReview, name: e.target.value })} />
                        <input className="modal-input" placeholder="Lokacija (Niš)..." value={newReview.location} onChange={(e) => setNewReview({ ...newReview, location: e.target.value })} />
                        <input className="modal-input" placeholder="Vrsta hrane..." value={newReview.cuisine} onChange={(e) => setNewReview({ ...newReview, cuisine: e.target.value })} />
                        <input
                            className="modal-input"
                            type="number"
                            step="1"
                            placeholder="Ocena (1-5)"
                            value={newReview.rating}
                            onKeyPress={(e) => {
                                // Dozvoljavamo SAMO brojeve od 0-9
                                if (!/[0-9]/.test(e.key)) {
                                    e.preventDefault();
                                }
                            }}
                            onChange={handleRatingChange}
                        />
                        <textarea className="modal-input" placeholder="Komentar..." rows="4" value={newReview.comment} onChange={(e) => setNewReview({ ...newReview, comment: e.target.value })}></textarea>
                        <div className="modal-actions">
                            <button className="save-btn" onClick={handleSaveReview}>SAČUVAJ</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}