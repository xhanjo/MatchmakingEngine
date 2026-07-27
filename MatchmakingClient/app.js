const { use } = require("react");

const apiUrl = "http://localhost:5001";

const authSection = document.getElementById("auth-section");
const lobbySection = document.getElementById("lobby-section");
const errorText = document.getElementById("auth-error");
const matchStatus = document.getElementById("match-status");
const playerNameSpan = document.getElementById("player-name");

let jwtToken = null;
let hubConnection = null;

async function register() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;
    const region = document.getElementById("region").value;

    try {
        const response = await fetch(`${apiUrl}/players`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password, region })
        });

        if (response.ok) {
            errorText.style.color = "lightgreen";
            errorText.innerText = "Реєстрація успішна! Тепер натисніть 'Увійти'.";
        } else {
            errorText.style.color = "#ff4d4d";
            errorText.innerText = "Помилка реєстрації. Перевірте дані.";
        }
    } catch (err) {
        errorText.innerText = "Помилка з'єднання з сервером.";
    }
}

async function login() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    try {
        const response = await fetch(`${apiUrl}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            const data = await response.json();
            jwtToken = data.token;

            authSection.style.display = "none";
            lobbySection.style.display = "block";
            playerNameSpan.innerText = username;

            startSignalR();
        } else {
            errorText.style.color = "#ff4d4d";
            errorText.innerText = "Неправильний логін або пароль";
        }
    } catch (err) {
        errorText.innerText = "Помилка з'єднання з сервером.";
    }
}

function startSignalR() {
    hubConnection = new startSignalR.hubConnectionBuilder()
        .withUrl(`${apiUrl}/hubs/matchmaking`, {
            accessTokenFactory: () => jwtToken
        })
        .withAutomaticReconnect()
        .build();

        hubConnection.on("MatchFound", (matchData) => {
            console.log("Отримано подію від сервера:", matchData);
            matchStatus.style.color = "lightgreen";
            matchStatus.innerText = `Матч знайдено! Лобі ID: ${matchData.lobbyId}`;

            const btn = document.getElementById("find-match-btn");
            btn.innerText = "У гру";
            btn.style.backgroundColor = "#28a745";
            btn.disabled = true;
        });

        hubConnection.start()
        .then(() => console.log("SignalR Connected!"))
        .catch(err => console.error("SignalR Connection error: ", err));
}

async function findMatch() {
    matchStatus.style.color = "white";
    matchStatus.innerText = "Пошук матчу... ";

    try {
        const response = await fetch(`${apiUrl}/matchmaking/queue`, {
            method: "POST", 
            headers: {
                "Content-Type" : "application/json",
                "Authorization": `Bearer ${jwtToken}`
            },
            body: JSON.stringify({})
        });

        if (response.ok) {
            matchStatus.style.color = "#ff4d4d";
            matchStatus.innerText = "Помилка при постановці в чергу.";
        }
    } catch (err) {
        matchStatus.style.color = "#ff4d4d";
        matchStatus.innerText = "Помилка з'єднання з API.";
    }
}

