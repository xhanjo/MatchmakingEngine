// config.js - Global application configuration
(function() {
    const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
    const isStandalonePort = window.location.port === '5500' || window.location.port === '3000' || window.location.protocol === 'file:';

    const defaultApiUrl = (!isStandalonePort && window.location.protocol.startsWith('http'))
        ? '/api'
        : 'http://matchmaking-engine-env.eba-mrkcxifv.eu-north-1.elasticbeanstalk.com/api';

    const defaultHubUrl = (!isStandalonePort && window.location.protocol.startsWith('http'))
        ? '/hubs/matchmaking'
        : 'http://matchmaking-engine-env.eba-mrkcxifv.eu-north-1.elasticbeanstalk.com/hubs/matchmaking';

    const APP_CONFIG = {
        API_URL: window.API_OVERRIDE_URL || defaultApiUrl,
        HUB_URL: window.HUB_OVERRIDE_URL || defaultHubUrl,
        REGIONS: {
            1: 'EU West',
            2: 'EU East',
            3: 'NA East',
            4: 'Asia'
        },
        ROLES: {
            0: 'Player',
            1: 'Admin',
            2: 'SuperAdmin'
        },
        ROLE_COLORS: {
            0: '#64748b',
            1: '#f59e0b',
            2: '#ef4444'
        },
        MAP_IMAGES: {
            'Mirage': 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?q=80&w=400&h=300&auto=format&fit=crop',
            'Inferno': 'https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=400&h=300&auto=format&fit=crop',
            'Dust2': 'https://images.unsplash.com/photo-1627850854446-c22bebb6e3ad?q=80&w=400&h=300&auto=format&fit=crop',
            'Overpass': 'https://images.unsplash.com/photo-1498084393753-b411b2d26b34?q=80&w=400&h=300&auto=format&fit=crop',
            'Nuke': 'https://images.unsplash.com/photo-1550751827-4bd374c3f58b?q=80&w=400&h=300&auto=format&fit=crop',
            'Vertigo': 'https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?q=80&w=400&h=300&auto=format&fit=crop',
            'Ancient': 'https://images.unsplash.com/photo-1590845947376-2638caa89309?q=80&w=400&h=300&auto=format&fit=crop'
        }
    };

    window.APP_CONFIG = APP_CONFIG;
})();
