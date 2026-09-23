// state.js - Central reactive store for MatchmakingClient

const Store = {
    state: {
        friends: [],
        pendingRequests: [],
        party: null,
        status: 'Idle',
        matchTimer: null,
        currentMatchId: null,
        vetoState: null
    },

    // Non-serialized runtime properties
    searchTimerInterval: null,
    searchStartTime: null,
    searchTimeout: null,
    _isTogglingMatchmaking: false,
    _pollingIntervals: [],
    _loggedBans: new Set(),
    _vetoTimerInterval: null,
    _vetoTimeLeft: 30
};

window.Store = Store;
