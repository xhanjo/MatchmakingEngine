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

        await this.loadInitialData();

        // Optimized intervals (SignalR handles real-time events; these are fallbacks)
        this._pollingIntervals = [
            setInterval(() => this.loadFriends(), 45000),
            setInterval(() => this.updateMatchmakingStatus(), 15000)
        ];

        // Refresh on window focus when user returns to tab
        window.addEventListener('focus', () => {
            if (Auth.isLoggedIn()) {
                this.loadFriends();
                this.updateMatchmakingStatus();
            }
        });
    },

    async loadInitialData() {
        await Promise.all([
            this.loadFriends(),
            this.loadParty(),
            this.updateMatchmakingStatus(),
            this.loadMyMmr()
        ]);
    },

    async loadMyMmr() {
        try {
            const player = await Api.getPlayer(Auth.getUserId());
            const displayEl = document.getElementById('nav-mmr-display');
            if (displayEl && player) {
                displayEl.innerText = `${player.mmr} MMR`;
            }
        } catch (error) {
            console.error('Failed to load my MMR', error);
        }
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
            this.state.status = status ? (status.status || status.Status || 'Idle') : 'Idle';
            const lobbyId = status?.lobbyId || status?.LobbyId || status?.vetoState?.matchId || status?.VetoState?.MatchId;
            if (lobbyId) {
                this.state.currentMatchId = lobbyId;
            }

            // If already actively inside the match room during Veto phase, let SignalR handle real-time events
            if (this.state.status === 'Veto' && window.location.hash === '#match-room') {
                return;
            }

            const vetoState = status?.vetoState || status?.VetoState;
            if (this.state.status === 'MapVeto' && vetoState) {
                this.state.status = 'Veto';
                this.state.vetoState = vetoState;
                if (vetoState.matchId || vetoState.MatchId) {
                    this.state.currentMatchId = vetoState.matchId || vetoState.MatchId;
                }
                
                const currentHash = window.location.hash;
                if (currentHash !== '#match-room') {
                    window.location.hash = '#match-room';
                }

                const team1 = (vetoState.team1 || vetoState.Team1 || [])[0]?.username || (vetoState.team1 || vetoState.Team1 || [])[0]?.Username || 'Team 1';
                const team2 = (vetoState.team2 || vetoState.Team2 || [])[0]?.username || (vetoState.team2 || vetoState.Team2 || [])[0]?.Username || 'Team 2';
                const t1 = document.querySelector('#veto-team1 .team-name');
                const t2 = document.querySelector('#veto-team2 .team-name');
                if (t1) t1.innerText = team1;
                if (t2) t2.innerText = team2;
                
                this.renderMapVetoUI();
            } else if (this.state.status === 'Idle' && window.location.hash === '#match-room') {
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
            header.innerText = `Pending (${this.state.pendingRequests.length})`;
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
                        <button class="btn btn-primary btn-sm" onclick="App.acceptFriend('${req.requestId}')">Accept</button>
                        <button class="btn btn-secondary btn-sm" onclick="App.declineFriend('${req.requestId}')">Decline</button>
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

                item.style.cssText = 'display:flex; align-items:center; justify-content:space-between; padding:10px 12px; margin-bottom:6px; border-radius:8px; background:rgba(255,255,255,0.03); border:1px solid rgba(255,255,255,0.06);';
                item.innerHTML = `
                    <div style="display:flex; align-items:center; gap:10px; min-width:0;">
                        <span class="item-name" style="font-weight:600; white-space:nowrap;">${Utils.escapeHtml(friend.username)}</span>
                        <span class="mmr-badge" style="font-size:0.7rem; padding:2px 8px; border-radius:4px; white-space:nowrap;">${friend.mmr} MMR</span>
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
                    ${isLeader ? '<span class="leader-crown text-amber-400 font-bold text-xs mr-1">[Leader]</span>' : ''}
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
            const shortId = this.state.currentMatchId ? Utils.escapeHtml(this.state.currentMatchId.substring(0, 8)) : '?';
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
        const createBtn = document.getElementById('create-party-btn');
        const leaveBtn = document.getElementById('leave-party-btn');
        const partyContainer = document.getElementById('party-container');
        const partyWrapper = document.getElementById('party-wrapper');
        
        if (!this.state.party) {
            if (createBtn) createBtn.style.display = this.state.status !== 'Idle' ? 'none' : 'inline-flex';
            if (partyContainer) partyContainer.style.display = 'none';
            if (partyWrapper) partyWrapper.style.display = this.state.status !== 'Idle' ? 'none' : 'block';
        } else {
            if (createBtn) createBtn.style.display = 'none';
            if (partyContainer) partyContainer.style.display = 'block';
            if (leaveBtn) leaveBtn.style.display = this.state.status !== 'Idle' ? 'none' : 'block';
            if (partyWrapper) partyWrapper.style.display = 'block';
        }
    },

    // ─── ACTIONS ─────────────────────────────────────────────────────────
    async toggleMatchmaking() {
        if (this._isTogglingMatchmaking) return;
        this._isTogglingMatchmaking = true;
        const btn = document.getElementById('find-match-btn');
        if (btn) btn.disabled = true;

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
        } finally {
            this._isTogglingMatchmaking = false;
            if (btn) btn.disabled = false;
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
                    item.style.cssText = 'display:flex; align-items:center; justify-content:space-between; padding:10px 12px; margin-bottom:6px; border-radius:8px; background:rgba(255,255,255,0.03); border:1px solid rgba(255,255,255,0.06);';
                    item.innerHTML = `
                        <div style="display:flex; align-items:center; gap:10px; min-width:0;">
                            <span class="item-name" style="font-weight:600; white-space:nowrap;">${Utils.escapeHtml(p.username)}</span>
                            <span class="mmr-badge" style="font-size:0.7rem; padding:2px 8px; border-radius:4px; white-space:nowrap;">${p.mmr} MMR</span>
                        </div>
                        <button class="btn btn-primary btn-sm" style="margin-left:12px; padding:4px 14px; font-size:0.75rem; white-space:nowrap;" onclick="App.addFriend('${p.id}')">Add</button>
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
        const shortId = matchId ? Utils.escapeHtml(matchId.substring(0, 8)) : '?';
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
                this.state.status = 'Idle';
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
                const mmrChange = myStats.mmrChange || myStats.MmrChange || 0;
                const mmrStr = mmrChange >= 0 ? `+${mmrChange}` : `${mmrChange}`;
                
                const msg = isWinner ? "You won!" : "You lost!";
                Utils.showToast(`Match Finished: ${msg} K:${kills} D:${deaths} MMR: ${mmrStr}`, isWinner ? "success" : "error", 5000);
            }
            
            this.state.currentMatchId = null;
            this.state.status = 'Idle';
            this.stopVetoTimer();
            
            // Navigate back to lobby and force route refresh
            window.location.hash = '#lobby';
            this.handleRoute();
            this.loadLeaderboard();
            this.loadMyMmr();
            this.updateMatchmakingStatus();
        } catch (e) {
            console.error("Error displaying match result:", e);
            Utils.showToast("Match Finished!", "success");
            this.state.currentMatchId = null;
            this.updateMatchmakingStatus();
            window.location.hash = '#lobby';
            this.handleRoute();
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
        btn.innerText = 'ACCEPTED';
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
            const [player, history, leaderboard] = await Promise.all([
                Api.getPlayer(userId),
                Api.getMyHistory(),
                Api.getLeaderboard().catch(() => [])
            ]);

            const regions = window.APP_CONFIG?.REGIONS || { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };

            // Determine leaderboard position
            let leaderboardRankText = 'Unranked';
            let leaderboardRankColor = '#94a3b8';
            let rankBadgeHtml = '';

            if (Array.isArray(leaderboard) && leaderboard.length > 0) {
                const rankIndex = leaderboard.findIndex(p => {
                    const pid = p.playerId || p.PlayerId || p.id || '';
                    return pid.toLowerCase() === userId.toLowerCase();
                });

                if (rankIndex !== -1) {
                    const rank = rankIndex + 1;
                    leaderboardRankText = `#${rank}`;
                    leaderboardRankColor = rank <= 3 ? '#fbbf24' : '#818cf8';
                    rankBadgeHtml = `<span style="background:rgba(251,191,36,0.12); color:#fbbf24; border:1px solid rgba(251,191,36,0.3); font-family:Outfit; font-weight:800; font-size:0.7rem; padding:2px 8px; border-radius:12px; margin-left:8px;">Rank #${rank}</span>`;
                } else if (player && player.mmr) {
                    leaderboardRankText = 'Top 100+';
                }
            }

            document.getElementById('profile-stats').innerHTML = `
                <div style="display:flex; align-items:center; gap:16px; margin-bottom:16px;">
                    <div style="width:52px; height:52px; border-radius:50%; background: linear-gradient(135deg, #6366f1, #4f46e5); display:flex; align-items:center; justify-content:center; font-family:Outfit; font-size:1.4rem; font-weight:900; color:white; flex-shrink:0;">
                        ${(player.username || 'P').charAt(0).toUpperCase()}
                    </div>
                    <div>
                        <div style="display:flex; align-items:center;">
                            <span style="font-family:Outfit; font-weight:700; font-size:1.1rem; color:#e2e8f0;">${Utils.escapeHtml(player.username)}</span>
                            ${rankBadgeHtml}
                        </div>
                        <div style="font-size:0.75rem; color:#475569; margin-top:2px;">
                            ${regions[player.region] || 'Unknown'} &nbsp;•&nbsp; Joined ${new Date(player.createdAt).toLocaleDateString()}
                        </div>
                    </div>
                </div>
                <div style="display:grid; grid-template-columns:1fr 1fr 1fr; gap:8px;">
                    <div style="background:#131825; border:1px solid #1c2035; border-radius:10px; padding:12px 14px;">
                        <div style="font-size:0.7rem; text-transform:uppercase; letter-spacing:0.08em; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:4px;">MMR</div>
                        <div style="font-family:Outfit; font-weight:900; font-size:1.4rem; color:#22d3ee;">${player.mmr}</div>
                    </div>
                    <div style="background:#131825; border:1px solid #1c2035; border-radius:10px; padding:12px 14px;">
                        <div style="font-size:0.7rem; text-transform:uppercase; letter-spacing:0.08em; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:4px;">Rank</div>
                        <div style="font-family:Outfit; font-weight:900; font-size:1.4rem; color:${leaderboardRankColor};">${leaderboardRankText}</div>
                    </div>
                    <div style="background:#131825; border:1px solid #1c2035; border-radius:10px; padding:12px 14px;">
                        <div style="font-size:0.7rem; text-transform:uppercase; letter-spacing:0.08em; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:4px;">Trust Factor</div>
                        <div style="font-family:Outfit; font-weight:900; font-size:1.4rem; color:${player.trustFactor >= 0.7 ? '#10b981' : player.trustFactor >= 0.4 ? '#f59e0b' : '#ef4444'};">
                            ${(player.trustFactor * 100).toFixed(0)}%
                        </div>
                    </div>
                </div>
            `;

            const historyContainer = document.getElementById('profile-history');

            // MatchStatus enum: Pending=0, Accepted=1, Canceled=2, Finished=3
            // The endpoint already filters to Finished only, but handle edge cases
            const finishedMatches = (history || []).filter(m =>
                m.status === 'Finished' || m.status === 6
            ).sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

            if (finishedMatches.length === 0) {
                historyContainer.innerHTML = '<p style="color:#334155; font-size:0.8rem;">No completed matches yet.</p>';
            } else {
                historyContainer.innerHTML = finishedMatches.map(match => {
                    const myStats = match.players?.find(p => {
                        const pid = p.playerId || p.PlayerId;
                        return pid && pid.toLowerCase() === userId.toLowerCase();
                    });
                    if (!myStats) return '';

                    const modeText = (match.gameMode === 1 || match.gameMode === 'Solo') ? 'Solo' : 'Duo';
                    const isMvp = myStats.isMvp;
                    const isWinner = myStats.isWinner;

                    const accentColor = isWinner ? '#10b981' : '#ef4444'; // Green for win, Red for loss
                    const outcomeText = isWinner ? 'WIN' : 'LOSS';

                    // Determine Elo MMR change
                    let mmrChange = null;
                    if (typeof myStats.mmrChange === 'number' && myStats.mmrChange !== 0) {
                        mmrChange = myStats.mmrChange;
                    } else if (typeof myStats.MmrChange === 'number' && myStats.MmrChange !== 0) {
                        mmrChange = myStats.MmrChange;
                    } else {
                        // Dynamic Elo calculation fallback (matches backend algorithm)
                        const players = match.players || [];
                        const team1Players = players.filter(p => (p.team ?? p.Team) === 1);
                        const team2Players = players.filter(p => (p.team ?? p.Team) === 2);
                        const myTeam = myStats.team ?? myStats.Team ?? 1;

                        const team1AvgMmr = team1Players.length > 0
                            ? (team1Players.reduce((acc, p) => acc + (p.mmr ?? p.Mmr ?? player?.mmr ?? 1000), 0) / team1Players.length)
                            : (player?.mmr ?? 1000);
                        const team2AvgMmr = team2Players.length > 0
                            ? (team2Players.reduce((acc, p) => acc + (p.mmr ?? p.Mmr ?? player?.mmr ?? 1000), 0) / team2Players.length)
                            : (player?.mmr ?? 1000);

                        const myTeamAvg = myTeam === 1 ? team1AvgMmr : team2AvgMmr;
                        const oppTeamAvg = myTeam === 1 ? team2AvgMmr : team1AvgMmr;

                        const expectedScore = 1.0 / (1.0 + Math.pow(10.0, (oppTeamAvg - myTeamAvg) / 400.0));
                        const actualScore = isWinner ? 1.0 : 0.0;
                        const kFactor = 50;
                        mmrChange = Math.round(kFactor * (actualScore - expectedScore));
                    }

                    const mmrChangeStr = mmrChange > 0 ? `+${mmrChange}` : `${mmrChange}`;
                    const mmrChangeColor = mmrChange > 0 ? '#10b981' : (mmrChange < 0 ? '#ef4444' : '#94a3b8');

                    const playerMmr = (myStats.mmr ?? myStats.Mmr) || player?.mmr;
                    let ratingDisplay = `${mmrChangeStr} MMR`;
                    if (playerMmr) {
                        ratingDisplay = `${playerMmr} (${mmrChangeStr})`;
                    }

                    const mmrBadgeHtml = `
                        <span style="background:${mmrChangeColor}15; color:${mmrChangeColor}; border:1px solid ${mmrChangeColor}30; font-family:Outfit; font-weight:800; font-size:0.65rem; padding:1px 7px; border-radius:10px;">${mmrChangeStr} MMR</span>
                    `;

                    const mapName = match.selectedMap || 'Unknown';

                    return `
                        <div style="background:#0d1018; border:1px solid #1c2035; border-left:3px solid ${accentColor}; border-radius:10px; padding:13px 16px; margin-bottom:8px;">
                            <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:8px;">
                                <div style="display:flex; align-items:center; gap:8px; flex-wrap:wrap;">
                                    <span style="font-family:Outfit; font-weight:700; font-size:0.85rem; color:#e2e8f0;">${modeText}</span>
                                    <span style="background:${accentColor}15; color:${accentColor}; border:1px solid ${accentColor}30; font-family:Outfit; font-weight:700; font-size:0.65rem; padding:1px 7px; border-radius:10px;">${outcomeText}</span>
                                    ${mmrBadgeHtml}
                                    ${isMvp ? '<span style="background:rgba(251,191,36,0.1); border:1px solid rgba(251,191,36,0.3); color:#fbbf24; font-family:Outfit; font-weight:700; font-size:0.65rem; padding:1px 7px; border-radius:10px;">MVP</span>' : ''}
                                    <span style="background:rgba(99,102,241,0.1); border:1px solid rgba(99,102,241,0.3); color:#818cf8; font-family:Outfit; font-weight:700; font-size:0.65rem; padding:1px 7px; border-radius:10px;">${mapName}</span>
                                </div>
                                <span style="font-size:0.7rem; color:#334155;">${new Date(match.createdAt).toLocaleDateString()}</span>
                            </div>
                            <div style="display:grid; grid-template-columns:1fr 1fr 1fr; gap:6px; text-align:center;">
                                <div style="background:#131825; border-radius:6px; padding:6px;">
                                    <div style="font-size:0.65rem; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:2px;">K/D/A</div>
                                    <div style="font-size:0.85rem; color:#e2e8f0; font-weight:600;">${myStats.kills}/${myStats.deaths}/${myStats.assists}</div>
                                </div>
                                <div style="background:#131825; border-radius:6px; padding:6px;">
                                    <div style="font-size:0.65rem; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:2px;">SCORE</div>
                                    <div style="font-size:0.85rem; color:#22d3ee; font-weight:600;">${myStats.score}</div>
                                </div>
                                <div style="background:#131825; border-radius:6px; padding:6px;">
                                    <div style="font-size:0.65rem; color:#475569; font-family:Outfit; font-weight:700; margin-bottom:2px;">RATING</div>
                                    <div style="font-size:0.85rem; color:${mmrChangeColor}; font-weight:800; font-family:Outfit;">${ratingDisplay}</div>
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
            
            const myUserId = Auth.getUserId();
            tbody.innerHTML = data.map((p, index) => {
                const rank = index + 1;
                const rankBadge = `#${rank}`;

                const pid = p.playerId || p.PlayerId || p.id || '';
                const isMe = pid.toLowerCase() === (myUserId || '').toLowerCase();
                const rowStyle = isMe ? 'background:rgba(99,102,241,0.08); border-left:3px solid #6366f1;' : '';
                const youBadge = isMe ? '<span style="background:#6366f1; color:white; font-size:0.65rem; padding:1px 6px; border-radius:10px; margin-left:8px; font-weight:700; font-family:Outfit;">YOU</span>' : '';

                return `
                <tr style="border-bottom:1px solid #1c2035; ${rowStyle}" onmouseover="this.style.background='#131825'" onmouseout="this.style.background='${isMe ? 'rgba(99,102,241,0.08)' : ''}'">
                    <td style="text-align:center; padding:14px 16px; font-family:Outfit; font-weight:900; color:#94a3b8; width:70px;">${rankBadge}</td>
                    <td style="padding:14px 16px; font-family:Outfit; font-weight:700; color:white;">
                        ${Utils.escapeHtml(p.username)}
                        ${youBadge}
                    </td>
                    <td style="padding:14px 16px; color:#64748b; font-size:0.875rem;">${regions[p.region] || 'Unknown'}</td>
                    <td style="text-align:right; padding:14px 16px; width:130px;"><span class="mmr-badge">${p.mmr} MMR</span></td>
                </tr>
                `;
            }).join('');
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

        const regions = window.APP_CONFIG?.REGIONS || { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };
        const roles   = window.APP_CONFIG?.ROLES || { 0: 'Player',  1: 'Admin',   2: 'SuperAdmin' };
        const roleColors = window.APP_CONFIG?.ROLE_COLORS || { 0: '#64748b', 1: '#f59e0b', 2: '#ef4444' };

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
            const safeMatchId = Utils.escapeHtml(match.id || '');
            if (match.status === 'Accepted' || match.status === 'StartingServer' || match.status === 'MapVeto') {
                actionHtml = `<button class="btn btn-primary btn-sm" onclick="App.completeAdminMatch('${safeMatchId}')">Complete</button>`;
            }

            return `
                <tr class="border-b border-app-border last:border-0 hover:bg-app-elevated transition-colors">
                    <td class="font-mono text-xs text-slate-500 py-4 px-4">${Utils.escapeHtml(match.id ? match.id.substring(0, 12) : '')}…</td>
                    <td class="py-4 px-4">
                        <span style="padding:4px 10px; border-radius:20px; font-size:0.7rem; font-family:Outfit; font-weight:700; letter-spacing:0.05em; background:${statusColor}15; color:${statusColor}; border:1px solid ${statusColor}30;">
                            ${Utils.escapeHtml(match.status || '')}
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
        this.closeMatchModal();
        this._loggedBans = new Set();
        const logContainer = document.getElementById('veto-log');
        if (logContainer) logContainer.innerHTML = '';

        this.state.status = 'Veto';
        this.state.currentMatchId = vetoState.matchId || vetoState.MatchId;
        this.state.vetoState = vetoState;
        
        window.location.hash = '#match-room';
        
        const team1 = (vetoState.team1 || vetoState.Team1 || [])[0]?.username || (vetoState.team1 || vetoState.Team1 || [])[0]?.Username || 'Team 1';
        const team2 = (vetoState.team2 || vetoState.Team2 || [])[0]?.username || (vetoState.team2 || vetoState.Team2 || [])[0]?.Username || 'Team 2';
        const t1 = document.querySelector('#veto-team1 .team-name');
        const t2 = document.querySelector('#veto-team2 .team-name');
        if (t1) t1.innerText = team1;
        if (t2) t2.innerText = team2;
        
        this.renderMapVetoUI();
        this.startVetoTimer(30);
        Utils.showToast('Map veto phase has started!', 'info');
    },

    startVetoTimer(seconds) {
        if (this._vetoTimerInterval) clearInterval(this._vetoTimerInterval);
        this._vetoTimeLeft = seconds;
        const timerEl = document.getElementById('veto-timer');
        if (timerEl) timerEl.innerText = this._vetoTimeLeft;
        this._vetoTimerInterval = setInterval(() => {
            this._vetoTimeLeft--;
            if (timerEl) timerEl.innerText = Math.max(0, this._vetoTimeLeft);
            if (this._vetoTimeLeft <= 0) {
                clearInterval(this._vetoTimerInterval);
                this._vetoTimerInterval = null;
            }
        }, 1000);
    },

    stopVetoTimer() {
        if (this._vetoTimerInterval) {
            clearInterval(this._vetoTimerInterval);
            this._vetoTimerInterval = null;
        }
    },

    onMapVetoUpdated(vetoState) {
        // Capture previous turn team before updating
        const prev = this.state.vetoState;
        const previousTurnTeam = prev ? (prev.currentTurnTeam ?? prev.CurrentTurnTeam ?? 1) : 1;
        
        this.state.vetoState = vetoState;
        if (vetoState.matchId || vetoState.MatchId) {
            this.state.currentMatchId = vetoState.matchId || vetoState.MatchId;
        }

        this.renderMapVetoUI();

        // Restart timer for next turn (if veto still in progress)
        const status = vetoState.status ?? vetoState.Status;
        if (status === 'InProgress' || status === 0) {
            this.startVetoTimer(30);
        } else {
            this.stopVetoTimer();
        }
        
        // Find last banned map to add to log
        const logContainer = document.getElementById('veto-log');
        const maps = vetoState.maps || vetoState.Maps || [];
        if (logContainer && maps.length > 0) {
            const lastMap = maps.find(m => (m.isBanned ?? m.IsBanned) && !this._loggedBans?.has(m.name || m.Name));
            if (lastMap) {
                const mapName = lastMap.name || lastMap.Name;
                this._loggedBans = this._loggedBans || new Set();
                this._loggedBans.add(mapName);

                const teamLabel = previousTurnTeam === 1 ? 'Team 1' : 'Team 2';
                const logItem = document.createElement('div');
                logItem.className = 'text-danger font-display text-xs p-1 bg-danger/10 rounded border border-danger/20 mb-1 animate-pulse';
                logItem.innerText = `${teamLabel} banned ${mapName}`;
                logContainer.appendChild(logItem);
                logContainer.scrollTop = logContainer.scrollHeight;
            }
        }
    },

    onMatchStarting(matchId) {
        if (window.location.hash === '#match-room') {
            const statusEl = document.getElementById('veto-status-text');
            if (statusEl) {
                statusEl.innerText = 'Server Starting...';
                statusEl.style.color = '#10b981';
            }
        }
    },

    renderMapVetoUI() {
        const v = this.state.vetoState;
        if (!v) return;

        const myId = (Auth.getUserId() || '').toLowerCase();
        const turnPlayerId = (v.currentVetoTurnPlayerId || v.CurrentVetoTurnPlayerId || '').toLowerCase();
        const currentTurnTeam = v.currentTurnTeam ?? v.CurrentTurnTeam;
        const team1 = v.team1 || v.Team1 || [];
        const team2 = v.team2 || v.Team2 || [];
        const status = v.status ?? v.Status;

        let isMyTurn = false;
        if (turnPlayerId) {
            isMyTurn = (turnPlayerId === myId);
        } else if (currentTurnTeam === 1) {
            isMyTurn = team1.some(p => ((p.playerId || p.PlayerId || p.id || p.Id || '')).toLowerCase() === myId);
        } else if (currentTurnTeam === 2) {
            isMyTurn = team2.some(p => ((p.playerId || p.PlayerId || p.id || p.Id || '')).toLowerCase() === myId);
        }

        const isProgress = (status === 'InProgress' || status === 0);
        const isCompleted = (status === 'Completed' || status === 1);

        const statusEl = document.getElementById('veto-status-text');
        if (statusEl) {
            if (isProgress) {
                statusEl.innerText = isMyTurn ? 'YOUR TURN TO VETO' : "OPPONENT'S TURN";
                statusEl.style.color = isMyTurn ? '#10b981' : '#f59e0b';
            } else if (isCompleted) {
                statusEl.innerText = 'Server Starting...';
                statusEl.style.color = '#10b981';
            } else {
                statusEl.innerText = 'Waiting...';
                statusEl.style.color = '#94a3b8';
            }
        }
        
        // Turn indicator styles
        const t1 = document.getElementById('veto-team1');
        const t2 = document.getElementById('veto-team2');
        if (t1 && t2) {
            if (currentTurnTeam === 1 && isProgress) {
                t1.classList.add('opacity-100'); t1.classList.remove('opacity-40');
                t2.classList.add('opacity-40'); t2.classList.remove('opacity-100');
            } else if (currentTurnTeam === 2 && isProgress) {
                t1.classList.add('opacity-40'); t1.classList.remove('opacity-100');
                t2.classList.add('opacity-100'); t2.classList.remove('opacity-40');
            } else {
                t1.classList.add('opacity-100'); t2.classList.add('opacity-100');
                t1.classList.remove('opacity-40'); t2.classList.remove('opacity-40');
            }
        }
        
        const mapsGrid = document.getElementById('veto-maps-grid');
        if (!mapsGrid) return;

        // FACEIT: Lock or unlock the entire grid container based on whether it is genuinely my turn
        if (isMyTurn && isProgress) {
            mapsGrid.classList.remove('veto-locked');
            mapsGrid.style.pointerEvents = 'auto';
        } else {
            mapsGrid.classList.add('veto-locked');
            mapsGrid.style.pointerEvents = 'none';
        }

        const maps = v.maps || v.Maps || [];
        mapsGrid.innerHTML = maps.map(m => {
            const isBanned = m.isBanned ?? m.IsBanned ?? false;
            const isSelected = isCompleted && !isBanned;
            const mapName = m.name || m.Name || '';
            const safeMapName = Utils.escapeHtml(mapName);

            let styles = "relative rounded-xl overflow-hidden transition-all border-2 select-none ";
            let innerStyles = "background: linear-gradient(0deg, rgba(0,0,0,0.85) 0%, rgba(0,0,0,0) 100%);";
            let overlay = "";
            let clickHandler = "";

            if (isBanned) {
                styles += "border-danger/30 opacity-40 grayscale cursor-not-allowed";
                overlay = `<div class="map-overlay absolute inset-0 bg-danger/25 flex flex-col items-center justify-center pointer-events-none z-10">
                    <div class="w-10 h-10 rounded-full bg-danger/20 border border-danger/40 flex items-center justify-center mb-1">
                        <svg class="w-5 h-5 text-danger" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"></path></svg>
                    </div>
                    <span class="font-display font-black text-danger text-xs uppercase tracking-widest bg-black/75 px-2.5 py-1 rounded border border-danger/30">BANNED</span>
                </div>`;
            } else if (isSelected) {
                styles += "border-safe transform scale-[1.03] shadow-[0_0_25px_rgba(16,185,129,0.5)] z-10 cursor-default";
                overlay = `<div class="map-overlay absolute inset-0 bg-safe/15 flex flex-col items-center justify-center pointer-events-none z-10">
                    <div class="w-10 h-10 rounded-full bg-safe/20 border border-safe/40 flex items-center justify-center mb-1">
                        <svg class="w-5 h-5 text-safe" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"></path></svg>
                    </div>
                    <span class="font-display font-black text-white bg-black/70 px-3 py-1 rounded text-xs uppercase tracking-widest text-safe border border-safe/40 backdrop-blur-sm shadow-xl">SELECTED MAP</span>
                </div>`;
            } else if (!isMyTurn) {
                styles += "border-app-border opacity-60 cursor-not-allowed";
            } else {
                styles += "border-app-border hover:border-danger hover:scale-[1.02] active:scale-95 cursor-pointer group shadow-lg";
                overlay = `<div class="map-overlay absolute inset-0 bg-danger/0 group-hover:bg-danger/25 transition-all flex items-center justify-center opacity-0 group-hover:opacity-100 pointer-events-none z-10">
                    <span class="font-display font-black text-white bg-danger/90 px-4 py-1.5 rounded-lg text-xs uppercase tracking-widest shadow-xl transform scale-90 group-hover:scale-100 transition-all pointer-events-none">BAN MAP</span>
                </div>`;
                clickHandler = `onclick="App.vetoMapClick('${safeMapName}')"`;
            }

            const mapNamesMap = window.APP_CONFIG?.MAP_IMAGES || {
                'Mirage': 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?q=80&w=400&h=300&auto=format&fit=crop',
                'Inferno': 'https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=400&h=300&auto=format&fit=crop',
                'Dust2': 'https://images.unsplash.com/photo-1627850854446-c22bebb6e3ad?q=80&w=400&h=300&auto=format&fit=crop',
                'Overpass': 'https://images.unsplash.com/photo-1498084393753-b411b2d26b34?q=80&w=400&h=300&auto=format&fit=crop',
                'Nuke': 'https://images.unsplash.com/photo-1550751827-4bd374c3f58b?q=80&w=400&h=300&auto=format&fit=crop',
                'Vertigo': 'https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?q=80&w=400&h=300&auto=format&fit=crop',
                'Ancient': 'https://images.unsplash.com/photo-1590845947376-2638caa89309?q=80&w=400&h=300&auto=format&fit=crop'
            };
            const bgUrl = mapNamesMap[mapName] || 'https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=400&h=300&auto=format&fit=crop'; 

            return `
                <div class="${styles}" data-map-name="${safeMapName}" ${clickHandler} style="aspect-ratio: 4/3; background-image: url('${bgUrl}'); background-size: cover; background-position: center;">
                    ${overlay}
                    <div class="absolute inset-0 flex items-end p-4 pointer-events-none" style="${innerStyles}">
                        <span class="font-display font-black text-xl text-white uppercase tracking-wider drop-shadow-md pointer-events-none">${safeMapName}</span>
                    </div>
                </div>
            `;
        }).join('');
    },

    async vetoMapClick(mapName) {
        const v = this.state.vetoState;
        const status = v ? (v.status ?? v.Status) : null;
        if (!v || (status !== 'InProgress' && status !== 0)) return;

        // Double check turn
        const myId = (Auth.getUserId() || '').toLowerCase();
        const turnPlayerId = (v.currentVetoTurnPlayerId || v.CurrentVetoTurnPlayerId || '').toLowerCase();
        const currentTurnTeam = v.currentTurnTeam ?? v.CurrentTurnTeam;
        const team1 = v.team1 || v.Team1 || [];
        const team2 = v.team2 || v.Team2 || [];

        let isMyTurn = false;
        if (turnPlayerId) {
            isMyTurn = (turnPlayerId === myId);
        } else if (currentTurnTeam === 1) {
            isMyTurn = team1.some(p => ((p.playerId || p.PlayerId || p.id || p.Id || '')).toLowerCase() === myId);
        } else if (currentTurnTeam === 2) {
            isMyTurn = team2.some(p => ((p.playerId || p.PlayerId || p.id || p.Id || '')).toLowerCase() === myId);
        }

        if (!isMyTurn) {
            Utils.showToast("Wait for your turn to ban a map!", 'info', 2000);
            return;
        }

        const matchId = this.state.currentMatchId || v.matchId || v.MatchId;
        if (!matchId) return;

        const maps = v.maps || v.Maps || [];
        const mapObj = maps.find(m => (m.name || m.Name || '').toLowerCase() === mapName.toLowerCase());
        if (!mapObj || mapObj.isBanned || mapObj.IsBanned) return;

        const actualName = mapObj.name || mapObj.Name || mapName;

        // ─────────────────────────────────────────────────────────────────
        // FACEIT ARCHITECTURE:
        // 1. Synchronously lock the entire grid to prevent ANY click on another map.
        // 2. Transition ONLY the clicked card into immediate "Banning..." visual state in-place.
        // ─────────────────────────────────────────────────────────────────
        const mapsGrid = document.getElementById('veto-maps-grid');
        if (mapsGrid) {
            mapsGrid.classList.add('veto-locked');
            mapsGrid.style.pointerEvents = 'none';
        }

        const cardEl = document.querySelector(`[data-map-name="${actualName}"]`);
        if (cardEl) {
            cardEl.classList.add('border-danger', 'scale-[0.98]', 'opacity-80');
            const overlayEl = cardEl.querySelector('.map-overlay');
            if (overlayEl) {
                overlayEl.className = 'map-overlay absolute inset-0 bg-danger/35 flex flex-col items-center justify-center z-10 pointer-events-none opacity-100';
                overlayEl.innerHTML = `
                    <div class="w-10 h-10 rounded-full bg-danger/20 border border-danger/40 flex items-center justify-center mb-1 animate-pulse">
                        <svg class="w-5 h-5 text-danger" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"></path></svg>
                    </div>
                    <span class="font-display font-black text-danger text-xs uppercase tracking-widest bg-black/80 px-2.5 py-1 rounded border border-danger/40">BANNING...</span>
                `;
            }
        }

        Utils.playNotificationSound();

        try {
            const res = await Api.vetoMap(matchId, actualName);
            const updatedVeto = res?.vetoState || res?.VetoState;
            if (updatedVeto) {
                this.onMapVetoUpdated(updatedVeto);
            }
        } catch (error) {
            console.error("Veto failed:", error);
            Utils.showToast(error.message || 'Failed to ban map. Please try again.', 'error');
            // Re-render UI to restore original state and unlock
            this.renderMapVetoUI();
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
