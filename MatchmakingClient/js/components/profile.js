// components/profile.js - Profile modal, player statistics, and match history

const ProfileComponent = {
    async showProfile() {
        try {
            const userId = Auth.getUserId();
            if (!userId) return;

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

            const statsContainer = document.getElementById('profile-stats');
            if (statsContainer) {
                statsContainer.innerHTML = `
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
            }

            const historyContainer = document.getElementById('profile-history');
            if (historyContainer) {
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

                        const accentColor = isWinner ? '#10b981' : '#ef4444';
                        const outcomeText = isWinner ? 'WIN' : 'LOSS';

                        // Determine Elo MMR change
                        let mmrChange = null;
                        if (typeof myStats.mmrChange === 'number' && myStats.mmrChange !== 0) {
                            mmrChange = myStats.mmrChange;
                        } else if (typeof myStats.MmrChange === 'number' && myStats.MmrChange !== 0) {
                            mmrChange = myStats.MmrChange;
                        } else {
                            // Dynamic Elo calculation fallback
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
            }

            const profileModal = document.getElementById('profile-modal');
            if (profileModal) profileModal.classList.add('active');
        } catch (error) {
            Utils.showToast('Failed to load profile', 'error');
            console.error(error);
        }
    }
};

window.ProfileComponent = ProfileComponent;
