import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import '../Profile.css';

export default function Profile() {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [profileImage, setProfileImage] = useState(null);
    const [reviews, setReviews] = useState([]);
    const [savedRestaurants, setSavedRestaurants] = useState([]);

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
    const [currentReview, setCurrentReview] = useState(null);

    // Follow Modal State
    const [showFollowModal, setShowFollowModal] = useState(false);
    const [modalTitle, setModalTitle] = useState("");
    const [modalUsers, setModalUsers] = useState([]);
    const [isModalLoading, setIsModalLoading] = useState(false);

    const handleNavigateToUserProfile = (targetUserId) => {
        setShowFollowModal(false);
        navigate(`/profile/${targetUserId}`);
    };

    // 1. Fetch Podataka - FIX: Sada resetuje state pre svakog novog učitavanja
    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setReviews([]);
            setSavedRestaurants([]);
            setUser(null);

            const authToken = localStorage.getItem("authToken");
            const currentLoggedInId = localStorage.getItem("userId");

            try {
                // User info
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
                }

                // Reviews
                const reviewsRes = await fetch(`http://localhost:5146/api/get/reviews/${id}`, {
                    headers: { 'Authorization': `Bearer ${authToken}` }
                });
                if (reviewsRes.ok) {
                    const reviewsData = await reviewsRes.json();
                    setReviews(reviewsData || []);
                }

                // Saved Restaurants - Samo za vlasnika
                if (isOwnProfile) {
                    const savedRes = await fetch(`http://localhost:5146/api/user/${id}/saved`, {
                        headers: { 'Authorization': `Bearer ${authToken}` }
                    });
                    if (savedRes.ok) {
                        const savedData = await savedRes.json();
                        setSavedRestaurants(savedData || []);
                    }
                }
            } catch (err) {
                console.error("Greška pri učitavanju:", err);
            } finally {
                setLoading(false);
            }
        };

        if (id) fetchData();
    }, [id, isOwnProfile]);

    // Search Logic (Debounce)
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
                } catch (err) { console.error(err); }
            } else { setSearchResults([]); }
        }, 300);
        return () => clearTimeout(delayDebounceFn);
    }, [searchTerm]);

    const handleFollowToggle = async () => {
        const authToken = localStorage.getItem("authToken");
        if (!authToken) {
            alert("Morate biti ulogovani.");
            return;
        }

        // Određujemo metodu na osnovu akcije
        const method = isFollowing ? 'DELETE' : 'POST';
        const endpoint = isFollowing ? 'unfollow' : 'follow';
        const url = `http://localhost:5146/api/user/${endpoint}/${id}`;

        try {
            const res = await fetch(url, {
                method: method, // OVO JE KLJUČNA IZMENA
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Content-Type': 'application/json'
                }
            });

            if (res.ok) {
                setIsFollowing(!isFollowing);
                setFollowersCount(prev => isFollowing ? prev - 1 : prev + 1);
            } else {
                const errorData = await res.json();
                console.error("Server error:", errorData);
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
            const res = await fetch(`http://localhost:5146/api/user/relations/${id}/${type}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setModalUsers(data);
            }
        } catch (err) { console.error(err); } finally { setIsModalLoading(false); }
    };

    const handleDelete = async (reviewId) => {
        if (!window.confirm("Da li ste sigurni?")) return;
        const authToken = localStorage.getItem("authToken");
        try {
            const res = await fetch(`http://localhost:5146/api/user/${loggedInUserId}/review/${reviewId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${authToken}` }
            });
            if (res.ok) setReviews(prev => prev.filter(r => (r.id || r.Id) !== reviewId));
        } catch (err) { console.error(err); }
    };

    const openEditModal = (review) => {
        setCurrentReview({ ...review });
        setIsEditModalOpen(true);
    };

    const handleUpdateReview = async () => {
        const authToken = localStorage.getItem("authToken");
        const loggedInUserId = localStorage.getItem("userId");
        try {
            const res = await fetch(`http://localhost:5146/api/user/update/${loggedInUserId}/review/${currentReview.restaurantId}`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(currentReview)
            });
            if (res.ok) {
                setReviews(prev => prev.map(r => (r.id || r.Id) === (currentReview.id || currentReview.Id) ? currentReview : r));
                setIsEditModalOpen(false);
            }
        } catch (err) { console.error(err); }
    };

    if (loading) return <div className="profile-page"><h2>Učitavanje...</h2></div>;

    return (
        <div className="profile-page">
            <input type="file" ref={fileInputRef} style={{ display: 'none' }} onChange={(e) => {
                const file = e.target.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onloadend = () => setProfileImage(reader.result);
                    reader.readAsDataURL(file);
                }
            }} />

            <div className="top-section-container">
                <div className="left-info-box">
                    <div className={`avatar-circle ${isOwnProfile ? 'editable' : ''}`} onClick={() => isOwnProfile && fileInputRef.current.click()}>
                        {profileImage ? <img src={profileImage} className="profile-img-element" alt="Profile" /> : <span style={{ fontSize: '50px' }}>👤</span>}
                    </div>
                    <div className="user-text-container">
                        <h2 className="username-text">{(user?.username || user?.Username || "KORISNIK").toUpperCase()}</h2>
                    </div>
                </div>

                <div className="center-stats-box">
                    <div className="stats-row">
                        <div className="stat-item">
                            <strong className="stat-number">{reviews.length}</strong>
                            <span className="stat-label">RECENZIJE</span>
                        </div>
                        <div className="stat-item clickable" onClick={() => openFollowModal("followers")}>
                            <strong className="stat-number">{followersCount}</strong>
                            <span className="stat-label">FOLLOWERS</span>
                        </div>
                        <div className="stat-item clickable" onClick={() => openFollowModal("following")}>
                            <strong className="stat-number">{user?.following?.length || user?.Following?.length || 0}</strong>
                            <span className="stat-label">FOLLOWING</span>
                        </div>
                    </div>
                    {!isOwnProfile && (
                        <button className={`follow-action-btn ${isFollowing ? 'unfollow-style' : ''}`} onClick={handleFollowToggle}>
                            {isFollowing ? "UNFOLLOW" : "FOLLOW"}
                        </button>
                    )}
                </div>

                <div className="right-search-area">
                    <div className="search-wrapper">
                        <input type="text" placeholder="Pretraži..." className="profile-search-input" value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} />
                        {searchResults.length > 0 && (
                            <div className="search-dropdown">
                                {searchResults.map((u) => (
                                    <div key={u.id || u.Id} className="search-item" onClick={() => { navigate(`/profile/${u.id || u.Id}`); setSearchTerm(""); }}>
                                        <span>👤 {u.username || u.Username}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* SAČUVANI RESTORANI SEKCIJA */}
            {isOwnProfile && (<div className="content-section">
                <div className="horizontal-divider"></div>
                <h3 className="section-title">SAČUVANI RESTORANI</h3>
                <div className="reviews-grid">
                    {savedRestaurants.length > 0 ? (
                        savedRestaurants.map((rest) => (
                            <div
                                key={rest.id || rest.Id}
                                className="review-card saved-card clickable-card"
                                onClick={() => navigate(`/restaurant/${rest.id || rest.Id}`)}
                            >
                                <h4>{rest.name}</h4>
                                <p className="review-cuisine">{rest.cuisine || 'Nije navedeno'}</p>
                            </div>
                        ))
                    ) : <p style={{ color: 'gray' }}>Nema sačuvanih restorana.</p>}
                </div>
            </div>)}

            {/* RECENZIJE SEKCIJA */}
            <div className="content-section">
                <div className="horizontal-divider"></div>
                <h3 className="section-title">{isOwnProfile ? "MOJE RECENZIJE" : "RECENZIJE KORISNIKA"}</h3>
                <div className="reviews-grid">
                    {reviews.map((rev) => (
                        <div key={rev.id || rev.Id} className="review-card">
                            <div className="review-rating">⭐ {rev.rating}</div>
                            <h4>{rev.name || rev.restaurantName}</h4>
                            <p className="review-comment">{rev.comment}</p>
                            {isOwnProfile && (
                                <div className="review-admin-actions-center">
                                    <button className="action-text-btn edit-link" onClick={() => openEditModal(rev)}>Edit</button>
                                    <button className="action-text-btn delete-link" onClick={() => handleDelete(rev.id || rev.Id)}>Delete</button>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            </div>
            

            {/* MODALI */}
            {isEditModalOpen && (
                <div className="modal-overlay">
                    <div className="edit-modal">
                        <h3>Izmeni recenziju</h3>
                        <label>Ocena:</label>
                        <input type="number" min="1" max="5" value={currentReview.rating} onChange={(e) => setCurrentReview({ ...currentReview, rating: Number(e.target.value) })} />
                        <label>Komentar:</label>
                        <textarea value={currentReview.comment} onChange={(e) => setCurrentReview({ ...currentReview, comment: e.target.value })} />
                        <div className="modal-buttons">
                            <button className="save-btn" onClick={handleUpdateReview}>Sačuvaj</button>
                            <button className="cancel-btn" onClick={() => setIsEditModalOpen(false)}>Otkaži</button>
                        </div>
                    </div>
                </div>
            )}

            {/* FOLLOWERS / FOLLOWING MODAL */}
            {showFollowModal && (
                <div className="modal-overlay" onClick={() => setShowFollowModal(false)}>
                    <div className="follow-modal" onClick={e => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>{modalTitle}</h3>
                            <button className="close-btn" onClick={() => setShowFollowModal(false)}>&times;</button>
                        </div>
                        <div className="modal-body">
                            {isModalLoading ? <p>Učitavanje...</p> :
                                modalUsers.length > 0 ? modalUsers.map(u => (
                                    <div key={u.id || u.Id} className="modal-user-item" onClick={() => { navigate(`/profile/${u.id || u.Id}`); setShowFollowModal(false); }}>
                                        <span>👤 {u.username || u.Username}</span>
                                    </div>
                                )) : <p>Nema korisnika za prikaz.</p>}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}