// components/lobby.js - Party, friends, player search, and invite handling

const LobbyComponent = {
    async loadFriends() {
        try {
            const [friends, pending] = await Promise.all([
                Api.getFriends(),
                Api.getPendingFriends()
            ]);
            Store.state.friends = friends || [];
            Store.state.pendingRequests = pending || [];
            this.renderFriendsList();
        } catch (error) {
            console.error('Failed to load friends', error);
        }
    },

    async loadParty() {
        try {
            const party = await Api.getMyParty();
            Store.state.party = party;
            this.renderParty();
        } catch (error) {
            Store.state.party = null;
            this.renderParty();
        }
    },

    renderFriendsList() {
        const friendsList = document.getElementById('friends-list');
        const pendingList = document.getElementById('pending-requests');
        if (!friendsList || !pendingList) return;

        // Pending requests
        pendingList.innerHTML = '';
        if (Store.state.pendingRequests.length > 0) {
            const header = document.createElement('p');
            header.className = 'pending-header';
            header.innerText = `Pending (${Store.state.pendingRequests.length})`;
            pendingList.appendChild(header);

            Store.state.pendingRequests.forEach(req => {
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
        if (Store.state.friends.length === 0) {
            friendsList.innerHTML = '<p style="color:#334155; font-size:0.8rem; padding: 8px 0;">No friends yet. Search for players above.</p>';
        } else {
            Store.state.friends.forEach(friend => {
                const item = document.createElement('div');
                item.className = 'list-item';

                let inviteBtn = '';
                if (Store.state.party && Store.state.party.leaderId === Auth.getUserId() && Store.state.party.members.length < 2) {
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

    renderParty() {
        const partyContainer = document.getElementById('party-container');
        const createBtn = document.getElementById('create-party-btn');
        const leaveBtn = document.getElementById('leave-party-btn');
        const membersList = document.getElementById('party-members-list');
        const partyWrapper = document.getElementById('party-wrapper');
        if (!partyContainer || !createBtn || !leaveBtn || !membersList) return;

        if (!Store.state.party) {
            createBtn.style.display = Store.state.status !== 'Idle' ? 'none' : 'inline-flex';
            partyContainer.style.display = 'none';
            leaveBtn.style.display = 'none';
            if (partyWrapper) partyWrapper.style.display = Store.state.status !== 'Idle' ? 'none' : 'block';
            if (window.QueueComponent) QueueComponent.renderMatchmakingButton();
            return;
        }

        if (partyWrapper) partyWrapper.style.display = 'block';

        if (Store.state.status !== 'Idle') {
            createBtn.style.display = 'none';
            partyContainer.style.display = 'block';
            leaveBtn.style.display = 'none';
        } else {
            createBtn.style.display = 'none';
            partyContainer.style.display = 'block';
            leaveBtn.style.display = 'block';
        }

        membersList.innerHTML = '';
        Store.state.party.members.forEach(m => {
            const isLeader = m.playerId === Store.state.party.leaderId;
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

        if (window.QueueComponent) QueueComponent.renderMatchmakingButton();
        this.renderFriendsList();
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
        clearTimeout(Store.searchTimeout);
        const resultsContainer = document.getElementById('search-results');
        if (!resultsContainer) return;

        if (!query || query.length < 2) {
            resultsContainer.innerHTML = '';
            return;
        }

        Store.searchTimeout = setTimeout(async () => {
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
            const searchInput = document.getElementById('player-search');
            if (searchInput) searchInput.value = '';
            const searchResults = document.getElementById('search-results');
            if (searchResults) searchResults.innerHTML = '';
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    },

    onPartyInviteReceived(partyId, senderId) {
        const modal = document.getElementById('invite-modal');
        if (!modal) return;
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
    }
};

window.LobbyComponent = LobbyComponent;
