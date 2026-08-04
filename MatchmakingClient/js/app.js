const App = {
    state: {
        friends: [],
        pendingRequests: [],
        party: null,
        status: 'Idle',
        matchTimer: null,
        currentMatchId: null
    },

    searchTimerInterval: null,
    searchStartTime: null,
    searchTimeout: null,

    async init() {
        Auth.requireAuth();
        SignalRManager.init();

        // Logout
        document.getElementById('logout-btn').addEventListener('click', () => Auth.logout());

        // Username display with avatar initial
        const username = Auth.getUserName() || 'Player';
        const displayEl = document.getElementById('username-display');
        displayEl.querySelector('span').innerText = username;
        displayEl.querySelector('div').innerText = username.charAt(0).toUpperCase();

        // Admin link in navbar
        if (Auth.isAdmin()) {
            const adminLink = document.createElement('a');
            adminLink.href = '#admin';
            adminLink.className = 'nav-link text-sm font-display font-semibold tracking-wider uppercase text-slate-500 hover:text-slate-300 border-b-2 border-transparent px-2 py-4 transition-colors';
            adminLink.innerText = 'Admin';
            document.getElementById('nav-links').appendChild(adminLink);
        }

        // Router
        window.addEventListener('hashchange', () => this.handleRoute());
        setTimeout(() => this.handleRoute(), 0); // initial route

        // Event listeners
        document.getElementById('find-match-btn').addEventListener('click', () => this.toggleMatchmaking());
        document.getElementById('create-party-btn').addEventListener('click', () => this.createParty());
        document.getElementById('leave-party-btn').addEventListener('click', () => this.leaveParty());
        document.getElementById('accept-match-btn').addEventListener('click', () => this.acceptMatch());
        document.getElementById('decline-match-btn').addEventListener('click', () => this.declineMatch());
        document.getElementById('player-search').addEventListener('input', (e) => this.handlePlayerSearch(e.target.value));

        await this.loadInitialData();
        setInterval(() => this.loadFriends(), 10000);
    },

    async loadInitialData() {
        await Promise.all([
            this.loadFriends(),
            this.loadParty(),
            this.updateMatchmakingStatus()
        ]);
    },

    async loadFriends() {
        try {
            const [friends, pending] = await Promise.all([
                Api.getFriends(),
                Api.getPendingFriends()
            ]);
            this.state.friends = friends || [];
            this.state.pendingRequests = pending || [];
            this.renderFriendsList();
        } catch (error) {
            console.error('Failed to load friends', error);
        }
    },

    async loadParty() {
        try {
            const party = await Api.getMyParty();
            this.state.party = party;
            this.renderParty();
        } catch (error) {
            this.state.party = null;
            this.renderParty();
        }
    },

    async updateMatchmakingStatus() {
        try {
            const status = await Api.getMatchmakingStatus();
            this.state.status = status ? status.status : 'Idle';
            if (status && status.lobbyId) {
                this.state.currentMatchId = status.lobbyId;
            }

            if (this.state.status === 'Idle' && window.location.hash === '#match-room') {
                window.location.hash = '#lobby';
            }

            this.renderMatchmakingButton();
        } catch (error) {
            console.error(error);
        }
    },

    // ─── RENDER: Friends ────────────────────────────────────────────────
    renderFriendsList() {
        const friendsList = document.getElementById('friends-list');
        const pendingList = document.getElementById('pending-requests');

        // Pending requests
        pendingList.innerHTML = '';
        if (this.state.pendingRequests.length > 0) {
            const header = document.createElement('p');
            header.className = 'pending-header';
            header.innerText = `⏳ Pending (${this.state.pendingRequests.length})`;
            pendingList.appendChild(header);

            this.state.pendingRequests.forEach(req => {
                const item = document.createElement('div');
                item.className = 'list-item';
                item.innerHTML = `
                    <div class="item-info">
                        <span class="item-name">${Utils.escapeHtml(req.senderUsername)}</span>
                        <span class="item-sub">Friend request</span>
                    </div>
                    <div class="item-actions">
                        <button class="btn btn-success btn-sm" onclick="App.acceptFriend('${req.requestId}')">✓</button>
                        <button class="btn btn-danger btn-sm" onclick="App.declineFriend('${req.requestId}')">✕</button>
                    </div>
                `;
                pendingList.appendChild(item);
            });
        }

        // Friends list
        friendsList.innerHTML = '';
        if (this.state.friends.length === 0) {
            friendsList.innerHTML = '<p style="color:#334155; font-size:0.8rem; padding: 8px 0;">No friends yet. Search for players above.</p>';
        } else {
            this.state.friends.forEach(friend => {
                const item = document.createElement('div');
                item.className = 'list-item';

                let inviteBtn = '';
                if (this.state.party && this.state.party.leaderId === Auth.getUserId() && this.state.party.members.length < 2) {
                    inviteBtn = `<button class="btn btn-primary btn-sm" onclick="App.inviteToParty('${friend.playerId}')">Invite</button>`;
                }

                item.innerHTML = `
                    <div class="item-info">
                        <span class="item-name">${Utils.escapeHtml(friend.username)}</span>
                        <span class="item-sub"><span class="mmr-badge">${friend.mmr} MMR</span></span>
                    </div>
                    <div class="item-actions">
                        ${inviteBtn}
                    </div>
                `;
                friendsList.appendChild(item);
            });
        }
    },

    // ─── RENDER: Party ───────────────────────────────────────────────────
    renderParty() {
        const partyContainer = document.getElementById('party-container');
        const createBtn = document.getElementById('create-party-btn');
        const leaveBtn = document.getElementById('leave-party-btn');
        const membersList = document.getElementById('party-members-list');
        const partyWrapper = document.getElementById('party-wrapper');

        if (!this.state.party) {
            createBtn.style.display = this.state.status !== 'Idle' ? 'none' : 'inline-flex';
            partyContainer.style.display = 'none';
            leaveBtn.style.display = 'none';
            if (partyWrapper) partyWrapper.style.display = this.state.status !== 'Idle' ? 'none' : 'block';
            this.renderMatchmakingButton();
            return;
        }

        if (partyWrapper) partyWrapper.style.display = 'block';

        if (this.state.status !== 'Idle') {
            createBtn.style.display = 'none';
            partyContainer.style.display = 'block';
            leaveBtn.style.display = 'none';
        } else {
            createBtn.style.display = 'none';
            partyContainer.style.display = 'block';
            leaveBtn.style.display = 'block';
        }

        membersList.innerHTML = '';
        this.state.party.members.forEach(m => {
            const isLeader = m.playerId === this.state.party.leaderId;
            const item = document.createElement('div');
            item.className = 'party-member';
            item.innerHTML = `
                <div>
                    ${isLeader ? '<span class="leader-crown">👑</span>' : ''}
                    <span class="item-name">${Utils.escapeHtml(m.username)}</span>
                </div>
                <span class="mmr-badge">${m.mmr} MMR</span>
            `;
            membersList.appendChild(item);
        });

        this.renderMatchmakingButton();
        this.renderFriendsList();
    },

    // ─── RENDER: Find Match Button ────────────────────────────────────────
    renderMatchmakingButton() {
        const btn = document.getElementById('find-match-btn');
        const statusText = document.getElementById('match-status-text');
        const modeText = this.state.party ? 'FIND DUO MATCH' : 'FIND MATCH';

        // Always reset class to base first
        btn.className = 'find-match-base';
        btn.disabled = false;

        if (this.state.status === 'Searching') {
            btn.classList.add('find-match-searching');
            btn.innerText = 'CANCEL SEARCH';
            if (!this.searchTimerInterval) {
                this.searchStartTime = Date.now();
                this.searchTimerInterval = setInterval(() => {
                    const elapsed = Math.floor((Date.now() - this.searchStartTime) / 1000);
                    const mins = Math.floor(elapsed / 60);
                    const secs = elapsed % 60;
                    statusText.innerText = `Searching… ${mins}:${secs.toString().padStart(2, '0')}`;
                }, 1000);
            }
        } else if (this.state.status === 'MatchFound') {
            btn.classList.add('find-match-found');
            btn.innerText = 'MATCH FOUND';
            btn.disabled = true;
            statusText.innerText = 'Waiting for all players to accept…';
        } else if (this.state.status === 'InGame' || this.state.status === 'Accepted') {
            btn.classList.add('find-match-idle');
            btn.innerText = 'MATCH IN PROGRESS';
            btn.disabled = true;
            const shortId = this.state.currentMatchId ? this.state.currentMatchId.substring(0, 8) : '?';
            statusText.innerHTML = `
                <span style="color:#10b981; font-weight:600;">● Match in progress</span>
                <span style="color:#334155; font-size:0.72rem; margin-left:8px;">ID: ${shortId}…</span>
            `;
            if (this.searchTimerInterval) {
                clearInterval(this.searchTimerInterval);
                this.searchTimerInterval = null;
            }
        } else {
            btn.classList.add('find-match-idle');
            btn.innerText = modeText;
            statusText.innerText = '';
            if (this.searchTimerInterval) {
                clearInterval(this.searchTimerInterval);
                this.searchTimerInterval = null;
            }
        }
        
        // Ensure party UI updates correctly if status changes
        if (this.state.party || !this.state.party) {
            const createBtn = document.getElementById('create-party-btn');
            const leaveBtn = document.getElementById('leave-party-btn');
            const partyContainer = document.getElementById('party-container');
            const partyWrapper = document.getElementById('party-wrapper');
            
            if (!this.state.party) {
                createBtn.style.display = this.state.status !== 'Idle' ? 'none' : 'inline-flex';
                partyContainer.style.display = 'none';
                if (partyWrapper) partyWrapper.style.display = this.state.status !== 'Idle' ? 'none' : 'block';
            } else {
                createBtn.style.display = 'none';
                partyContainer.style.display = 'block';
                leaveBtn.style.display = this.state.status !== 'Idle' ? 'none' : 'block';
                if (partyWrapper) partyWrapper.style.display = 'block';
            }
        }
    },

    // ─── ACTIONS ─────────────────────────────────────────────────────────
    async toggleMatchmaking() {
        try {
            if (this.state.status === 'Searching') {
                await Api.leaveMatchmaking();
                Utils.showToast('Left matchmaking queue');
            } else {
                await Api.joinMatchmaking();
                Utils.showToast('Joined matchmaking queue', 'success');
            }
            await this.updateMatchmakingStatus();
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    async createParty() {
        try {
            await Api.createParty();
            Utils.showToast('Duo lobby created!', 'success');
            await this.loadParty();
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    async leaveParty() {
        try {
            await Api.leaveParty();
            Utils.showToast('Left lobby');
            await this.loadParty();
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    async inviteToParty(friendId) {
        try {
            await Api.inviteToParty(friendId);
            Utils.showToast('Invite sent!', 'success');
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    async acceptFriend(requestId) {
        try {
            await Api.acceptFriendRequest(requestId);
            Utils.showToast('Friend added!', 'success');
            await this.loadFriends();
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    async declineFriend(requestId) {
        try {
            await Api.declineFriendRequest(requestId);
            await this.loadFriends();
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    async handlePlayerSearch(query) {
        clearTimeout(this.searchTimeout);
        const resultsContainer = document.getElementById('search-results');

        if (!query || query.length < 2) {
            resultsContainer.innerHTML = '';
            return;
        }

        this.searchTimeout = setTimeout(async () => {
            try {
                const players = await Api.getPlayers();
                const myId = Auth.getUserId();
                const results = players.filter(p =>
                    p.username.toLowerCase().includes(query.toLowerCase()) && p.id !== myId
                );

                resultsContainer.innerHTML = '';
                if (results.length === 0) {
                    resultsContainer.innerHTML = '<p style="color:#334155; font-size:0.75rem; padding:6px 0;">No players found</p>';
                    return;
                }

                results.slice(0, 5).forEach(p => {
                    const item = document.createElement('div');
                    item.className = 'list-item';
                    item.innerHTML = `
                        <div class="item-info">
                            <span class="item-name">${Utils.escapeHtml(p.username)}</span>
                            <span class="item-sub"><span class="mmr-badge">${p.mmr} MMR</span></span>
                        </div>
                        <div class="item-actions">
                            <button class="btn btn-primary btn-sm" onclick="App.addFriend('${p.id}')">Add</button>
                        </div>
                    `;
                    resultsContainer.appendChild(item);
                });
            } catch (error) {
                console.error(error);
            }
        }, 500);
    },

    async addFriend(playerId) {
        try {
            await Api.sendFriendRequest(playerId);
            Utils.showToast('Friend request sent!', 'success');
            document.getElementById('player-search').value = '';
            document.getElementById('search-results').innerHTML = '';
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    // ─── SIGNALR CALLBACKS ────────────────────────────────────────────────
    onMatchFound(matchId) {
        this.state.currentMatchId = matchId;
        this.state.status = 'MatchFound';
        this.renderMatchmakingButton();

        Utils.playNotificationSound();

        const modal = document.getElementById('match-modal');
        modal.classList.add('active');

        // Reset + start timer
        const circle = document.querySelector('.timer-circle .progress-stroke');
        circle.style.transition = 'none';
        circle.style.strokeDashoffset = '0';
        setTimeout(() => {
            circle.style.transition = 'stroke-dashoffset 30s linear, stroke 0.3s';
            circle.style.strokeDashoffset = '283';
        }, 50);

        let timeLeft = 30;
        document.getElementById('timer-text').innerText = timeLeft;
        clearInterval(this.matchTimer);
        this.matchTimer = setInterval(() => {
            timeLeft--;
            document.getElementById('timer-text').innerText = timeLeft;
            if (timeLeft <= 0) {
                clearInterval(this.matchTimer);
                this.closeMatchModal();
            }
        }, 1000);
    },

    onMatchCanceled() {
        Utils.showToast('Match was canceled — another player declined', 'error');
        this.closeMatchModal();
        this.updateMatchmakingStatus();
    },

    onMatchStarted(matchId) {
        this.closeMatchModal();
        this.state.status = 'InGame';
        this.state.currentMatchId = matchId;
        this.renderMatchmakingButton();

        const statusText = document.getElementById('match-status-text');
        const shortId = matchId ? matchId.substring(0, 8) : '?';
        statusText.innerHTML = `
            <span style="color:#10b981; font-weight:600;">● Match in progress</span>
            <span style="color:#334155; font-size:0.72rem; margin-left:8px;">ID: ${shortId}…</span>
        `;

        Utils.showToast('All players accepted — match is starting!', 'success');
        
        if (window.location.hash === '#match-room') {
            document.getElementById('veto-status-text').innerText = 'Server Starting...';
            document.getElementById('veto-status-text').style.color = '#10b981';
        }
    },

    onMatchFinished(result) {
        try {
            const myId = Auth.getUserId();
            const scoreboard = result.scoreboard || result.Scoreboard; // handle JSON casing differences
            
            if (!scoreboard) {
                Utils.showToast("Match finished!", "success");
                this.state.currentMatchId = null;
                this.updateMatchmakingStatus();
                return;
            }
            
            const myStats = scoreboard.find(p => {
                const pid = p.playerId || p.PlayerId;
                return pid && pid.toLowerCase() === myId.toLowerCase();
            });
            
            if (myStats) {
                const team = myStats.team || myStats.Team;
                const winningTeam = result.winningTeam || result.WinningTeam;
                const isWinner = team === winningTeam;
                
                const kills = myStats.kills || myStats.Kills || 0;
                const deaths = myStats.deaths || myStats.Deaths || 0;
                
                const msg = isWinner ? "You won!" : "You lost!";
                Utils.showToast(`Match Finished: ${msg} (Kills: ${kills}, Deaths: ${deaths})`, isWinner ? "success" : "error", 5000);
            }
            
            this.state.currentMatchId = null;
            this.updateMatchmakingStatus();
        } catch (e) {
            console.error("Error displaying match result:", e);
            Utils.showToast("Match Finished!", "success");
            this.state.currentMatchId = null;
            this.updateMatchmakingStatus();
        }
    },

    onPartyInviteReceived(partyId, senderId) {
        const modal = document.getElementById('invite-modal');
        modal.classList.add('active');
        document.getElementById('invite-text').innerText = 'You have received a party invite!';

        document.getElementById('accept-invite-btn').onclick = async () => {
            try {
                await Api.joinParty(partyId);
                Utils.showToast('Joined party!', 'success');
                modal.classList.remove('active');
                await this.loadParty();
            } catch (error) {
                Utils.showToast(error.message, 'error');
                modal.classList.remove('active');
            }
        };

        document.getElementById('decline-invite-btn').onclick = () => {
            modal.classList.remove('active');
        };
    },

    onPlayerJoinedParty(playerId) {
        Utils.showToast('A player joined your lobby!', 'info');
        this.loadParty();
    },

    // ─── MATCH ACCEPT / DECLINE ───────────────────────────────────────────
    async acceptMatch() {
        if (!this.state.currentMatchId) return;
        const btn = document.getElementById('accept-match-btn');
        const declineBtn = document.getElementById('decline-match-btn');

        // Immediate visual feedback before API call
        btn.innerText = '✓  ACCEPTED';
        btn.style.cssText = 'width:100%; padding:16px; border-radius:12px; font-family:Outfit; font-weight:900; font-size:1.125rem; text-transform:uppercase; letter-spacing:0.1em; color:white; border:none; cursor:default; background:#047857; box-shadow:0 0 25px rgba(16,185,129,0.35);';
        btn.disabled = true;
        declineBtn.disabled = true;

        const titleEl = document.getElementById('match-modal').querySelector('.match-found-title');
        if (titleEl) {
            titleEl.innerText = 'WAITING FOR OTHERS…';
            titleEl.style.color = '#f59e0b';
            titleEl.style.textShadow = '0 0 20px rgba(245,158,11,0.5)';
        }

        try {
            await Api.acceptMatch(this.state.currentMatchId);
        } catch (error) {
            Utils.showToast(error.message, 'error');
            this.closeMatchModal();
        }
    },

    async declineMatch() {
        if (!this.state.currentMatchId) return;
        try {
            await Api.declineMatch(this.state.currentMatchId);
            this.closeMatchModal();
        } catch (error) {
            Utils.showToast(error.message, 'error');
            this.closeMatchModal();
        }
    },

    closeMatchModal() {
        clearInterval(this.matchTimer);
        document.getElementById('match-modal').classList.remove('active');

        setTimeout(() => {
            const titleEl = document.getElementById('match-modal').querySelector('.match-found-title');
            if (titleEl) {
                titleEl.innerText = 'MATCH FOUND';
                titleEl.style.color = '';
                titleEl.style.textShadow = '';
            }
            const btn = document.getElementById('accept-match-btn');
            btn.innerText = 'ACCEPT';
            btn.style.cssText = 'background:#059669; box-shadow:0 0 25px rgba(16,185,129,0.4); width:100%; padding:16px; border-radius:12px; font-family:Outfit; font-weight:900; font-size:1.125rem; text-transform:uppercase; letter-spacing:0.1em; color:white; border:none; cursor:pointer;';
            document.getElementById('accept-match-btn').disabled = false;
            document.getElementById('decline-match-btn').disabled = false;
        }, 300);

        this.state.currentMatchId = null;
        this.updateMatchmakingStatus();
    },

    // ─── PROFILE MODAL ────────────────────────────────────────────────────
    async showProfile() {
        try {
            const userId = Auth.getUserId();
            const [player, history] = await Promise.all([
                Api.getPlayer(userId),
                Api.getMyHistory()
            ]);

            const regions = { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };

            document.getElementById('profile-stats').innerHTML = `
                <div style="display:flex; align-items:center; gap:16px; margin-bottom:16px;">
                    <div style="width:52px; height:52px; border-radius:50%; background: linear-gradient(135deg, #6366f1, #4f46e5); display:flex; align-items:center; justify-content:center; font-family:Outfit; font-size:1.4rem; font-weight:900; color:white; flex-shrink:0;">
                        ${(player.username || 'P').charAt(0).toUpperCase()}
                    </div>
                    <div>
                        <div style="font-family:Outfit; font-weight:700; font-size:1.1rem; color:#e2e8f0;">${Utils.escapeHtml(player.username)}</div>
                        <div style="font-size:0.75rem; color:#475569; margin-top:2px;">
                            ${regions[player.region] || 'Unknown'} &nbsp;•&nbsp; Joined ${new Date(player.createdAt).toLocaleDateString()}
                        </div>
                    </div>
                </div>
                <div style="display:grid; grid-template-columns:1fr 1fr; gap:10px;">
                    <div style="background:#131825; border:1px solid #1c2035; border-radius:10px; padding:12px 16px;">
                        <div style="font-size:0.7rem; text-transform:uppercase; letter-spacing:0.08em; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:4px;">MMR</div>
                        <div style="font-family:Outfit; font-weight:900; font-size:1.5rem; color:#22d3ee;">${player.mmr}</div>
                    </div>
                    <div style="background:#131825; border:1px solid #1c2035; border-radius:10px; padding:12px 16px;">
                        <div style="font-size:0.7rem; text-transform:uppercase; letter-spacing:0.08em; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:4px;">Trust Factor</div>
                        <div style="font-family:Outfit; font-weight:900; font-size:1.5rem; color:${player.trustFactor >= 0.7 ? '#10b981' : player.trustFactor >= 0.4 ? '#f59e0b' : '#ef4444'};">
                            ${(player.trustFactor * 100).toFixed(0)}%
                        </div>
                    </div>
                </div>
            `;

            const historyContainer = document.getElementById('profile-history');

            // MatchStatus enum: Pending=0, Accepted=1, Canceled=2, Finished=3
            // The endpoint already filters to Finished only, but handle edge cases
            console.log('History data:', history, 'UserId:', userId);
            const finishedMatches = (history || []).filter(m =>
                m.status === 'Finished' || m.status === 6
            ).sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
            console.log('Finished matches:', finishedMatches);

            if (finishedMatches.length === 0) {
                historyContainer.innerHTML = '<p style="color:#334155; font-size:0.8rem;">No completed matches yet.</p>';
            } else {
                historyContainer.innerHTML = finishedMatches.map(match => {
                    const myStats = match.players?.find(p => {
                        const pid = p.playerId || p.PlayerId;
                        return pid && pid.toLowerCase() === userId.toLowerCase();
                    });
                    console.log('Match:', match.id, 'Players:', match.players, 'MyStats:', myStats);
                    if (!myStats) return '';

                    const modeText = (match.gameMode === 1 || match.gameMode === 'Solo') ? 'Solo' : 'Duo';
                    const isMvp = myStats.isMvp;
                    const isWinner = myStats.isWinner; // Using the new boolean from backend

                    const accentColor = isWinner ? '#10b981' : '#ef4444'; // Green for win, Red for loss
                    const outcomeText = isWinner ? 'WIN' : 'LOSS';

                    return `
                        <div style="background:#0d1018; border:1px solid #1c2035; border-left:3px solid ${accentColor}; border-radius:10px; padding:13px 16px; margin-bottom:8px;">
                            <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:8px;">
                                <div style="display:flex; align-items:center; gap:8px;">
                                    <span style="font-family:Outfit; font-weight:700; font-size:0.85rem; color:#e2e8f0;">${modeText}</span>
                                    <span style="background:${accentColor}15; color:${accentColor}; border:1px solid ${accentColor}30; font-family:Outfit; font-weight:700; font-size:0.65rem; padding:1px 7px; border-radius:10px;">${outcomeText}</span>
                                    ${isMvp ? '<span style="background:rgba(251,191,36,0.1); border:1px solid rgba(251,191,36,0.3); color:#fbbf24; font-family:Outfit; font-weight:700; font-size:0.65rem; padding:1px 7px; border-radius:10px;">⭐ MVP</span>' : ''}
                                </div>
                                <span style="font-size:0.7rem; color:#334155;">${new Date(match.createdAt).toLocaleDateString()}</span>
                            </div>
                            <div style="display:grid; grid-template-columns:1fr 1fr; gap:6px; text-align:center;">
                                <div style="background:#131825; border-radius:6px; padding:6px;">
                                    <div style="font-size:0.65rem; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:2px;">K/D/A</div>
                                    <div style="font-size:0.85rem; color:#e2e8f0; font-weight:600;">${myStats.kills}/${myStats.deaths}/${myStats.assists}</div>
                                </div>
                                <div style="background:#131825; border-radius:6px; padding:6px;">
                                    <div style="font-size:0.65rem; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:2px;">SCORE</div>
                                    <div style="font-size:0.85rem; color:#22d3ee; font-weight:600;">${myStats.score}</div>
                                </div>
                            </div>
                        </div>
                    `;
                }).join('');
            }

            document.getElementById('profile-modal').classList.add('active');
        } catch (error) {
            Utils.showToast('Failed to load profile', 'error');
            console.error(error);
        }
    },

    // ─── ROUTER ────────────────────────────────────────────────────────
    handleRoute() {
        const hash = window.location.hash || '#lobby';
        const views = ['view-lobby', 'view-leaderboard', 'view-admin', 'view-match-room'];
        
        views.forEach(v => {
            const el = document.getElementById(v);
            if (el) {
                el.classList.add('hidden');
                el.classList.remove('flex');
            }
        });

        document.querySelectorAll('#nav-links .nav-link').forEach(el => {
            el.classList.remove('text-white', 'border-brand');
            el.classList.add('text-slate-500', 'border-transparent');
            if (el.getAttribute('href') === hash) {
                el.classList.remove('text-slate-500', 'border-transparent');
                el.classList.add('text-white', 'border-brand');
            }
        });

        if (hash === '#lobby') {
            const v = document.getElementById('view-lobby');
            v.classList.remove('hidden');
            v.classList.add('flex');
        } else if (hash === '#leaderboard') {
            document.getElementById('view-leaderboard').classList.remove('hidden');
            this.loadLeaderboard();
        } else if (hash === '#admin') {
            if (Auth.isAdmin()) {
                document.getElementById('view-admin').classList.remove('hidden');
                this.loadAdminData();
            } else {
                window.location.hash = '#lobby';
            }
        } else if (hash === '#match-room') {
            const v = document.getElementById('view-match-room');
            v.classList.remove('hidden');
            v.classList.add('flex');
        }
    },

    // ─── LEADERBOARD ──────────────────────────────────────────────────
    async loadLeaderboard() {
        try {
            const tbody = document.getElementById('leaderboard-tbody');
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-slate-600 py-8">Loading...</td></tr>';
            const data = await Api.getLeaderboard();
            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-slate-600 py-8">No players found</td></tr>';
                return;
            }
            
            const regions = { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };
            tbody.innerHTML = data.map((p, index) => `
                <tr style="border-bottom:1px solid #1c2035;" onmouseover="this.style.background='#131825'" onmouseout="this.style.background=''">
                    <td style="text-align:center; padding:14px 16px; font-family:Outfit; font-weight:900; color:#94a3b8; width:60px;">#${index + 1}</td>
                    <td style="padding:14px 16px; font-family:Outfit; font-weight:700; color:white;">${Utils.escapeHtml(p.username)}</td>
                    <td style="padding:14px 16px; color:#64748b; font-size:0.875rem;">${regions[p.region] || 'Unknown'}</td>
                    <td style="text-align:right; padding:14px 16px; width:130px;"><span class="mmr-badge">${p.mmr} MMR</span></td>
                </tr>
            `).join('');
        } catch (error) {
            console.error('Failed to load leaderboard', error);
            document.getElementById('leaderboard-tbody').innerHTML = '<tr><td colspan="4" class="text-center text-danger py-8">Failed to load</td></tr>';
        }
    },

    // ─── ADMIN ────────────────────────────────────────────────────────
    async loadAdminData() {
        try {
            const [activeMatches, allMatches, players] = await Promise.all([
                Api.getActiveMatches(),
                Api.getAllMatches(),
                Api.getPlayers()
            ]);

            this.renderAdminMatchTable('active-matches-tbody', activeMatches, true);
            this.renderAdminMatchTable('all-matches-tbody', allMatches, false);
            this.renderAdminPlayersTable(players);

            const countEl = document.getElementById('all-matches-count');
            if (countEl) countEl.innerText = `(${allMatches?.length ?? 0})`;
        } catch (error) {
            Utils.showToast('Failed to load admin data', 'error');
            console.error(error);
        }
    },

    renderAdminPlayersTable(players) {
        const tbody = document.getElementById('players-tbody');
        if (!players || players.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-slate-600 py-8">No players registered</td></tr>';
            return;
        }

        const regions = { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };
        const roles   = { 0: 'Player',  1: 'Admin',   2: 'SuperAdmin' };
        const roleColors = { 0: '#64748b', 1: '#f59e0b', 2: '#ef4444' };

        tbody.innerHTML = players.map(p => `
            <tr class="border-b border-app-border last:border-0 hover:bg-app-elevated transition-colors">
                <td class="font-medium text-slate-200 py-4 px-4">${Utils.escapeHtml(p.username)}</td>
                <td class="py-4 px-4">
                    <span style="padding:4px 10px; border-radius:6px; font-size:0.7rem; font-family:Outfit; font-weight:700; letter-spacing:0.05em; background:${roleColors[p.role] || '#64748b'}15; color:${roleColors[p.role] || '#64748b'}; border:1px solid ${roleColors[p.role] || '#64748b'}30;">
                        ${roles[p.role] || 'Player'}
                    </span>
                </td>
                <td class="text-slate-500 py-4 px-4">${regions[p.region] || 'Unknown'}</td>
                <td class="py-4 px-4"><span class="mmr-badge">${p.mmr} MMR</span></td>
                <td class="py-4 px-4">
                    <div class="flex items-center gap-2">
                        <div class="flex-1 h-1.5 bg-app-border rounded-full max-w-[60px] overflow-hidden">
                            <div class="h-full rounded-full" style="width:${Math.round(p.trustFactor * 100)}%; background:${p.trustFactor >= 0.7 ? '#10b981' : p.trustFactor >= 0.4 ? '#f59e0b' : '#ef4444'};"></div>
                        </div>
                        <span class="text-xs text-slate-500 font-display">${(p.trustFactor * 100).toFixed(0)}%</span>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    renderAdminMatchTable(tbodyId, matches, isActive) {
        const tbody = document.getElementById(tbodyId);
        if (!tbody) return;

        if (!matches || matches.length === 0) {
            tbody.innerHTML = `<tr><td colspan="7" class="text-center text-slate-600 py-8">No matches found</td></tr>`;
            return;
        }

        const statusColors = { 'Pending': '#f59e0b', 'Accepted': '#6366f1', 'Finished': '#10b981', 'Canceled': '#475569' };

        tbody.innerHTML = matches.map(match => {
            const createdAt = new Date(match.createdAt).toLocaleString();
            const avgMmr = match.averageMmr ?? '-';
            const mode = (match.gameMode === 1 || match.gameMode === 'Solo') ? 'Solo' : 'Duo';
            const statusColor = statusColors[match.status] || '#64748b';

            let actionHtml = '<span class="text-slate-500">—</span>';
            if (match.status === 'Accepted' || match.status === 'StartingServer' || match.status === 'MapVeto') {
                actionHtml = `<button class="btn btn-primary btn-sm" onclick="App.completeAdminMatch('${match.id}')">Complete</button>`;
            }

            return `
                <tr class="border-b border-app-border last:border-0 hover:bg-app-elevated transition-colors">
                    <td class="font-mono text-xs text-slate-500 py-4 px-4">${match.id.substring(0, 12)}…</td>
                    <td class="py-4 px-4">
                        <span style="padding:4px 10px; border-radius:20px; font-size:0.7rem; font-family:Outfit; font-weight:700; letter-spacing:0.05em; background:${statusColor}15; color:${statusColor}; border:1px solid ${statusColor}30;">
                            ${match.status}
                        </span>
                    </td>
                    <td class="py-4 px-4"><span class="mmr-badge">${avgMmr}</span></td>
                    <td class="text-slate-400 text-sm py-4 px-4">${mode}</td>
                    <td class="text-xs text-slate-500 py-4 px-4">${createdAt}</td>
                    <td class="text-slate-500 py-4 px-4 font-display">${match.playerCount ?? '?'}</td>
                    <td class="py-4 px-4">${actionHtml}</td>
                </tr>
            `;
        }).join('');
    },

    async completeAdminMatch(matchId) {
        try {
            await Api.completeMatch(matchId);
            Utils.showToast('Match completed successfully!', 'success');
            await this.loadAdminData();
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    // ─── MAP VETO ─────────────────────────────────────────────────────
    onMatchReadyForVeto(vetoState) {
        console.log('Veto ready!', vetoState);
        this.closeMatchModal();
        this.state.status = 'Veto';
        this.state.currentMatchId = vetoState.matchId;
        this.state.vetoState = vetoState;
        
        window.location.hash = '#match-room';
        
        const team1 = vetoState.team1[0]?.username || 'Team 1';
        const team2 = vetoState.team2[0]?.username || 'Team 2';
        document.querySelector('#veto-team1 .team-name').innerText = team1;
        document.querySelector('#veto-team2 .team-name').innerText = team2;
        
        this.renderMapVetoUI();
        Utils.showToast('Map veto phase has started!', 'info');
    },

    onMapVetoUpdated(vetoState) {
        console.log('Map Veto Updated!', vetoState);
        this.state.vetoState = vetoState;
        this.renderMapVetoUI();
        
        // Find last banned map to add to log
        const logContainer = document.getElementById('veto-log');
        if (logContainer) {
            const lastMap = vetoState.maps.find(m => m.isBanned && !this._loggedBans?.has(m.name));
            if (lastMap) {
                this._loggedBans = this._loggedBans || new Set();
                this._loggedBans.add(lastMap.name);
                
                const logItem = document.createElement('div');
                logItem.className = 'text-danger font-display text-xs p-1 bg-danger/10 rounded border border-danger/20 mb-1 animate-pulse';
                logItem.innerText = `${lastMap.bannedByUsername || 'Player'} banned ${lastMap.name}`;
                logContainer.appendChild(logItem);
                logContainer.scrollTop = logContainer.scrollHeight;
            }
        }
    },

    onMatchStarting(matchId) {
        console.log('Match Starting:', matchId);
        if (window.location.hash === '#match-room') {
            document.getElementById('veto-status-text').innerText = 'Server Starting...';
            document.getElementById('veto-status-text').style.color = '#10b981';
        }
    },

    renderMapVetoUI() {
        const v = this.state.vetoState;
        if (!v) return;

        const myId = Auth.getUserId();
        
        let statusText = 'Waiting...';
        if (v.status === 'InProgress' || v.status === 0) { // InProgress
            const isMyTurn = (v.currentTurnTeam === 1 && v.team1.some(p => p.playerId === myId)) || 
                             (v.currentTurnTeam === 2 && v.team2.some(p => p.playerId === myId));
            statusText = isMyTurn ? 'YOUR TURN TO VETO' : "OPPONENT'S TURN";
            document.getElementById('veto-status-text').style.color = isMyTurn ? '#10b981' : '#f59e0b';
        } else if (v.status === 'Completed' || v.status === 1) { // Completed
            statusText = 'MAP SELECTED!';
            document.getElementById('veto-status-text').style.color = '#6366f1';
        }

        document.getElementById('veto-status-text').innerText = statusText;
        
        // Turn indicator styles
        const t1 = document.getElementById('veto-team1');
        const t2 = document.getElementById('veto-team2');
        if (v.currentTurnTeam === 1 && (v.status === 'InProgress' || v.status === 0)) {
            t1.classList.add('opacity-100'); t1.classList.remove('opacity-40');
            t2.classList.add('opacity-40'); t2.classList.remove('opacity-100');
        } else if (v.currentTurnTeam === 2 && (v.status === 'InProgress' || v.status === 0)) {
            t1.classList.add('opacity-40'); t1.classList.remove('opacity-100');
            t2.classList.add('opacity-100'); t2.classList.remove('opacity-40');
        } else {
            t1.classList.add('opacity-100'); t2.classList.add('opacity-100');
            t1.classList.remove('opacity-40'); t2.classList.remove('opacity-40');
        }
        
        const mapsGrid = document.getElementById('veto-maps-grid');
        mapsGrid.innerHTML = v.maps.map(m => {
            const isBanned = m.isBanned;
            const isSelected = (v.status === 'Completed' || v.status === 1) && !isBanned;
            let styles = "relative rounded-xl overflow-hidden cursor-pointer transition-all border-2 ";
            let innerStyles = "";
            let overlay = "";

            if (isBanned) {
                styles += "border-danger/30 opacity-50 grayscale";
                overlay = `<div class="absolute inset-0 bg-danger/20 flex flex-col items-center justify-center">
                    <span class="text-4xl drop-shadow-md">❌</span>
                </div>`;
            } else if (isSelected) {
                styles += "border-safe transform scale-[1.03] shadow-[0_0_25px_rgba(16,185,129,0.5)] z-10";
                overlay = `<div class="absolute inset-0 bg-safe/10 flex flex-col items-center justify-center">
                    <span class="text-4xl drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]">⚔️</span>
                    <span class="font-display font-black text-white bg-black/50 px-3 py-1 rounded text-xs mt-2 uppercase tracking-widest text-safe border border-safe/30 backdrop-blur-sm shadow-xl">Selected Map</span>
                </div>`;
            } else {
                styles += "border-app-border hover:border-brand hover:-translate-y-1";
                innerStyles = "background: linear-gradient(0deg, rgba(0,0,0,0.8) 0%, rgba(0,0,0,0) 100%);";
            }

            const mapNamesMap = {
                'Mirage': 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?q=80&w=400&h=300&auto=format&fit=crop',
                'Inferno': 'https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=400&h=300&auto=format&fit=crop',
                'Dust2': 'https://images.unsplash.com/photo-1627850854446-c22bebb6e3ad?q=80&w=400&h=300&auto=format&fit=crop',
                'Overpass': 'https://images.unsplash.com/photo-1498084393753-b411b2d26b34?q=80&w=400&h=300&auto=format&fit=crop',
                'Nuke': 'https://images.unsplash.com/photo-1550751827-4bd374c3f58b?q=80&w=400&h=300&auto=format&fit=crop',
                'Vertigo': 'https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?q=80&w=400&h=300&auto=format&fit=crop',
                'Ancient': 'https://images.unsplash.com/photo-1590845947376-2638caa89309?q=80&w=400&h=300&auto=format&fit=crop'
            };
            const bgUrl = mapNamesMap[m.name] || 'https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=400&h=300&auto=format&fit=crop'; 

            return `
                <div class="${styles}" onclick="App.vetoMapClick('${m.name}')" style="aspect-ratio: 4/3; background-image: url('${bgUrl}'); background-size: cover; background-position: center;">
                    ${overlay}
                    <div class="absolute inset-0 flex items-end p-4" style="${innerStyles}">
                        <span class="font-display font-black text-xl text-white uppercase tracking-wider drop-shadow-md">${m.name}</span>
                    </div>
                </div>
            `;
        }).join('');
    },

    async vetoMapClick(mapName) {
        if (!this.state.vetoState || (this.state.vetoState.status !== 'InProgress' && this.state.vetoState.status !== 0)) return;
        try {
            await Api.vetoMap(this.state.currentMatchId, mapName);
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    }
};

window.App = App;
document.addEventListener('DOMContentLoaded', () => {
    const path = window.location.pathname;
    if (path.endsWith('index.html') || path === '/' || path.endsWith('MatchmakingClient/') || path.endsWith('MatchmakingClient')) {
        App.init();
    }
});
