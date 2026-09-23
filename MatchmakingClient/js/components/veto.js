// components/veto.js - Map Veto Phase controller (turns, timer, grid rendering, FaceIt 1-click ban)

const VetoComponent = {
    onMatchReadyForVeto(vetoState) {
        if (window.QueueComponent) {
            QueueComponent.closeMatchModal();
        }

        const logContainer = document.getElementById('veto-log');
        if (logContainer) logContainer.innerHTML = '';
        Store._loggedBans = new Set();

        Store.state.status = 'Veto';
        Store.state.currentMatchId = vetoState.matchId || vetoState.MatchId;
        Store.state.vetoState = vetoState;

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
        if (Store._vetoTimerInterval) clearInterval(Store._vetoTimerInterval);
        Store._vetoTimeLeft = seconds;
        const timerEl = document.getElementById('veto-timer');
        if (timerEl) timerEl.innerText = Store._vetoTimeLeft;
        Store._vetoTimerInterval = setInterval(() => {
            Store._vetoTimeLeft--;
            if (timerEl) timerEl.innerText = Math.max(0, Store._vetoTimeLeft);
            if (Store._vetoTimeLeft <= 0) {
                clearInterval(Store._vetoTimerInterval);
                Store._vetoTimerInterval = null;
            }
        }, 1000);
    },

    stopVetoTimer() {
        if (Store._vetoTimerInterval) {
            clearInterval(Store._vetoTimerInterval);
            Store._vetoTimerInterval = null;
        }
    },

    onMapVetoUpdated(vetoState) {
        const prev = Store.state.vetoState;
        const previousTurnTeam = prev ? (prev.currentTurnTeam ?? prev.CurrentTurnTeam ?? 1) : 1;
        
        Store.state.vetoState = vetoState;
        if (vetoState.matchId || vetoState.MatchId) {
            Store.state.currentMatchId = vetoState.matchId || vetoState.MatchId;
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
            const lastMap = maps.find(m => (m.isBanned ?? m.IsBanned) && !Store._loggedBans?.has(m.name || m.Name));
            if (lastMap) {
                const mapName = lastMap.name || lastMap.Name;
                Store._loggedBans = Store._loggedBans || new Set();
                Store._loggedBans.add(mapName);

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
        const v = Store.state.vetoState;
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

        // Visual indicator: dim the grid when it's not the player's turn
        // but ALWAYS keep pointer-events enabled so clicks can reach handlers and show feedback
        if (isMyTurn && isProgress) {
            mapsGrid.classList.remove('veto-locked');
        } else {
            mapsGrid.classList.add('veto-locked');
        }
        mapsGrid.style.pointerEvents = 'auto';

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
                // Still attach click handler so the player gets feedback ("Wait for your turn")
                clickHandler = `onclick="App.vetoMapClick('${safeMapName}')"`;
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
        const v = Store.state.vetoState;
        const status = v ? (v.status ?? v.Status) : null;
        if (!v || (status !== 'InProgress' && status !== 0)) {
            console.warn('[Veto] Click ignored: veto not in progress. Status:', status);
            return;
        }

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

        const matchId = Store.state.currentMatchId || v.matchId || v.MatchId;
        if (!matchId) {
            console.error('[Veto] No matchId available. State:', JSON.stringify({ currentMatchId: Store.state.currentMatchId, vetoMatchId: v.matchId || v.MatchId }));
            Utils.showToast('Unable to process ban. Please refresh the page.', 'error');
            return;
        }

        const maps = v.maps || v.Maps || [];
        const mapObj = maps.find(m => (m.name || m.Name || '').toLowerCase() === mapName.toLowerCase());
        if (!mapObj || mapObj.isBanned || mapObj.IsBanned) {
            console.warn('[Veto] Map already banned or not found:', mapName);
            return;
        }

        const actualName = mapObj.name || mapObj.Name || mapName;

        // FACEIT ARCHITECTURE:
        // 1. Synchronously lock the entire grid to prevent ANY click on another map.
        // 2. Transition ONLY the clicked card into immediate "Banning..." visual state in-place.
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

window.VetoComponent = VetoComponent;
