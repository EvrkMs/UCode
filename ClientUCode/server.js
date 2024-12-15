const express = require('express');
const axios = require('axios');
const path = require('path');
const signalR = require('@microsoft/signalr');
const fs = require('fs'); // Для работы с файлами
const helmet = require('helmet');
const app = express();

const TOP_USERS_FILE = path.join(__dirname, 'topUsers.json');
// Загружаем данные топ-10 пользователей из файла
let topUsers = [];
if (fs.existsSync(TOP_USERS_FILE)) {
    topUsers = JSON.parse(fs.readFileSync(TOP_USERS_FILE, 'utf-8'));
}
app.use(
    helmet.contentSecurityPolicy({
        directives: {
            defaultSrc: ["'self'"],
            scriptSrc: [
                "'self'",
                "'unsafe-inline'", // Telegram WebApp требует 'unsafe-inline'
                "https://telegram.org",
                "*.telegram.org",
                "*.cloudflare.com",
                "*.cloudflareinsights.com",
                "static.cloudflareinsights.com",
            ],
            styleSrc: ["'self'", "'unsafe-inline'"], // Разрешаем inline-стили
            imgSrc: [
                "'self'",
                "data:",
                "https://t.me",
                "*.t.me",
                "*.telegram.org",
                "*.cdn-telegram.org",
                "cdn-telegram.org",
                "ava-kk.com",
            ],
            connectSrc: [
                "'self'",
                "https://telegram.org",
                "wss://telegram.org",
                "https://cdn.jsdelivr.net",
            ],
            frameSrc: [
                "'self'",
                "https://telegram.org",
                "*.telegram.org",
                "https://web.telegram.org",
                "https://cdn.jsdelivr.net",
            ],
            objectSrc: ["'none'"], // Отключаем object, embed и applet
            reportUri: '/csp-violation' // Указываем URL для отчетов
        },
    })
);
// Обработка отчетов CSP
app.post('/csp-violation', express.json(), (req, res) => {
    console.error('CSP violation:', req.body);
    res.status(204).end();
    res.render('csp-violation');
});
// Устанавливаем EJS как шаблонизатор
app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

// Подключение статической папки
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// Создаём подключение к SignalR-хабу
const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/topUsers')
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Функция для обработки события получения данных о пользователях
connection.on('ReceiveTopUsers', (users) => {
    console.log('Обновление топ-10 пользователей:');
    if (users.length > 0) {
        users.forEach(user => {
            console.log(`rank: ${user.rank} Имя: ${user.firstName}, Баллы: ${user.totalAmount}, Фото: ${user.photoURL || 'Отсутствует'}`);
        });
    } else {
        console.log('Список пуст.');
    }

    // Обновляем локальное хранилище
    topUsers = users;

    // Сохраняем обновлённые данные в файл
    fs.writeFileSync(TOP_USERS_FILE, JSON.stringify(topUsers, null, 2));
});

// Запускаем подключение к SignalR с переподключением
async function startSignalRConnection() {
    try {
        await connection.start();
        console.log('Подключение к SignalR успешно установлено.');
    } catch (err) {
        console.error('Ошибка подключения к SignalR:', err.message);
        console.log('Попытка переподключения через 5 секунд...');
        setTimeout(startSignalRConnection, 5000);
    }
}

// Обработка отключения
connection.onclose(async (error) => {
    console.error('Соединение с SignalR потеряно. Причина:', error ? error.message : 'Неизвестно');
    console.log('Попытка переподключения через 5 секунд...');
    setTimeout(startSignalRConnection, 5000);
});

// Запускаем подключение
startSignalRConnection();

// Маршрут для рендеринга страницы с топ-10
app.get('/', (req, res) => {
    res.render('index', { topUsers });
    return;
});
app.get('/csp-violation', (req, res) => {
    res.render('csp-violation');
    return;
});

// Маршрут для обработки ошибок
app.get('/error', (req, res) => {
    res.render('csp-violation');
    return;
});

// Конфигурация API-ключа
const API_KEY = 'your-secure-api-key'; // Секретный API-ключ, недоступный клиенту

// Маршрут для активации промокодов
app.post('/activate', async (req, res) => {
    const { code, telegramId } = req.body;

    try {
        // Отправляем запрос на бэкенд с API-ключом
        const response = await axios.post('http://localhost:5000/api/Users/activateUCode', {
            code,
            telegramId,
        }, {
            headers: {
                'X-API-KEY': API_KEY // Передача API-ключа на сервер
            }
        });

        res.json(response.data);
    } catch (error) {
        console.error("Ошибка активации промокода:", error.message);
        res.status(500).json({ error: "Не удалось активировать промокод." });
    }
});

// Маршрут для идентификации пользователя
app.post('/identify', async (req, res) => {
    const { telegramId } = req.body;

    try {
        // Отправляем запрос на бэкенд с API-ключом
        const response = await axios.post('http://localhost:5000/api/Users/identify', {
            TelegramId: telegramId,
        }, {
            headers: {
                'X-API-KEY': API_KEY // Передача API-ключа на сервер
            }
        });

        res.json(response.data);
    } catch (error) {
        console.error("Ошибка идентификации пользователя:", error.message);
        res.status(500).json({ error: "Не удалось идентифицировать пользователя." });
    }
});
// Запуск сервера
app.listen(6000, () => console.log('Сервер запущен на http://localhost:6000'));
