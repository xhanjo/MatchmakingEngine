const SignalRManager = {
    connection: null,
    
    init() {
        if (!Auth.isLoggedIn()) return;
        
        const token = Auth.getToken();
        const hubUrl = 'http://localhost:5001/hubs/matchmaking';
        
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();
            
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
