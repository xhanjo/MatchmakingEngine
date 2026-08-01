// auth.js - Authentication and JWT handling

const Auth = {
    getToken() {
        return localStorage.getItem('jwt_token');
    },
    
    setToken(token) {
        localStorage.setItem('jwt_token', token);
    },

    clearToken() {
        localStorage.removeItem('jwt_token');
    },

    isLoggedIn() {
        const token = this.getToken();
        if (!token) return false;
        
        try {
            const payload = this.getDecodedToken();
            const exp = payload.exp * 1000;
            return Date.now() < exp;
        } catch (e) {
            return false;
        }
    },

    getDecodedToken() {
        const token = this.getToken();
        if (!token) return null;
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            return JSON.parse(jsonPayload);
        } catch (e) {
            console.error("Failed to decode token", e);
            return null;
        }
    },

    getUserId() {
        const decoded = this.getDecodedToken();
        return decoded ? decoded.PlayerId : null;
    },

    getUserName() {
        const decoded = this.getDecodedToken();
        return decoded ? (decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || decoded.unique_name || decoded.sub) : null;
    },

    isAdmin() {
        const decoded = this.getDecodedToken();
        if (!decoded) return false;
        const role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        return role === "Admin";
    },

    requireAuth() {
        if (!this.isLoggedIn()) {
            window.location.href = 'login.html';
        }
    },

    requireAdmin() {
        this.requireAuth();
        if (!this.isAdmin()) {
            window.location.href = 'index.html';
        }
    },

    logout() {
        this.clearToken();
        window.location.href = 'login.html';
    }
};

window.Auth = Auth;
