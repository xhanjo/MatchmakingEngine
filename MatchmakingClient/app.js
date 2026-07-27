const apiUrl = "http://localhost:5001";

const authSection = document.getElementById("auth-section");
const lobbySection = document.getElementById("lobby-section");
const adminSection = document.getElementById("admin-section");
const errorText = document.getElementById("auth-error");

const matchStatus = document.getElementById("match-status");
const searchTimer = document.getElementById("search-timer");
const lobbyHint = document.getElementById("lobby-hint");

const btnFind = document.getElementById("find-match-btn");
const btnCancel = document.getElementById("cancel-match-btn");
const btnAdmin = document.getElementById("admin-panel-btn");
const matchmakingControls = document.getElementById("matchmaking-controls");

const matchModal = document.getElementById("match-modal");
const acceptStatus = document.getElementById("accept-status");
const acceptBtn = document.getElementById("accept-match-btn");

let jwtToken = null;
let hubConnection = null;
let isAdmin = false;

let timerInterval = null;
let secondsElapsed = 0;
let currentMatchId = null;

const regionMap = { 1: "EUW", 2: "EUE", 3: "NA", 4: "Asia" };

// ===== Session Persistence (localStorage) =====

function saveSession(token) {
    localStorage.setItem("jwt", token);
}

function loadSession() {
    return localStorage.getItem("jwt");
}

function clearSession() {
    localStorage.removeItem("jwt");
}

// ===== JWT Parsing =====

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

function isTokenExpired(token) {
    const decoded = parseJwt(token);
    if (!decoded || !decoded.exp) return true;
    // exp is in seconds, Date.now() is in ms
    return Date.now() >= decoded.exp * 1000;
}

// ===== Password Toggle =====

function togglePassword() {
    const pwdInput = document.getElementById("password");
    const eyeOpen = document.getElementById("eye-icon-open");
    const eyeClosed = document.getElementById("eye-icon-closed");
    if (pwdInput.type === "password") {
        pwdInput.type = "text";
        eyeOpen.style.display = "none";
        eyeClosed.style.display = "block";
    } else {
        pwdInput.type = "password";
        eyeOpen.style.display = "block";
        eyeClosed.style.display = "none";
    }
}

// ===== Auth =====

async function register() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;
    const region = parseInt(document.getElementById("region").value);

    try {
        const response = await fetch(`${apiUrl}/api/players/register`, { 
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password, region })
        });
        if (response.ok) {
            errorText.style.color = "var(--success)";
            errorText.innerText = "Реєстрація успішна! Виконуємо вхід...";
            setTimeout(() => { login(); }, 1000);
        } else {
            errorText.style.color = "var(--danger)";
            errorText.innerText = "Помилка реєстрації. Можливо, такий гравець вже існує.";
        }
    } catch (err) {
        errorText.innerText = "Помилка з'єднання з сервером.";
    }
}

