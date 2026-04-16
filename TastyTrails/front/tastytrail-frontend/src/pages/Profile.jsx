import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import '../Profile.css';

export default function Profile() {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [profileImage, setProfileImage] = useState(null);
    const [reviews, setReviews] = useState([]);

    const [isFollowing, setIsFollowing] = useState(false);
    const [followersCount, setFollowersCount] = useState(0);

    const [searchTerm, setSearchTerm] = useState("");
    const [searchResults, setSearchResults] = useState([]);

    const fileInputRef = useRef(null);
    const { id } = useParams();
    const navigate = useNavigate();

    const loggedInUserId = localStorage.getItem("userId");
    const isOwnProfile = id === loggedInUserId;

    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [currentReview, setCurrentReview] = useState(null); // Recenzija koju menjamo

    const [showFollowModal, setShowFollowModal] = useState(false);
    const [modalTitle, setModalTitle] = useState("");
    const [modalUsers, setModalUsers] = useState([]);
    const [isModalLoading, setIsModalLoading] = useState(false);

    const handleNavigateToUserProfile = (targetUserId) => {
        setShowFollowModal(false); // Zatvori modal
        navigate(`/profile/${targetUserId}`); // Navigacija na novi ID
    };

    const [savedRestaurants, setSavedRestaurants] = useState([]);

    // 1. Fetch Podataka
    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setReviews([]);
            const authToken = localStorage.getItem("authToken");
            const currentLoggedInId = localStorage.getItem("userId");

            try {
                const userRes = await fetch(`http://localhost:5146/api/get/user/${id}`, {
                    headers: { 'Authorization': `Bearer ${authToken}` }
                });

                if (userRes.ok) {
                    const userData = await userRes.json();
                    setUser(userData);

                    const followersList = userData.followers || userData.Followers || [];
                    setFollowersCount(followersList.length);

                    if (currentLoggedInId) {
                        const amIFollowing = followersList.some(fId =>
                            fId.toString().toLowerCase() === currentLoggedInId.toLowerCase()
                        );
                        setIsFollowing(amIFollowing);
                    }
                } else if (userRes.status === 401) {
                    // Ako je token istekao, obriši ga i vrati na login
                    localStorage.removeItem("authToken");
                    localStorage.removeItem("userId");
                    navigate("/login");
                }

                const reviewsRes = await fetch(`http://localhost:5146/api/get/reviews/${id}`, {
                    headers: { 'Authorization': `Bearer ${authToken}` }
                });

                if (reviewsRes.ok) {
                    const reviewsData = await reviewsRes.json();
                    setReviews(reviewsData || []);
                } else {
                    setReviews([]);
                }

                const savedRes = await fetch(`http://localhost:5146/api/user/${id}/saved`, {
                    headers: { 'Authorization': `Bearer ${authToken}` }
                });

                if (savedRes.ok) {
                    const savedData = await savedRes.json();
                    setSavedRestaurants(savedData || []);
                }
            } catch (err) {
                console.error("Greška pri učitavanju:", err);
                setReviews([]);
            } finally {
                setLoading(false);
            }
        };

        if (id) fetchData();
    }, [id, navigate]);

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

   //Follow
    const handleFollowToggle = async () => {
        const authToken = localStorage.getItem("authToken");
        if (!authToken) {
            alert("Morate biti ulogovani.");
            return;
        }

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
                setIsFollowing(!isFollowing);
                setFollowersCount(prev => isFollowing ? prev - 1 : prev + 1);
            }
        } catch (err) {
            console.error("Mrežna greška:", err);
        }
    };

    const openFollowModal = async (type) => {
        setModalTitle(type === "followers" ? "Followers" : "Following");
        setShowFollowModal(true);
        setModalUsers([]);
        setIsModalLoading(true);

        try {
            const token = localStorage.getItem('authToken');

            // ISPRAVLJENO: Promenljiva se zove 'user', a ID može biti 'id' ili 'Id'
            const userId = user?.id || user?.Id;

            if (!userId) {
                console.error("ID korisnika nije pronađen.");
                return;
            }

            const res = await fetch(`http://localhost:5146/api/user/relations/${userId}/${type}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                const data = await res.json();
                setModalUsers(data);
            } else {
                console.error("Server vratio grešku:", res.status);
            }
        } catch (err) {
            console.error("Greška pri učitavanju liste:", err);
        } finally {
            setIsModalLoading(false);
        }
    };

    //Delete
    const handleDelete = async (reviewId) => {
        if (!window.confirm("Da li ste sigurni da želite da obrišete ovu recenziju?")) return;

        const authToken = localStorage.getItem("authToken");
        try {
            const res = await fetch(`http://localhost:5146/api/user/review/delete/${reviewId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${authToken}` }
            });

            if (res.ok) {
                // Skloni recenziju iz state-a odmah
                setReviews(prev => prev.filter(r => (r.id || r.Id) !== reviewId));
            } else {
                alert("Greška pri brisanju.");
            }
        } catch (err) {
            console.error("Greška:", err);
        }
    };

    const openEditModal = (review) => {
        setCurrentReview({ ...review }); // Kopiramo podatke da ne menjamo direktno u listi dok ne sačuvamo
        setIsEditModalOpen(true);
    };

    //Edit
    const handleUpdateReview = async () => {
        const authToken = localStorage.getItem("authToken");
        try {
            const res = await fetch(`http://localhost:5146/api/user/review/update`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(currentReview)
            });

            if (res.ok) {
                // Ažuriraj listu recenzija u state-u
                setReviews(prev => prev.map(r => (r.id || r.Id) === currentReview.id ? currentReview : r));
                setIsEditModalOpen(false);
            }
        } catch (err) {
            console.error("Greška pri ažuriranju:", err);
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
                        <div
                            className="stat-item"
                            onClick={() => openFollowModal("followers")}
                            style={{ cursor: 'pointer' }}
                        >
                            <strong className="stat-number">{followersCount}</strong>
                            <span className="stat-label">FOLLOWERS</span>
                        </div>

                        <div
                            className="stat-item"
                            onClick={() => openFollowModal("following")}
                            style={{ cursor: 'pointer' }}
                        >
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

                <div className="reviews-grid">
                    {reviews.map((rev) => (
                        <div key={rev.id || rev.Id} className="review-card">

                            {/* NOVO: Dugmad za Edit i Delete (samo za tvoj profil) */}
                            {isOwnProfile && (
                                <div className="review-admin-actions-center">
                                    <button
                                        className="action-text-btn edit-link"
                                        onClick={() => openEditModal(rev)}
                                    >
                                        Edit
                                    </button>
                                    <span className="action-separator">|</span>
                                    <button
                                        className="action-text-btn delete-link"
                                        onClick={() => handleDelete(rev.id || rev.Id)}
                                    >
                                        Delete
                                    </button>
                                </div>
                            )}

                            <div className="review-rating">⭐ {rev.rating}</div>
                            <h4>{rev.name || rev.restaurantName}</h4>
                            <p className="review-cuisine">{rev.cuisine || 'VRSTA HRANE NIJE NAVEDENA'}</p>
                            <p className="review-comment">{rev.comment}</p>
                        </div>
                    ))}
                    {reviews.length === 0 && <p style={{ color: 'gray', marginTop: '20px' }}>Nema pronađenih recenzija.</p>}
                </div>
            </div>
            {isEditModalOpen && (
                <div className="modal-overlay">
                    <div className="edit-modal">
                        <h3>Izmeni recenziju</h3>
                        <label>Ocena (1-5):</label>
                        <input
                            type="number"
                            min="1" max="5"
                            value={currentReview.rating}
                            onChange={(e) => setCurrentReview({ ...currentReview, rating: e.target.value })}
                        />

                        <label>Komentar:</label>
                        <textarea
                            value={currentReview.comment}
                            onChange={(e) => setCurrentReview({ ...currentReview, comment: e.target.value })}
                        />

                        <div className="modal-buttons">
                            <button className="save-btn" onClick={handleUpdateReview}>Sačuvaj</button>
                            <button className="cancel-btn" onClick={() => setIsEditModalOpen(false)}>Otkaži</button>
                        </div>
                    </div>
                </div>
            )}

            <div className="content-section">
                <div className="horizontal-divider"></div>
                <h3 className="section-title">SAČUVANI RESTORANI</h3>

                <div className="reviews-grid">
                    {savedRestaurants.length > 0 ? (
                        savedRestaurants.map((rest) => (
                            <div key={rest.id || rest.Id} className="review-card saved-card">
                                <h4>{rest.name}</h4>
                                <p className="review-cuisine">
                                    {rest.cuisine && rest.cuisine !== 'unknown'
                                        ? rest.cuisine
                                        : 'Nije navedeno'}
                                </p>
                                <button
                                    className="view-profile-btn"
                                    onClick={() => navigate(`/explore`)}
                                    style={{ marginTop: '10px', width: '100%' }}
                                >
                                    Pogledaj
                                </button>
                            </div>
                        ))
                    ) : (
                        <p style={{ color: 'gray', marginTop: '20px' }}>Nema sačuvanih restorana.</p>
                    )}
                </div>
            </div>

            {showFollowModal && (
                <div className="modal-overlay" onClick={() => setShowFollowModal(false)}>
                    <div className="modal-container" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>{modalTitle}</h3>
                            <button className="close-button" onClick={() => setShowFollowModal(false)}>&times;</button>
                        </div>

                        <div className="modal-body">
                            {isModalLoading ? (
                                <div className="loader">Učitavanje...</div>
                            ) : modalUsers.length > 0 ? (
                                    modalUsers.map((user) => (                                      
                                        <div key={user.id} className="user-row">
                                            <div className="user-info">
                                                {user.profileImage ? (
                                                    <img
                                                        src={user.profileImage}
                                                        alt={user.username}
                                                        className="modal-avatar"
                                                    />
                                                ) : (
                                                    <span className="modal-avatar-placeholder">👤</span>
                                                )}
                                                <span className="modal-username">{user.username}</span>
                                            </div>
                                            <button
                                                className="view-profile-btn"
                                                onClick={() => handleNavigateToUserProfile(user.id)}
                                            >
                                                Profil
                                            </button>
                                        </div>
                                    ))
                            ) : (
                                <p className="no-data">Nema korisnika za prikaz.</p>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}