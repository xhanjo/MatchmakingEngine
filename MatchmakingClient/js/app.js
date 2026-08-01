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
            adminLink.href = 'admin.html';
            adminLink.className = 'text-xs px-3 py-1.5 rounded-lg font-display font-semibold tracking-wider uppercase transition-all border border-app-border text-slate-500 hover:border-brand hover:text-brand-light';
            adminLink.innerText = 'Admin Panel';
            adminLink.style.cssText = 'border-color: #1c2035; color: #64748b;';
            document.getElementById('user-actions').prepend(adminLink);
        }

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
                
                const team1Score = scoreboard.filter(p => (p.team || p.Team) === 1).reduce((sum, p) => sum + (p.kills || p.Kills || 0), 0);
                const team2Score = scoreboard.filter(p => (p.team || p.Team) === 2).reduce((sum, p) => sum + (p.kills || p.Kills || 0), 0);
                const scoreText = `${team1Score} - ${team2Score}`;
                
                const msg = isWinner ? "You won!" : "You lost!";
                Utils.showToast(`Match Finished: ${msg} Score: ${scoreText}`, isWinner ? "success" : "error");
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
            const finishedMatches = (history || []).filter(m =>
                m.status === 'Finished' || m.status === 3
            );

            if (finishedMatches.length === 0) {
                historyContainer.innerHTML = '<p style="color:#334155; font-size:0.8rem;">No completed matches yet.</p>';
            } else {
                historyContainer.innerHTML = finishedMatches.map(match => {
                    const myStats = match.players.find(p => p.playerId === userId);
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
    }
};

window.App = App;
document.addEventListener('DOMContentLoaded', () => {
    const path = window.location.pathname;
    if (path.endsWith('index.html') || path === '/' || path.endsWith('MatchmakingClient/') || path.endsWith('MatchmakingClient')) {
        App.init();
    }
});
