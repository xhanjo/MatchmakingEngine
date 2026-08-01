const Admin = {
    async init() {
        Auth.requireAdmin();
        document.getElementById('logout-btn').addEventListener('click', () => Auth.logout());
        document.getElementById('username-display').innerText = Auth.getUserName() || 'Admin';

        await this.loadData();
        
        SignalRManager.init();
        
        // Remove setInterval and rely on SignalR for real-time updates
        SignalRManager.connection.on('AdminMatchesUpdated', () => {
            console.log('AdminMatchesUpdated received');
            this.loadData();
        });
    },

    async loadData() {
        try {
            const [activeMatches, allMatches, players] = await Promise.all([
                Api.request('/Admin/active-matches'),
                Api.request('/Admin/matches'),
                Api.getPlayers()
            ]);

            this.renderMatchTable('active-matches-tbody', activeMatches, true);
            this.renderMatchTable('all-matches-tbody', allMatches, false);
            this.renderPlayersTable(players);

            // Update count
            const countEl = document.getElementById('all-matches-count');
            if (countEl) countEl.innerText = `(${allMatches?.length ?? 0})`;
        } catch (error) {
            Utils.showToast('Failed to load admin data', 'error');
            console.error(error);
        }
    },

    renderPlayersTable(players) {
        const tbody = document.getElementById('players-tbody');
        if (!players || players.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center; color:#334155; padding:24px;">No players registered</td></tr>';
            return;
        }

        const regions = { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };
        const roles   = { 0: 'Player',  1: 'Admin',   2: 'SuperAdmin' };
        const roleColors = { 0: '#64748b', 1: '#f59e0b', 2: '#ef4444' };

        tbody.innerHTML = players.map(p => `
            <tr>
                <td style="font-weight:500; color:#e2e8f0;">${Utils.escapeHtml(p.username)}</td>
                <td>
                    <span style="padding:2px 8px; border-radius:6px; font-size:0.7rem; font-family:Outfit; font-weight:700; letter-spacing:0.05em;
                          background:${roleColors[p.role] || '#64748b'}15; color:${roleColors[p.role] || '#64748b'}; border:1px solid ${roleColors[p.role] || '#64748b'}30;">
                        ${roles[p.role] || 'Player'}
                    </span>
                </td>
                <td style="color:#64748b;">${regions[p.region] || 'Unknown'}</td>
                <td><span class="mmr-badge">${p.mmr} MMR</span></td>
                <td>
                    <div style="display:flex; align-items:center; gap:6px;">
                        <div style="flex:1; height:4px; background:#1c2035; border-radius:4px; max-width:60px;">
                            <div style="height:100%; border-radius:4px; width:${Math.round(p.trustFactor * 100)}%;
                                        background:${p.trustFactor >= 0.7 ? '#10b981' : p.trustFactor >= 0.4 ? '#f59e0b' : '#ef4444'};"></div>
                        </div>
                        <span style="font-size:0.75rem; color:#64748b;">${(p.trustFactor * 100).toFixed(0)}%</span>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    renderMatchTable(tbodyId, matches, isActive) {
        const tbody = document.getElementById(tbodyId);
        if (!tbody) return;

        if (!matches || matches.length === 0) {
            tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:#334155; padding:24px;">No matches found</td></tr>`;
            return;
        }

        const statusColors = {
            'Pending':  '#f59e0b',
            'Accepted': '#6366f1',
            'Finished': '#10b981',
            'Canceled': '#475569',
        };

        tbody.innerHTML = matches.map(match => {
            const createdAt = new Date(match.createdAt).toLocaleString();
            const avgMmr = match.averageMmr ?? '-';
            const mode = (match.gameMode === 1 || match.gameMode === 'Solo') ? 'Solo' : 'Duo';
            const statusColor = statusColors[match.status] || '#64748b';

            let actionHtml = '<span style="color:#334155;">—</span>';
            if (isActive && match.status === 'Accepted') {
                actionHtml = `<button class="btn btn-primary btn-sm" onclick="Admin.completeMatch('${match.id}')">Complete</button>`;
            }

            return `
                <tr>
                    <td style="font-family:monospace; font-size:0.75rem; color:#475569;">${match.id.substring(0, 12)}…</td>
                    <td>
                        <span style="padding:2px 10px; border-radius:20px; font-size:0.7rem; font-family:Outfit; font-weight:700; letter-spacing:0.05em;
                              background:${statusColor}15; color:${statusColor}; border:1px solid ${statusColor}30;">
                            ${match.status}
                        </span>
                    </td>
                    <td><span class="mmr-badge">${avgMmr}</span></td>
                    <td style="color:#94a3b8;">${mode}</td>
                    <td style="color:#475569; font-size:0.8rem;">${createdAt}</td>
                    <td style="color:#64748b;">${match.playerCount ?? '?'}</td>
                    <td>${actionHtml}</td>
                </tr>
            `;
        }).join('');
    },

    async completeMatch(matchId) {
        try {
            await Api.completeMatch(matchId);
            Utils.showToast('Match completed successfully!', 'success');
            await this.loadData();   // ← fixed: was this.loadMatches() which didn't exist
        } catch (error) {
            Utils.showToast(error.message, 'error');
        }
    }
};

window.Admin = Admin;
document.addEventListener('DOMContentLoaded', () => {
    if (window.location.pathname.endsWith('admin.html')) {
        Admin.init();
    }
});
