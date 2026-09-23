// components/admin.js - Admin dashboard, player management, and match controls

const AdminComponent = {
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
            console.error('Failed to load admin data', error);
        }
    },

    renderAdminPlayersTable(players) {
        const tbody = document.getElementById('players-tbody');
        if (!tbody) return;
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
    }
};

window.AdminComponent = AdminComponent;
