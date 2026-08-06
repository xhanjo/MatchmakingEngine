// auth.js - Authentication and JWT handling

const Auth = {
    getToken() {
        return ""; // Токен більше не доступний клієнту
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
        } catch (e) {
            console.error("Failed to decode and save token payload", e);
        }
    },

    clearToken() {
        localStorage.removeItem('user_payload');
    },

    isLoggedIn() {
        const payload = this.getDecodedToken();
        if (!payload) return false;
        
        try {
            const exp = payload.exp * 1000;
            return Date.now() < exp;
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

    async logout() {
        this.clearToken();
        try {
            await fetch('http://localhost:5001/api/Auth/logout', { 
                method: 'POST',
                credentials: 'include' 
            });
        } catch (e) {}
        window.location.href = 'login.html';
    }
};

window.Auth = Auth;
