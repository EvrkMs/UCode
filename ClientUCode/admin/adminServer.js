const express = require('express');
const axios = require('axios');
const path = require('path');

const app = express();

app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views_admin'));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const ADMIN_TELEGRAM_ID = '6707816107'; // ID администратора

// Рендеринг панели администратора
app.get('/', (req, res) => {
    res.render('admin', { message: null, error: null }); // Без проверок
});
app.get('/error', (req, res) => {
    res.redirect('https://ava-kk.com/error');
});
// Проверка Telegram ID
app.post('/verify-admin', (req, res) => {
    const telegramId = req.body.telegramId?.toString().trim(); // Приводим к строке и убираем пробелы
    console.log(`Полученный Telegram ID: "${telegramId}"`);

    if (!telegramId) {
        console.log('Telegram ID отсутствует');
        return res.status(400).json({ error: 'Telegram ID отсутствует' });
    }

    if (telegramId === ADMIN_TELEGRAM_ID) {
        console.log('Доступ разрешён');
        return res.json({ isAdmin: true });
    }

    console.log(`Доступ запрещён для Telegram ID: "${telegramId}"`);
    return res.status(403).json({ error: 'Недостаточно прав' });
});

// Обработка генерации промокодов
app.post('/generate', async (req, res) => {
    const { amount } = req.body;

    try {
        const response = await axios.post(
            'http://localhost:5000/api/Users/addUCode',
            {
                amount: parseFloat(amount),
            },
            {
                headers: {
                    'X-API-KEY': 'your-secure-api-key',
                }
            }
        );

        res.render('admin', {
            message: `Промокод "${response.data.code}" на сумму ${response.data.amount} успешно создан.`,
            error: null,
        });
    } catch (error) {
        console.error('Ошибка генерации промокода:', error.response?.data || error.message);
        res.render('admin', {
            message: null,
            error: 'Ошибка генерации промокода.',
        });
    }
});

app.listen(6005, () => console.log('Admin panel is running on http://localhost:6005'));