async function login() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;
    try {
        const response = await fetch(`${apiUrl}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });
        if (response.ok) {
            const data = await response.json();
            jwtToken = data.token;
            saveSession(jwtToken);
            enterLobby();
        } else {
            errorText.style.color = "var(--danger)";
            errorText.innerText = "Неправильний логін або пароль";
        }
    } catch (err) {
        errorText.innerText = "Помилка з'єднання з сервером.";
    }
}

function logout() {
    clearSession();
    jwtToken = null;
    isAdmin = false;
    if (hubConnection) {
        hubConnection.stop();
        hubConnection = null;
    }
    stopTimer();
    lobbySection.style.display = "none";
    adminSection.style.display = "none";
    authSection.style.display = "block";
    // Reset UI
    matchStatus.className = "status-text";
    matchStatus.innerText = "Готовий до гри";
    searchTimer.style.display = "none";
    btnFind.style.display = "block";
    btnCancel.style.display = "none";
    btnAdmin.style.display = "none";
    matchmakingControls.style.display = "block";
    lobbyHint.innerText = "Натисніть кнопку вище, щоб стати в чергу на пошук гри.";
}

async function enterLobby() {
    const decodedToken = parseJwt(jwtToken);
    const playerId = decodedToken.PlayerId || decodedToken.sub;
    const role = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decodedToken.role;
    
    isAdmin = (role === "Admin" || role === "1" || role === 1);
    
    await loadProfile(playerId);
    
    if (isAdmin) {
        btnAdmin.style.display = "block";
        // Admin doesn't need matchmaking controls
        matchmakingControls.style.display = "none";
        lobbyHint.innerText = "Ви увійшли як адміністратор.";
    } else {
        matchmakingControls.style.display = "block";
        lobbyHint.innerText = "Натисніть кнопку вище, щоб стати в чергу на пошук гри.";
    }

    authSection.style.display = "none";
    lobbySection.style.display = "block";
    startSignalR();
}

// ===== Auto-restore session on page load =====

window.addEventListener("DOMContentLoaded", () => {
    const savedToken = loadSession();
    if (savedToken && !isTokenExpired(savedToken)) {
        jwtToken = savedToken;
        enterLobby();
    }
});

// ===== Profile =====

async function loadProfile(playerId) {
    try {
        const response = await fetch(`${apiUrl}/api/players/${playerId}`, {
            headers: { "Authorization": `Bearer ${jwtToken}` }
        });
        if (response.ok) {
            const profile = await response.json();
            document.getElementById("player-name").innerText = profile.username;
            document.getElementById("player-mmr").innerText = Math.round(profile.mmr);
            document.getElementById("player-avatar").src = `https://api.dicebear.com/7.x/micah/svg?seed=${profile.username}&backgroundColor=transparent`;
        }
    } catch (err) {
        console.error("Помилка завантаження профілю", err);
    }
}

// ===== SignalR =====

function startSignalR() {
    if (hubConnection) return; // don't reconnect if already connected

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${apiUrl}/hubs/matchmaking`, {
            accessTokenFactory: () => jwtToken 
        })
        .withAutomaticReconnect()
        .build();
        
    hubConnection.on("MatchFound", (matchData) => {
        stopTimer();
        currentMatchId = matchData.lobbyId;
        
        matchStatus.className = "status-text found";
        matchStatus.innerText = "Гру знайдено! Очікування підтвердження...";
        searchTimer.style.display = "none";
        lobbyHint.innerText = "";
        btnCancel.style.display = "none";
        
        // Show Modal
        matchModal.classList.add("active");
        
        acceptStatus.innerText = "";
        acceptStatus.style.color = "var(--text-muted)";
        acceptBtn.disabled = false;
        acceptBtn.innerText = "ПРИЙНЯТИ";
        acceptBtn.className = "success-btn";
    });
    hubConnection.on("MatchCanceled", () => {
        matchModal.classList.remove("active");
        resetMatchUI();
        alert("Матч було відхилено або час очікування вийшов.");
    });
    
    hubConnection.start().catch(err => console.error("SignalR Connection Error: ", err));
}

function resetMatchUI() {
    matchStatus.className = "status-text";
    matchStatus.style.color = "";
    matchStatus.innerText = "Готовий до гри";
    lobbyHint.innerText = "Натисніть кнопку вище, щоб стати в чергу на пошук гри.";
    btnCancel.style.display = "none";
    btnFind.style.display = "block";
    btnFind.disabled = false;
    currentMatchId = null;
    
    acceptBtn.disabled = false;
    acceptBtn.innerText = "ПРИЙНЯТИ";
    acceptBtn.className = "success-btn";
    
    const declineBtn = document.getElementById("decline-match-btn");
    if (declineBtn) {
        declineBtn.disabled = false;
        declineBtn.innerText = "ВІДХИЛИТИ";
    }
    acceptStatus.innerText = "";
}

// ===== Timer =====

function formatTime(seconds) {
    const m = Math.floor(seconds / 60).toString().padStart(2, '0');
    const s = (seconds % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
}

function startTimer() {
    secondsElapsed = 0;
    searchTimer.innerText = "00:00";
    searchTimer.style.display = "block";
    timerInterval = setInterval(() => {
        secondsElapsed++;
        searchTimer.innerText = formatTime(secondsElapsed);
    }, 1000);
}

function stopTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
}

// ===== Matchmaking =====

async function findMatch() {
    btnFind.disabled = true;
    
    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/join`, { 
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "Authorization": `Bearer ${jwtToken}` 
            }
        });

        if (response.ok) {
            matchStatus.className = "status-text searching";
            matchStatus.innerText = "Шукаємо суперників...";
            lobbyHint.innerText = "Не закривайте сторінку, поки триває пошук гри.";
            
            btnFind.style.display = "none";
            btnCancel.style.display = "block";
            btnFind.disabled = false;
            
            startTimer();
        } else {
            matchStatus.className = "status-text";
            matchStatus.style.color = "var(--danger)";
            matchStatus.innerText = "Помилка при постановці в чергу.";
            btnFind.disabled = false;
        }
    } catch (err) {
        matchStatus.className = "status-text";
        matchStatus.style.color = "var(--danger)";
        matchStatus.innerText = "Помилка з'єднання з API.";
        btnFind.disabled = false;
    }
}

