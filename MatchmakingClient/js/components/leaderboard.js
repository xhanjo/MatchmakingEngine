// components/leaderboard.js - Leaderboard top 100 table

const LeaderboardComponent = {
    async loadLeaderboard() {
        try {
            const tbody = document.getElementById('leaderboard-tbody');
            if (!tbody) return;
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-slate-600 py-8">Loading...</td></tr>';
            const data = await Api.getLeaderboard();
            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-slate-600 py-8">No players found</td></tr>';
                return;
            }
            
            const regions = window.APP_CONFIG?.REGIONS || { 1: 'EU West', 2: 'EU East', 3: 'NA East', 4: 'Asia' };
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
            const tbody = document.getElementById('leaderboard-tbody');
            if (tbody) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-8">Failed to load</td></tr>';
            }
        }
    }
};

window.LeaderboardComponent = LeaderboardComponent;
