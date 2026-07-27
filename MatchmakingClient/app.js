const apiUrl = "http://localhost:5001";

const authSection = document.getElementById("auth-section");
const lobbySection = document.getElementById("lobby-section");
const errorText = document.getElementById("auth-error");
const matchStatus = document.getElementById("match-status");
const btnFind = document.getElementById("find-match-btn");
const btnCancel = document.getElementById("cancel-match-btn");

let jwtToken = null;
let hubConnection = null;

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
            errorText.style.color = "lightgreen";
            errorText.innerText = "Реєстрація успішна! Тепер натисни 'Увійти'.";
        } else {
            errorText.style.color = "#ff4d4d";
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
            
            const decodedToken = parseJwt(jwtToken);
            const playerId = decodedToken.PlayerId || decodedToken.sub;
            
            await loadProfile(playerId);
            authSection.style.display = "none";
            lobbySection.style.display = "block";
            startSignalR();
        } else {
            errorText.style.color = "#ff4d4d";
            errorText.innerText = "Неправильний логін або пароль";
        }
    } catch (err) {
        errorText.innerText = "Помилка з'єднання з сервером.";
    }
}

async function loadProfile(playerId) {
    try {
        const response = await fetch(`${apiUrl}/api/players/${playerId}`);
        if (response.ok) {
            const profile = await response.json();
            document.getElementById("player-name").innerText = profile.username;
            document.getElementById("player-mmr").innerText = Math.round(profile.mmr);
            
            document.getElementById("player-avatar").src = `https://api.dicebear.com/7.x/avataaars/svg?seed=${profile.username}`;
        }
    } catch (err) {
        console.error("Помилка завантаження профілю", err);
    }
}


function startSignalR() {
    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${apiUrl}/hubs/matchmaking`, {
            accessTokenFactory: () => jwtToken 
        })
        .withAutomaticReconnect()
        .build();
    hubConnection.on("MatchFound", (matchData) => {
        matchStatus.style.color = "lightgreen";
        matchStatus.innerText = `МАТЧ ЗНАЙДЕНО! Лобі ID: ${matchData.lobbyId}`;
        
        btnFind.innerText = "У ГРІ";
        btnFind.style.backgroundColor = "#28a745";
        btnFind.style.display = "inline-block";
        btnFind.disabled = true;
        
        btnCancel.style.display = "none";
    });
    hubConnection.start().catch(err => console.error("SignalR Connection Error: ", err));
}


async function findMatch() {
    matchStatus.style.color = "white";
    matchStatus.innerText = "Пошук матчу... ";

    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/join`, { 
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "Authorization": `Bearer ${jwtToken}` 
            }
        });

        if (response.ok) {
            btnFind.style.display = "none";
            btnCancel.style.display = "inline-block";
        } else {
            matchStatus.style.color = "#ff4d4d";
            matchStatus.innerText = "Помилка при постановці в чергу.";
        }
    } catch (err) {
        matchStatus.innerText = "Помилка з'єднання з API.";
    }
}

async function cancelMatch() {
    matchStatus.innerText = "Відміна...";
    try {
        const response = await fetch(`${apiUrl}/api/matchmaking/leave`, {
            method: "POST",
            headers: { 
                "Authorization": `Bearer ${jwtToken}` 
            }
        });
        if (response.ok) {
            matchStatus.innerText = "";
            btnCancel.style.display = "none";
            btnFind.style.display = "inline-block";
        } else {
            matchStatus.style.color = "#ff4d4d";
            matchStatus.innerText = "Помилка відміни.";
        }
    } catch (err) {
        matchStatus.innerText = "Помилка з'єднання з API.";
    }
}