async function cancelMatch() {
    btnCancel.disabled = true;
    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/leave`, {
            method: "POST",
            headers: { "Authorization": `Bearer ${jwtToken}` }
        });
        if (response.ok) {
            stopTimer();
            searchTimer.style.display = "none";
            
            matchStatus.className = "status-text";
            matchStatus.style.color = "";
            matchStatus.innerText = "Готовий до гри";
            lobbyHint.innerText = "Натисніть кнопку вище, щоб стати в чергу на пошук гри.";
            
            btnCancel.style.display = "none";
            btnFind.style.display = "block";
            btnCancel.disabled = false;
        } else {
            matchStatus.className = "status-text";
            matchStatus.style.color = "var(--danger)";
            matchStatus.innerText = "Помилка відміни.";
            btnCancel.disabled = false;
        }
    } catch (err) {
        matchStatus.className = "status-text";
        matchStatus.style.color = "var(--danger)";
        matchStatus.innerText = "Помилка з'єднання з API.";
        btnCancel.disabled = false;
    }
}

async function acceptMatch() {
    acceptBtn.disabled = true;
    acceptBtn.innerText = "ПІДТВЕРДЖЕНО...";
    acceptBtn.className = "secondary";
    
    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/accept/${currentMatchId}`, {
            method: "POST",
            headers: { "Authorization": `Bearer ${jwtToken}` }
        });
        
        if (response.ok) {
            const data = await response.json();
            if (data.status === 1 || data.status === "Accepted") {
                acceptStatus.style.color = "var(--success)";
                acceptStatus.innerText = "Всі гравці прийняли гру! Матч починається.";
                setTimeout(() => {
                    matchModal.classList.remove("active");
                    matchStatus.innerText = "У ГРІ";
                    matchStatus.className = "status-text found";
                }, 2000);
            } else {
                acceptStatus.style.color = "var(--text-main)";
                acceptStatus.innerText = "Очікуємо іншого гравця...";
            }
        } else {
            acceptStatus.style.color = "var(--danger)";
            acceptStatus.innerText = "Помилка при підтвердженні.";
            acceptBtn.disabled = false;
        }
    } catch (err) {
        acceptStatus.style.color = "var(--danger)";
        acceptStatus.innerText = "Помилка з'єднання.";
        acceptBtn.disabled = false;
    }
}

