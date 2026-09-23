// router.js - Hash-based navigation and view switcher

const Router = {
    init() {
        window.addEventListener('hashchange', () => this.handleRoute());
        setTimeout(() => this.handleRoute(), 0);
    },

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
            if (v) {
                v.classList.remove('hidden');
                v.classList.add('flex');
            }
        } else if (hash === '#leaderboard') {
            const v = document.getElementById('view-leaderboard');
            if (v) v.classList.remove('hidden');
            if (window.LeaderboardComponent) {
                LeaderboardComponent.loadLeaderboard();
            }
        } else if (hash === '#admin') {
            if (Auth.isAdmin()) {
                const v = document.getElementById('view-admin');
                if (v) v.classList.remove('hidden');
                if (window.AdminComponent) {
                    AdminComponent.loadAdminData();
                }
            } else {
                window.location.hash = '#lobby';
            }
        } else if (hash === '#match-room') {
            const v = document.getElementById('view-match-room');
            if (v) {
                v.classList.remove('hidden');
                v.classList.add('flex');
            }
        }
    }
};

window.Router = Router;
