// auth.js - Authentication and JWT handling

const Auth = {
    getToken() {
        return localStorage.getItem('jwt_token') || "";
    },
    
    setToken(token) {
        // Ми отримуємо токен від бекенду під час логіну лише один раз
        // Розкодовуємо його і зберігаємо лише корисну інформацію (ID, Ім'я, Роль)
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            localStorage.setItem('user_payload', jsonPayload);
            localStorage.setItem('jwt_token', token);
        } catch (e) {
            console.error("Failed to decode and save token payload", e);
        }
    },

    clearToken() {
        localStorage.removeItem('user_payload');
        localStorage.removeItem('jwt_token');
    },

    isLoggedIn() {
        const payload = this.getDecodedToken();
        const token = this.getToken();
        if (!payload || !token) return false;
        
        try {
            const exp = payload.exp * 1000;
            // 30-second grace window to prevent edge-case expirations during in-flight requests
            return Date.now() < (exp - 30000);
        } catch (e) {
            return false;
        }
    },

    getDecodedToken() {
        const payloadJson = localStorage.getItem('user_payload');
        if (!payloadJson) return null;
        try {
            return JSON.parse(payloadJson);
        } catch (e) {
            return null;
        }
    },

    getUserId() {
        const decoded = this.getDecodedToken();
        return decoded ? (decoded.PlayerId || decoded.playerId || decoded.nameid || decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || decoded.sub) : null;
    },

    getUserName() {
        const decoded = this.getDecodedToken();
        return decoded ? (decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || decoded.unique_name || decoded.sub) : null;
    },

    isAdmin() {
        const decoded = this.getDecodedToken();
        if (!decoded) return false;
        const role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        return role === "Admin" || role === "SuperAdmin";
    },

    requireAuth() {
        if (!this.isLoggedIn()) {
            this.clearToken();
            window.location.href = 'login.html';
        }
    },

    requireAdmin() {
        this.requireAuth();
        if (!this.isAdmin()) {
            window.location.href = 'index.html';
        }
    },

    async logout() {
        const token = this.getToken();
        this.clearToken();
        try {
            if (token) {
                const apiUrl = (window.APP_CONFIG && window.APP_CONFIG.API_URL) 
                    ? window.APP_CONFIG.API_URL 
                    : (typeof API_URL !== 'undefined' ? API_URL : '/api');
                await fetch(`${apiUrl}/Auth/logout`, { 
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${token}` }
                });
            }
        } catch (e) {
            console.warn("Logout request failed:", e);
        }
        window.location.href = 'login.html';
    }
};

window.Auth = Auth;
