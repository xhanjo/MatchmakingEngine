// app.js - Application bootstrap, event coordination, and backward-compatible facade

const App = {
    // Shared state reference
    get state() {
        return Store.state;
    },
    set state(val) {
        Store.state = val;
    },

    // Runtime timer aliases
    get searchTimerInterval() { return Store.searchTimerInterval; },
    set searchTimerInterval(v) { Store.searchTimerInterval = v; },
    get searchStartTime() { return Store.searchStartTime; },
    set searchStartTime(v) { Store.searchStartTime = v; },
    get searchTimeout() { return Store.searchTimeout; },
    set searchTimeout(v) { Store.searchTimeout = v; },

    // ─── INITIALIZATION ──────────────────────────────────────────────────
    async init() {
        Auth.requireAuth();
        SignalRManager.init();

        // Logout button
        const logoutBtn = document.getElementById('logout-btn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', () => Auth.logout());
        }

        // Username display with avatar initial
        const username = Auth.getUserName() || 'Player';
        const displayEl = document.getElementById('username-display');
        if (displayEl) {
            const nameSpan = displayEl.querySelector('span');
            const avatarDiv = displayEl.querySelector('div');
            if (nameSpan) nameSpan.innerText = username;
            if (avatarDiv) avatarDiv.innerText = username.charAt(0).toUpperCase();
        }

        // Admin link in navbar
        if (Auth.isAdmin()) {
            const adminLink = document.createElement('a');
            adminLink.href = '#admin';
            adminLink.className = 'nav-link text-sm font-display font-semibold tracking-wider uppercase text-slate-500 hover:text-slate-300 border-b-2 border-transparent px-2 py-4 transition-colors';
            adminLink.innerText = 'Admin';
            const navLinks = document.getElementById('nav-links');
            if (navLinks) navLinks.appendChild(adminLink);
        }

        // Router initialization
        Router.init();

        // DOM event listeners
        const findMatchBtn = document.getElementById('find-match-btn');
        if (findMatchBtn) findMatchBtn.addEventListener('click', () => this.toggleMatchmaking());

        const createPartyBtn = document.getElementById('create-party-btn');
        if (createPartyBtn) createPartyBtn.addEventListener('click', () => this.createParty());

        const leavePartyBtn = document.getElementById('leave-party-btn');
        if (leavePartyBtn) leavePartyBtn.addEventListener('click', () => this.leaveParty());

        const acceptMatchBtn = document.getElementById('accept-match-btn');
        if (acceptMatchBtn) acceptMatchBtn.addEventListener('click', () => this.acceptMatch());

        const declineMatchBtn = document.getElementById('decline-match-btn');
        if (declineMatchBtn) declineMatchBtn.addEventListener('click', () => this.declineMatch());

        const playerSearch = document.getElementById('player-search');
        if (playerSearch) playerSearch.addEventListener('input', (e) => this.handlePlayerSearch(e.target.value));

        // Initial data fetch
        await this.loadInitialData();

        // Fallback polling intervals
        Store._pollingIntervals = [
            setInterval(() => this.loadFriends(), 45000),
            setInterval(() => this.updateMatchmakingStatus(), 15000)
        ];

        // Refresh on window focus
        window.addEventListener('focus', () => {
            this.loadFriends();
            this.updateMatchmakingStatus();
        });
    },

    // ─── DELEGATED METHODS (Compatibility Facade) ─────────────────────────

    // Lifecycle & Queue
    loadInitialData: () => QueueComponent.loadInitialData(),
    loadMyMmr: () => QueueComponent.loadMyMmr(),
    updateMatchmakingStatus: () => QueueComponent.updateMatchmakingStatus(),
    renderMatchmakingButton: () => QueueComponent.renderMatchmakingButton(),
    toggleMatchmaking: () => QueueComponent.toggleMatchmaking(),
    acceptMatch: () => QueueComponent.acceptMatch(),
    declineMatch: () => QueueComponent.declineMatch(),
    closeMatchModal: () => QueueComponent.closeMatchModal(),
    onMatchFound: (matchId) => QueueComponent.onMatchFound(matchId),
    onMatchCanceled: () => QueueComponent.onMatchCanceled(),
    onMatchStarted: (matchId) => QueueComponent.onMatchStarted(matchId),
    onMatchFinished: (result) => QueueComponent.onMatchFinished(result),

    // Lobby & Friends
    loadFriends: () => LobbyComponent.loadFriends(),
    loadParty: () => LobbyComponent.loadParty(),
    renderFriendsList: () => LobbyComponent.renderFriendsList(),
    renderParty: () => LobbyComponent.renderParty(),
    createParty: () => LobbyComponent.createParty(),
    leaveParty: () => LobbyComponent.leaveParty(),
    inviteToParty: (friendId) => LobbyComponent.inviteToParty(friendId),
    acceptFriend: (requestId) => LobbyComponent.acceptFriend(requestId),
    declineFriend: (requestId) => LobbyComponent.declineFriend(requestId),
    handlePlayerSearch: (query) => LobbyComponent.handlePlayerSearch(query),
    addFriend: (playerId) => LobbyComponent.addFriend(playerId),
    onPartyInviteReceived: (partyId, senderId) => LobbyComponent.onPartyInviteReceived(partyId, senderId),
    onPlayerJoinedParty: (playerId) => LobbyComponent.onPlayerJoinedParty(playerId),

    // Map Veto
    onMatchReadyForVeto: (vetoState) => VetoComponent.onMatchReadyForVeto(vetoState),
    onMapVetoUpdated: (vetoState) => VetoComponent.onMapVetoUpdated(vetoState),
    onMatchStarting: (matchId) => VetoComponent.onMatchStarting(matchId),
    startVetoTimer: (seconds) => VetoComponent.startVetoTimer(seconds),
    stopVetoTimer: () => VetoComponent.stopVetoTimer(),
    renderMapVetoUI: () => VetoComponent.renderMapVetoUI(),
    vetoMapClick: (mapName) => VetoComponent.vetoMapClick(mapName),

    // Profile & Leaderboard
    showProfile: () => ProfileComponent.showProfile(),
    loadLeaderboard: () => LeaderboardComponent.loadLeaderboard(),

    // Admin
    loadAdminData: () => AdminComponent.loadAdminData(),
    renderAdminPlayersTable: (players) => AdminComponent.renderAdminPlayersTable(players),
    renderAdminMatchTable: (tbodyId, matches, isActive) => AdminComponent.renderAdminMatchTable(tbodyId, matches, isActive),
    completeAdminMatch: (matchId) => AdminComponent.completeAdminMatch(matchId),

    // Router
    handleRoute: () => Router.handleRoute()
};

window.App = App;

document.addEventListener('DOMContentLoaded', () => {
    const path = window.location.pathname;
    if (path.endsWith('index.html') || path === '/' || path.endsWith('MatchmakingClient/') || path.endsWith('MatchmakingClient')) {
        App.init();
    }
});
