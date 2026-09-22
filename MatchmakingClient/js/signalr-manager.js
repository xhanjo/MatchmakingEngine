const SignalRManager = {
    connection: null,
    
    init() {
        if (!window.Auth || !window.Auth.isLoggedIn()) return;
        
        const token = window.Auth.getToken();
        const hubUrl = (window.APP_CONFIG && window.APP_CONFIG.HUB_URL)
            ? window.APP_CONFIG.HUB_URL
            : (window.API_URL || (window.Api ? window.Api.getBaseUrl() : '')).replace('/api', '/hubs/matchmaking');
        
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, { accessTokenFactory: () => token })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        this.connection.onreconnecting((error) => {
            console.warn('SignalR reconnecting:', error);
            if (window.Utils) window.Utils.showToast('Reconnecting to server...', 'info', 2000);
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected:', connectionId);
            if (window.Utils) window.Utils.showToast('Reconnected to server!', 'success', 2000);
            if (window.App && typeof window.App.updateMatchmakingStatus === 'function') {
                window.App.updateMatchmakingStatus();
            }
        });
            
        // Event handlers
        this.connection.on('MatchFound', (matchId) => {
            console.log('Match Found:', matchId);
            if (window.App && typeof window.App.onMatchFound === 'function') {
                window.App.onMatchFound(matchId);
            }
        });
        
        this.connection.on("MatchStarted", (matchId) => {
            console.log("MatchStarted event received", matchId);
            if (window.App && typeof window.App.onMatchStarted === 'function') {
                window.App.onMatchStarted(matchId);
            }
        });
        
        this.connection.on("MatchStarting", (matchId) => {
            console.log("MatchStarting event received", matchId);
            if (window.App && typeof window.App.onMatchStarting === 'function') {
                window.App.onMatchStarting(matchId);
            }
        });

        this.connection.on('MatchCanceled', () => {
            console.log('Match Canceled');
            if (window.App && typeof window.App.onMatchCanceled === 'function') {
                window.App.onMatchCanceled();
            }
        });
        
        this.connection.on('PartyInviteReceived', (partyId, senderId) => {
            console.log('Party Invite:', partyId, senderId);
            if (window.App && typeof window.App.onPartyInviteReceived === 'function') {
                window.App.onPartyInviteReceived(partyId, senderId);
            }
        });
        
        this.connection.on('PlayerJoinedParty', (playerId) => {
            console.log('Player Joined Party:', playerId);
            if (window.App && typeof window.App.onPlayerJoinedParty === 'function') {
                window.App.onPlayerJoinedParty(playerId);
            }
        });

        this.connection.on('FriendsUpdated', () => {
            console.log('Friends updated!');
            if (window.App && typeof window.App.loadFriends === 'function') {
                window.App.loadFriends();
            }
        });

        this.connection.on('MatchFinished', (result) => {
            console.log('Match finished!', result);
            if (window.App && typeof window.App.onMatchFinished === 'function') {
                window.App.onMatchFinished(result);
            }
        });

        this.connection.on('MatchReadyForVeto', (vetoState) => {
            console.log('MatchReadyForVeto received', vetoState);
            if (window.App && typeof window.App.onMatchReadyForVeto === 'function') {
                window.App.onMatchReadyForVeto(vetoState);
            }
        });

        this.connection.on('MapVetoUpdated', (vetoState) => {
            console.log('MapVetoUpdated received', vetoState);
            if (window.App && typeof window.App.onMapVetoUpdated === 'function') {
                window.App.onMapVetoUpdated(vetoState);
            }
        });

        this.connection.on('AdminMatchesUpdated', () => {
            console.log('AdminMatchesUpdated received');
            if (window.App && typeof window.App.loadAdminData === 'function' && window.location.hash === '#admin') {
                window.App.loadAdminData();
            }
        });
        
        this.start();
    },
    
    async start() {
        try {
            await this.connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            setTimeout(() => this.start(), 5000);
        }
    }
};

window.SignalRManager = SignalRManager;
