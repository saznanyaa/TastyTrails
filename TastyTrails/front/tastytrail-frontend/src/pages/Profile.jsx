import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import '../Profile.css';

export default function Profile() {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [profileImage, setProfileImage] = useState(null);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [reviews, setReviews] = useState([]);

    const [isFollowing, setIsFollowing] = useState(false);
    const [followersCount, setFollowersCount] = useState(0);

    const [searchTerm, setSearchTerm] = useState("");
    const [searchResults, setSearchResults] = useState([]);

    const [newReview, setNewReview] = useState({
        name: '',
        location: '',
        cuisine: '',
        rating: '',
        comment: ''
    });

    const fileInputRef = useRef(null);
    const { id } = useParams();
    const navigate = useNavigate();

    const loggedInUserId = localStorage.getItem("userId");
    const isOwnProfile = id === loggedInUserId;

    // 1. Fetch Podataka
    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            const authToken = localStorage.getItem("authToken");

            try {
                const userRes = await fetch(`http://localhost:5146/api/get/user/${id}`, {
                    headers: { 'Authorization': `Bearer ${authToken}` }
                });

                if (userRes.ok) {
                    const userData = await userRes.json();
                    setUser(userData);

                    // Rešavamo problem velikih/malih slova u JSON ključevima
                    const followersList = userData.followers || userData.Followers || [];
                    setFollowersCount(followersList.length);

                    if (loggedInUserId) {
                        // FIX: Provera mora biti case-insensitive i raditi sa stringovima
                        const amIFollowing = followersList.some(fId =>
                            fId.toString().toLowerCase() === loggedInUserId.toLowerCase()
                        );
                        setIsFollowing(amIFollowing);
                    }
                }

                const reviewsRes = await fetch(`http://localhost:5146/api/get/reviews/${id}/mongouser/reviews`, {
                    headers: { 'Authorization': `Bearer ${authToken}` }
                });

                if (reviewsRes.ok) {
                    const reviewsData = await reviewsRes.json();
                    setReviews(reviewsData);
                }
            } catch (err) {
                console.error("Greška pri učitavanju:", err);
            } finally {
                setLoading(false);
            }
        };

        if (id) fetchData();
    }, [id, loggedInUserId]);

    // 2. Search Logic
    useEffect(() => {
        const delayDebounceFn = setTimeout(async () => {
            if (searchTerm.trim().length > 1) {
                const authToken = localStorage.getItem("authToken");
                try {
                    const res = await fetch(`http://localhost:5146/api/get/users/search?username=${searchTerm}`, {
                        headers: { 'Authorization': `Bearer ${authToken}` }
                    });
                    if (res.ok) {
                        const data = await res.json();
                        setSearchResults(data);
                    }
                } catch (err) {
                    console.error("Greška pri pretrazi:", err);
                }
            } else {
                setSearchResults([]);
            }
        }, 300);

        return () => clearTimeout(delayDebounceFn);
    }, [searchTerm]);

    // 3. Follow / Unfollow logika (Sređeno da radi sa tvojim novim backendom)
    const handleFollowToggle = async () => {
        const authToken = localStorage.getItem("authToken");

        if (!authToken) {
            alert("Morate biti ulogovani.");
            return;
        }

        // Određujemo endpoint na osnovu trenutnog isFollowing stanja
        const endpoint = isFollowing ? 'unfollow' : 'follow';
        const url = `http://localhost:5146/api/user/${endpoint}/${id}`;

        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Content-Type': 'application/json'
                }
            });

            if (res.ok) {
                // UI update: ako je bilo follow, sad je true, broj raste. Ako je bio unfollow, false, broj opada.
                setIsFollowing(!isFollowing);
                setFollowersCount(prev => isFollowing ? prev - 1 : prev + 1);
            } else if (res.status === 401) {
                alert("Sesija je nevažeća (401). Prijavite se ponovo.");
            } else {
                const errorText = await res.text();
                console.error("Server greška:", errorText);
            }
        } catch (err) {
            console.error("Mrežna greška:", err);
        }
    };

    const handleRatingChange = (e) => {
        let val = e.target.value;

        // 1. Ako je polje prazno, dozvoli brisanje (prazan string)
        if (val === '') {
            setNewReview({ ...newReview, rating: '' });
            return;
        }

        // 2. REGEX: Provera da li su uneti samo brojevi. 
        // Ako nije broj, funkcija se ovde prekida i ne menja stanje (slovo se ne pojavljuje)
        if (!/^\d+$/.test(val)) {
            return;
        }

        // 3. Logika za limit 1-5
        let numericVal = parseInt(val, 10);

        if (numericVal > 5) {
            val = '5';
        } else if (numericVal < 1) {
            // Dozvoljavamo da ostane šta je kucao dok ne završi, 
            // ili automatski stavljamo 1 ako je npr. kucao 0
            val = '1';
        } else {
            val = numericVal.toString();
        }

        setNewReview({ ...newReview, rating: val });
    };

    const handleSaveReview = async () => {
        try {
            const authToken = localStorage.getItem("authToken");
            const generatedId = crypto.randomUUID();

            //i ovo mi se ne svidja kako radi
            await fetch(`http://localhost:5146/api/post/restaurant`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${authToken}` },
                body: JSON.stringify({
                    Id: generatedId,
                    Name: newReview.name,
                    Location: newReview.location,
                    Cuisine: newReview.cuisine
                })
            });

            //oovo mora da se proveri!!!!
            const reviewResponse = await fetch(`http://localhost:5146/api/post/restaurants/${generatedId}/review?userId=${id}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${authToken}` },
                body: JSON.stringify({
                    rating: parseInt(newReview.rating, 10),
                    comment: newReview.comment
                })
            });

            if (reviewResponse.ok) {
                alert("Recenzija uspešno sačuvana!");
                setIsModalOpen(false);
                window.location.reload();
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

            <div className="top-section-container">
                <div className="left-info-box">
                    <div className={`avatar-circle ${isOwnProfile ? 'editable' : ''}`} onClick={() => isOwnProfile && fileInputRef.current.click()}>
                        {profileImage ? <img src={profileImage} className="profile-img-element" alt="Profile" /> : <span style={{ fontSize: '50px' }}>👤</span>}
                    </div>
                    <div className="user-text-container">
                        <h2 className="username-text">
                            {(user?.username || user?.Username || user?.name || user?.Name || "NEPOZNAT").toUpperCase()}
                        </h2>
                    </div>
                </div>

                <div className="center-stats-box">
                    <div className="stats-row">
                        <div className="stat-item">
                            <strong className="stat-number">{reviews.length}</strong>
                            <span className="stat-label">RECENZIJE</span>
                        </div>
                        <div className="stat-item">
                            <strong className="stat-number">{followersCount}</strong>
                            <span className="stat-label">FOLLOWERS</span>
                        </div>
                        <div className="stat-item">
                            <strong className="stat-number">
                                {(user?.following?.length || user?.Following?.length || 0)}
                            </strong>
                            <span className="stat-label">FOLLOWING</span>
                        </div>
                    </div>

                    {!isOwnProfile && (
                        <button
                            className={`follow-action-btn ${isFollowing ? 'unfollow-style' : ''}`}
                            onClick={handleFollowToggle}
                        >
                            {isFollowing ? "UNFOLLOW" : "FOLLOW"}
                        </button>
                    )}
                </div>

                <div className="right-search-area">
                    <div className="search-wrapper">
                        <input
                            type="text"
                            placeholder="Pretraži korisnike..."
                            className="profile-search-input"
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                        />
                        {searchResults.length > 0 && (
                            <div className="search-dropdown">
                                {searchResults.map((u) => (
                                    <div
                                        key={u.id || u.Id}
                                        className="search-item"
                                        onClick={() => {
                                            navigate(`/profile/${u.id || u.Id}`);
                                            setSearchTerm("");
                                        }}
                                    >
                                        <span className="search-icon">👤</span>
                                        <span className="search-name">{u.username || u.Username}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <div className="content-section">
                <div className="horizontal-divider"></div>
                <h3 className="section-title">
                    {isOwnProfile ? "MOJE RECENZIJE" : "RECENZIJE KORISNIKA"}
                </h3>

                {isOwnProfile && (
                    <button className="add-review-btn" onClick={() => setIsModalOpen(true)}>
                        + DODAJ RECENZIJU
                    </button>
                )}

                <div className="reviews-grid">
                    {reviews.map((rev) => (
                        <div key={rev.id || rev.Id} className="review-card">
                            <div className="review-rating">⭐ {rev.rating}</div>
                            <h4>{rev.name}</h4>
                            <p className="review-cuisine">{rev.cuisine || 'VRSTA HRANE NIJE NAVEDENA'}</p>
                            <p className="review-comment">{rev.comment}</p>
                        </div>
                    ))}
                    {reviews.length === 0 && <p style={{ color: 'gray', marginTop: '20px' }}>Nema pronađenih recenzija.</p>}
                </div>
            </div>

            {isModalOpen && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        {/* X dugme sada ima samo klasu close-modal bez ikakvog okvira */}
                        <button className="close-modal" onClick={() => setIsModalOpen(false)}>
                            &times;
                        </button>

                        <h2 className="section-title">NOVA RECENZIJA</h2>

                        <div className="modal-inputs-container">
                            <input className="modal-input" placeholder="Ime restorana..." value={newReview.name} onChange={(e) => setNewReview({ ...newReview, name: e.target.value })} />
                            <input className="modal-input" placeholder="Lokacija..." value={newReview.location} onChange={(e) => setNewReview({ ...newReview, location: e.target.value })} />
                            <input className="modal-input" placeholder="Vrsta hrane..." value={newReview.cuisine} onChange={(e) => setNewReview({ ...newReview, cuisine: e.target.value })} />

                            {/* Input za ocenu je sada type="text" da bi sakrili strelice, a tvoja logika u handleRatingChange već hendluje 1-5 */}
                            <input
                                className="modal-input"
                                type="text"
                                inputMode="numeric"
                                placeholder="Ocena (1-5)"
                                value={newReview.rating}
                                onChange={handleRatingChange}
                            />

                            <textarea className="modal-input" placeholder="Komentar..." rows="4" value={newReview.comment} onChange={(e) => setNewReview({ ...newReview, comment: e.target.value })}></textarea>
                        </div>

                        <div className="modal-actions-centered">
                            <button className="add-review-btn" onClick={handleSaveReview}>SAČUVAJ</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}