// components/queue.js - Matchmaking queue, accept/decline modal, and match lifecycle

const QueueComponent = {
    async loadInitialData() {
        await Promise.all([
            LobbyComponent.loadFriends(),
            LobbyComponent.loadParty(),
            this.updateMatchmakingStatus(),
            this.loadMyMmr()
        ]);
    },

    async loadMyMmr() {
        try {
            const userId = Auth.getUserId();
            if (!userId) return;
            const player = await Api.getPlayer(userId);
            const mmrDisplay = document.getElementById('nav-mmr-display');
            if (mmrDisplay && player) {
                mmrDisplay.innerText = `${player.mmr} MMR`;
                mmrDisplay.classList.remove('hidden');
            }
        } catch (error) {
            console.error('Failed to load my MMR', error);
        }
    },

    async updateMatchmakingStatus() {
        try {
            const status = await Api.getMatchmakingStatus();
            Store.state.status = status ? (status.status || status.Status || 'Idle') : 'Idle';
            const lobbyId = status?.lobbyId || status?.LobbyId || status?.vetoState?.matchId || status?.VetoState?.MatchId;
            if (lobbyId) {
                Store.state.currentMatchId = lobbyId;
            }

            // If already inside the match room during any veto-related phase,
            // do NOT re-render — let SignalR handle real-time events exclusively.
            // Re-rendering via innerHTML destroys onclick handlers and causes the "0 reaction" bug.
            const isVetoPhase = Store.state.status === 'Veto' || Store.state.status === 'MapVeto';
            if (isVetoPhase && window.location.hash === '#match-room') {
                return;
            }

            const vetoState = status?.vetoState || status?.VetoState;
            if (Store.state.status === 'MapVeto' && vetoState) {
                Store.state.status = 'Veto';
                Store.state.vetoState = vetoState;
                if (vetoState.matchId || vetoState.MatchId) {
                    Store.state.currentMatchId = vetoState.matchId || vetoState.MatchId;
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
                
                if (window.VetoComponent) {
                    VetoComponent.renderMapVetoUI();
                }
            } else if (Store.state.status === 'Idle' && window.location.hash === '#match-room') {
                window.location.hash = '#lobby';
            }

            this.renderMatchmakingButton();
        } catch (error) {
            console.error('Failed to update matchmaking status', error);
        }
    },

    renderMatchmakingButton() {
        const btn = document.getElementById('find-match-btn');
        const statusText = document.getElementById('match-status-text');
        if (!btn || !statusText) return;

        const modeText = Store.state.party ? 'FIND DUO MATCH' : 'FIND MATCH';

        // Always reset class to base first
        btn.className = 'find-match-base';
        btn.disabled = false;

        if (Store.state.status === 'Searching') {
            btn.classList.add('find-match-searching');
            btn.innerText = 'CANCEL SEARCH';
            if (!Store.searchTimerInterval) {
                Store.searchStartTime = Date.now();
                Store.searchTimerInterval = setInterval(() => {
                    const elapsed = Math.floor((Date.now() - Store.searchStartTime) / 1000);
                    const mins = Math.floor(elapsed / 60);
                    const secs = elapsed % 60;
                    statusText.innerText = `Searching… ${mins}:${secs.toString().padStart(2, '0')}`;
                }, 1000);
            }
        } else if (Store.state.status === 'MatchFound') {
            btn.classList.add('find-match-found');
            btn.innerText = 'MATCH FOUND';
            btn.disabled = true;
            statusText.innerText = 'Waiting for all players to accept…';
            if (Store.searchTimerInterval) {
                clearInterval(Store.searchTimerInterval);
                Store.searchTimerInterval = null;
            }
        } else if (Store.state.status === 'InGame' || Store.state.status === 'Accepted') {
            btn.classList.add('find-match-idle');
            btn.innerText = 'MATCH IN PROGRESS';
            btn.disabled = true;
            const shortId = Store.state.currentMatchId ? Utils.escapeHtml(Store.state.currentMatchId.substring(0, 8)) : '?';
            statusText.innerHTML = `
                <span style="color:#10b981; font-weight:600;">● Match in progress</span>
                <span style="color:#334155; font-size:0.72rem; margin-left:8px;">ID: ${shortId}…</span>
            `;
            if (Store.searchTimerInterval) {
                clearInterval(Store.searchTimerInterval);
                Store.searchTimerInterval = null;
            }
        } else {
            btn.classList.add('find-match-idle');
            btn.innerText = modeText;
            statusText.innerText = '';
            if (Store.searchTimerInterval) {
                clearInterval(Store.searchTimerInterval);
                Store.searchTimerInterval = null;
            }
        }
        
        // Ensure party UI updates correctly if status changes
        const createBtn = document.getElementById('create-party-btn');
        const leaveBtn = document.getElementById('leave-party-btn');
        const partyContainer = document.getElementById('party-container');
        const partyWrapper = document.getElementById('party-wrapper');
        
        if (!Store.state.party) {
            if (createBtn) createBtn.style.display = Store.state.status !== 'Idle' ? 'none' : 'inline-flex';
            if (partyContainer) partyContainer.style.display = 'none';
            if (partyWrapper) partyWrapper.style.display = Store.state.status !== 'Idle' ? 'none' : 'block';
        } else {
            if (createBtn) createBtn.style.display = 'none';
            if (partyContainer) partyContainer.style.display = 'block';
            if (leaveBtn) leaveBtn.style.display = Store.state.status !== 'Idle' ? 'none' : 'block';
            if (partyWrapper) partyWrapper.style.display = 'block';
        }
    },

    async toggleMatchmaking() {
        if (Store._isTogglingMatchmaking) return;
        Store._isTogglingMatchmaking = true;
        const btn = document.getElementById('find-match-btn');
        if (btn) btn.disabled = true;

        try {
            if (Store.state.status === 'Searching') {
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
            Store._isTogglingMatchmaking = false;
            if (btn) btn.disabled = false;
        }
    },

    onMatchFound(matchId) {
        if (Store.searchTimerInterval) {
            clearInterval(Store.searchTimerInterval);
            Store.searchTimerInterval = null;
        }

        Store.state.currentMatchId = matchId;
        Store.state.status = 'MatchFound';
        this.renderMatchmakingButton();

        Utils.playNotificationSound();

        const modal = document.getElementById('match-modal');
        if (!modal) return;
        modal.classList.add('active');

        // Reset + start timer
        const circle = document.querySelector('.timer-circle .progress-stroke');
        let timeLeft = 30;
        const totalTime = 30;
        const circumference = 283;

        if (circle) {
            circle.style.transition = 'none';
            circle.style.strokeDashoffset = '0';
            circle.style.stroke = '#10b981';
        }

        const timerText = document.getElementById('timer-text');
        if (timerText) timerText.innerText = timeLeft;

        clearInterval(Store.state.matchTimer);
        Store.state.matchTimer = setInterval(() => {
            timeLeft--;
            if (timerText) timerText.innerText = timeLeft;
            if (circle) {
                const offset = circumference * (1 - timeLeft / totalTime);
                circle.style.transition = 'stroke-dashoffset 1s linear, stroke 0.3s';
                circle.style.strokeDashoffset = offset;
                if (timeLeft <= 10) {
                    circle.style.stroke = '#ef4444';
                }
            }
            if (timeLeft <= 0) {
                clearInterval(Store.state.matchTimer);
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
        Store.state.status = 'InGame';
        Store.state.currentMatchId = matchId;
        this.renderMatchmakingButton();

        const statusText = document.getElementById('match-status-text');
        const shortId = matchId ? Utils.escapeHtml(matchId.substring(0, 8)) : '?';
        if (statusText) {
            statusText.innerHTML = `
                <span style="color:#10b981; font-weight:600;">● Match in progress</span>
                <span style="color:#334155; font-size:0.72rem; margin-left:8px;">ID: ${shortId}…</span>
            `;
        }

        Utils.showToast('All players accepted — match is starting!', 'success');
        
        if (window.location.hash === '#match-room') {
            const vetoStatus = document.getElementById('veto-status-text');
            if (vetoStatus) {
                vetoStatus.innerText = 'Server Starting...';
                vetoStatus.style.color = '#10b981';
            }
        }
    },

    onMatchFinished(result) {
        try {
            const myId = Auth.getUserId();
            const scoreboard = result.scoreboard || result.Scoreboard;
            
            if (!scoreboard) {
                Utils.showToast("Match finished!", "success");
                Store.state.currentMatchId = null;
                Store.state.status = 'Idle';
                this.updateMatchmakingStatus();
                return;
            }
            
            const myStats = scoreboard.find(p => {
                const pid = p.playerId || p.PlayerId;
                return pid && pid.toLowerCase() === (myId || '').toLowerCase();
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
            
            Store.state.currentMatchId = null;
            Store.state.status = 'Idle';
            if (window.VetoComponent) VetoComponent.stopVetoTimer();
            
            // Navigate back to lobby and force route refresh
            window.location.hash = '#lobby';
            Router.handleRoute();
            if (window.LeaderboardComponent) LeaderboardComponent.loadLeaderboard();
            this.loadMyMmr();
            this.updateMatchmakingStatus();
        } catch (e) {
            console.error("Error displaying match result:", e);
            Utils.showToast("Match Finished!", "success");
            Store.state.currentMatchId = null;
            this.updateMatchmakingStatus();
            window.location.hash = '#lobby';
            Router.handleRoute();
        }
    },

    async acceptMatch() {
        if (!Store.state.currentMatchId) return;
        const btn = document.getElementById('accept-match-btn');
        const declineBtn = document.getElementById('decline-match-btn');

        // Immediate visual feedback before API call
        if (btn) {
            btn.innerText = 'ACCEPTED';
            btn.style.cssText = 'width:100%; padding:16px; border-radius:12px; font-family:Outfit; font-weight:900; font-size:1.125rem; text-transform:uppercase; letter-spacing:0.1em; color:white; border:none; cursor:default; background:#047857; box-shadow:0 0 25px rgba(16,185,129,0.35);';
            btn.disabled = true;
        }
        if (declineBtn) declineBtn.disabled = true;

        const titleEl = document.getElementById('match-modal')?.querySelector('.match-found-title');
        if (titleEl) {
            titleEl.innerText = 'WAITING FOR OTHERS…';
            titleEl.style.color = '#f59e0b';
            titleEl.style.textShadow = '0 0 20px rgba(245,158,11,0.5)';
        }

        try {
            await Api.acceptMatch(Store.state.currentMatchId);
        } catch (error) {
            Utils.showToast(error.message, 'error');
            this.closeMatchModal();
        }
    },

    async declineMatch() {
        if (!Store.state.currentMatchId) return;
        try {
            await Api.declineMatch(Store.state.currentMatchId);
            this.closeMatchModal();
        } catch (error) {
            Utils.showToast(error.message, 'error');
            this.closeMatchModal();
        }
    },

    closeMatchModal() {
        clearInterval(Store.state.matchTimer);
        const circle = document.querySelector('.timer-circle .progress-stroke');
        if (circle) {
            circle.style.transition = 'none';
            circle.style.strokeDashoffset = '0';
            circle.style.stroke = '#10b981';
        }

        const modal = document.getElementById('match-modal');
        if (modal) modal.classList.remove('active');

        setTimeout(() => {
            const titleEl = document.getElementById('match-modal')?.querySelector('.match-found-title');
            if (titleEl) {
                titleEl.innerText = 'MATCH FOUND';
                titleEl.style.color = '';
                titleEl.style.textShadow = '';
            }
            const btn = document.getElementById('accept-match-btn');
            if (btn) {
                btn.innerText = 'ACCEPT';
                btn.style.cssText = 'background:#059669; box-shadow:0 0 25px rgba(16,185,129,0.4); width:100%; padding:16px; border-radius:12px; font-family:Outfit; font-weight:900; font-size:1.125rem; text-transform:uppercase; letter-spacing:0.1em; color:white; border:none; cursor:pointer;';
                btn.disabled = false;
            }
            const declineBtn = document.getElementById('decline-match-btn');
            if (declineBtn) declineBtn.disabled = false;
        }, 300);

        Store.state.currentMatchId = null;
        this.updateMatchmakingStatus();
    }
};

window.QueueComponent = QueueComponent;