async function declineMatch() {
    acceptBtn.disabled = true;
    const declineBtn = document.getElementById("decline-match-btn");
    if (declineBtn) {
        declineBtn.disabled = true;
        declineBtn.innerText = "ВІДХИЛЕННЯ...";
    }

    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/decline/${currentMatchId}`, {
            method: "POST",
            headers: { "Authorization": `Bearer ${jwtToken}` }
        });
        
        if (response.ok) {
            matchModal.classList.remove("active");
            resetMatchUI();
        } else {
            acceptStatus.style.color = "var(--danger)";
            acceptStatus.innerText = "Помилка відхилення.";
            acceptBtn.disabled = false;
            if (declineBtn) {
                declineBtn.disabled = false;
                declineBtn.innerText = "ВІДХИЛИТИ";
            }
        }
    } catch (err) {
        acceptStatus.style.color = "var(--danger)";
        acceptStatus.innerText = "Помилка з'єднання.";
        acceptBtn.disabled = false;
        if (declineBtn) {
            declineBtn.disabled = false;
            declineBtn.innerText = "ВІДХИЛИТИ";
        }
    }
}

// ===== Admin Panel =====

function openAdminPanel() {
    lobbySection.style.display = "none";
    adminSection.style.display = "block";
    loadAdminData();
}

function closeAdminPanel() {
    adminSection.style.display = "none";
    lobbySection.style.display = "block";
}

async function loadAdminData() {
    const playersTbody = document.querySelector("#players-table tbody");
    const matchesTbody = document.querySelector("#matches-table tbody");
    
    playersTbody.innerHTML = "<tr><td colspan='4'>Завантаження...</td></tr>";
    matchesTbody.innerHTML = "<tr><td colspan='4'>Завантаження...</td></tr>";
    
    try {
        // Players
        const playersRes = await fetch(`${apiUrl}/api/players`, {
            headers: { "Authorization": `Bearer ${jwtToken}` }
        });
        if (playersRes.ok) {
            const players = await playersRes.json();
            playersTbody.innerHTML = "";
            if (players.length === 0) {
                playersTbody.innerHTML = "<tr><td colspan='4' style='color:var(--text-muted)'>Гравців немає</td></tr>";
            } else {
                players.forEach(p => {
                    const tr = document.createElement("tr");
                    const roleName = (p.role === 1 || p.role === "Admin") ? "Admin" : "Player";
                    const regionName = regionMap[p.region] || p.region;
                    tr.innerHTML = `
                        <td>${p.username}</td>
                        <td>${p.mmr}</td>
                        <td>${regionName}</td>
                        <td>${roleName}</td>
                    `;
                    playersTbody.appendChild(tr);
                });
            }
        }
        
        // Matches
        const matchesRes = await fetch(`${apiUrl}/api/admin/matches`, {
            headers: { "Authorization": `Bearer ${jwtToken}` }
        }).catch(() => null);
        
        if (matchesRes && matchesRes.ok) {
            const matches = await matchesRes.json();
            matchesTbody.innerHTML = "";
            if (matches.length === 0) {
                matchesTbody.innerHTML = "<tr><td colspan='4' style='color:var(--text-muted)'>Матчів немає</td></tr>";
            } else {
                matches.forEach(m => {
                    const tr = document.createElement("tr");
                    const statusLower = m.status.toLowerCase();
                    const canComplete = (statusLower === "pending" || statusLower === "accepted");
                    tr.innerHTML = `
                        <td title="${m.id}">${m.id.substring(0,8)}...</td>
                        <td><span class="status-badge ${statusLower}">${m.status}</span></td>
                        <td>${m.averageMmr}</td>
                        <td>${canComplete 
                            ? `<button class="table-action-btn danger" onclick="completeMatch('${m.id}')">Finish</button>` 
                            : '-'}</td>
                    `;
                    matchesTbody.appendChild(tr);
                });
            }
        } else {
            matchesTbody.innerHTML = "<tr><td colspan='4' style='color:var(--text-muted)'>Ендпоінт /api/admin/matches ще не реалізовано.</td></tr>";
        }

    } catch (err) {
        console.error("Admin load error", err);
        playersTbody.innerHTML = "<tr><td colspan='4' style='color:var(--danger)'>Помилка завантаження</td></tr>";
    }
}

async function completeMatch(matchId) {
    // For now, we just call the existing complete endpoint.
    // The winnerId is required by your backend, so we pass a dummy value or 
    // you can extend this with a prompt to pick a winner.
    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/complete/${matchId}?winnerId=00000000-0000-0000-0000-000000000000`, {
            method: "POST",
            headers: { "Authorization": `Bearer ${jwtToken}` }
        });
        if (response.ok) {
            loadAdminData(); // refresh the table
        } else {
            const err = await response.text();
            alert("Помилка завершення матчу: " + err);
        }
    } catch (err) {
        alert("Помилка з'єднання з API.");
    }
}
