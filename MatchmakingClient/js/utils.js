// utils.js - Global utility functions

const Utils = {
    showToast(message, type = 'info', duration = 3000) {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        
        let icon = '';
        if (type === 'success') icon = '✓ ';
        else if (type === 'error') icon = '⚠ ';
        
        toast.innerHTML = `<span>${icon}${message}</span>`;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'fade-out 0.3s ease-in forwards';
            setTimeout(() => {
                if(toast.parentElement) toast.remove();
            }, 300);
        }, duration);
    },

    playNotificationSound() {
        // Simple base64 encoded beep
        const beep = "data:audio/wav;base64,UklGRl9vT19XQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YU"; 
        try {
            // Using a more standard approach for a web synth beep instead to ensure it works reliably
            const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioCtx.createOscillator();
            const gainNode = audioCtx.createGain();
            
            oscillator.type = 'sine';
            oscillator.frequency.setValueAtTime(880, audioCtx.currentTime); // A5
            
            gainNode.gain.setValueAtTime(0.1, audioCtx.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.00001, audioCtx.currentTime + 0.5);
            
            oscillator.connect(gainNode);
            gainNode.connect(audioCtx.destination);
            
            oscillator.start();
            oscillator.stop(audioCtx.currentTime + 0.5);
        } catch (e) {
            console.log("Audio play failed:", e);
        }
    },

    escapeHtml(unsafe) {
        return (unsafe || "").toString()
             .replace(/&/g, "&amp;")
             .replace(/</g, "&lt;")
             .replace(/>/g, "&gt;")
             .replace(/"/g, "&quot;")
             .replace(/'/g, "&#039;");
    }
};

window.Utils = Utils;
