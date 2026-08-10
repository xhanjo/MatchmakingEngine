const API_URL = 'http://matchmaking-engine-env.eba-mrkcxifv.eu-north-1.elasticbeanstalk.com/api';

const Api = {
    async request(endpoint, options = {}) {
        const url = `${API_URL}${endpoint}`;
        
        const token = Auth.getToken();
        const headers = {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            ...(options.headers || {})
        };

        const response = await fetch(url, { ...options, headers });
        
        if (response.status === 401) {
            if (!window.location.href.includes('login.html') && !window.location.href.includes('register.html')) {
                Auth.logout();
            }
            throw new Error("Invalid username or password");
        }

        const isJson = response.headers.get('content-type')?.includes('application/json');
        const data = isJson ? await response.json() : await response.text();

        if (!response.ok) {
            let errorMsg = `Error ${response.status}`;
            if (data) {
                if (typeof data === 'string') {
                    errorMsg = data;
                } else if (data.message) {
                    errorMsg = data.message;
                } else if (data.error) {
                    errorMsg = data.error;
                } else if (data.title) {
                    errorMsg = data.title;
                } else {
                    errorMsg = JSON.stringify(data);
                }
            }
            throw new Error(errorMsg);
        }

        return data;
    },

    // Auth
    login: (username, password) => Api.request('/Auth/login', { method: 'POST', body: JSON.stringify({ username, password }) }),
    register: (username, password, region) => Api.request('/Players/register', { method: 'POST', body: JSON.stringify({ username, password, region: parseInt(region) }) }),

    // Players
    getPlayers: () => Api.request('/Players'),
    getPlayer: (id) => Api.request(`/Players/${id}`),

    // Friends
    sendFriendRequest: (targetPlayerId) => Api.request(`/Friends/request/${targetPlayerId}`, { method: 'POST' }),
    acceptFriendRequest: (requestId) => Api.request(`/Friends/accept/${requestId}`, { method: 'POST' }),
    declineFriendRequest: (requestId) => Api.request(`/Friends/decline/${requestId}`, { method: 'POST' }),
    removeFriend: (friendshipId) => Api.request(`/Friends/${friendshipId}`, { method: 'DELETE' }),
    getFriends: () => Api.request('/Friends'),
    getPendingFriends: () => Api.request('/Friends/pending'),
    getMyHistory: () => Api.request('/Players/my/history'),
    getLeaderboard: () => Api.request('/Leaderboard'),

    // Matchmaking
    joinMatchmaking: () => Api.request('/Matchmaking/join', { method: 'POST' }),
    leaveMatchmaking: () => Api.request('/Matchmaking/leave', { method: 'POST' }),
    getMatchmakingStatus: () => Api.request('/Matchmaking/status'),
    acceptMatch: (matchId) => Api.request(`/Matchmaking/accept/${matchId}`, { method: 'POST' }),
    declineMatch: (matchId) => Api.request(`/Matchmaking/decline/${matchId}`, { method: 'POST' }),
    vetoMap: (matchId, mapName) => Api.request(`/Matchmaking/veto/${matchId}/${mapName}`, { method: 'POST' }),

    // Party
    createParty: () => Api.request('/Party/create', { method: 'POST', body: JSON.stringify(2) }),
    inviteToParty: (friendId) => Api.request(`/Party/invite/${friendId}`, { method: 'POST' }),
    joinParty: (partyId) => Api.request(`/Party/join/${partyId}`, { method: 'POST' }),
    leaveParty: () => Api.request('/Party/leave', { method: 'POST' }),
    getMyParty: () => Api.request('/Party/my').catch(e => null), // Return null if not in party

    // Admin
    getAllMatches: () => Api.request('/Admin/matches'),
    getActiveMatches: () => Api.request('/Admin/active-matches'),
    completeMatch: (matchId) => Api.request(`/Matchmaking/complete/${matchId}`, { method: 'POST' }),
};

window.Api = Api;
