using Microsoft.EntityFrameworkCore;

namespace Server
{
    public class UserService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<List<TopUser>> GetTopUsersAsync(int count = 100)
        {
            var usersFromDb = await _context.Users
                .Where(u => u.TotalAmount > 0)
                .OrderByDescending(u => u.TotalAmount)
                .Take(count)
                .ToListAsync();

            // Присваиваем ранги (индексы) пользователям
            return usersFromDb
                .Select((u, index) => new TopUser
                {
                    Rank = index + 1, // Индекс + 1, чтобы начинать с 1
                    FirstName = u.FirstName,
                    PhotoURL = u.PhotoURL ?? string.Empty,
                    TotalAmount = u.TotalAmount
                })
                .ToList();
        }
        public async Task<List<UCode>> GetUCodeByDateAsync(DateTime? fromDate, DateTime? toDate, bool? isActivated, bool? isTbk)
        {
            // Получаем все записи из базы
            var query = _context.UCodes.AsQueryable();

            // Применяем фильтрацию по дате, если указана
            if (fromDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= toDate.Value);
            }

            // Применяем фильтрацию по IsActivated, если указана
            if (isActivated.HasValue)
            {
                query = query.Where(u => u.IsActivated == isActivated.Value);
            }
            if (isTbk.HasValue)
            {
                query = query.Where(u => u.IsTbk == isTbk.Value);
            }
            Console.WriteLine($"fromDate={fromDate},toDate={toDate}, isActivated={isActivated}, isTbk={isTbk}");
            // Выполняем запрос и возвращаем результат
            return await query.ToListAsync();
        }
        public async Task<bool> SetIsTbkAsync(int id, bool value)
        {
            var uCode = await _context.UCodes.FindAsync(id);

            if (uCode == null)
            {
                return false; // Код не найден
            }

            // Устанавливаем IsTbk в переданное значение (true или false)
            uCode.IsTbk = value;

            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();

            return true; // Успешное обновление
        }

        public async Task<UCode> GenerateUCodeAsync(int amount)
        {
            var newCode = new UCode
            {
                Code = Guid.NewGuid().ToString()[..8].ToUpper(),
                Amount = amount,
                IsActivated = false,
                CreatedAt = DateTime.Now
            };

            _context.UCodes.Add(newCode);
            await _context.SaveChangesAsync();

            return newCode;
        }
        public async Task<List<UCode>> GetUCodesLastHourAsync()
        {
            // Получаем текущее время
            var currentTime = DateTime.Now;

            // Вычисляем время 1 час назад
            var oneHourAgo = currentTime.AddHours(-1);

            // Запрашиваем все UCode, созданные за последний час
            var codesLastHour = await _context.UCodes
                .Where(u => u.CreatedAt >= oneHourAgo)
                .OrderByDescending(u => u.CreatedAt) // Сортируем по времени создания
                .ToListAsync();

            return codesLastHour;  // Если нет данных, вернётся пустой список
        }

        public async Task<User> IdentifyUserAsync(long telegramId, string firstName, string? photoUrl)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
            if (user == null)
            {
                user = new User
                {
                    TelegramId = telegramId,
                    FirstName = firstName,
                    TotalAmount = 0,
                    PhotoURL = photoUrl ?? string.Empty
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else if (user.FirstName != firstName)
            {
                user.FirstName = firstName;
                await _context.SaveChangesAsync();
            }

            return user;
        }

        public async Task<int> ActivateUCodeAsync(string code, long telegramId)
        {
            var ucode = await _context.UCodes.FirstOrDefaultAsync(u => u.Code == code && !u.IsActivated)
                ?? throw new Exception("Code not found or already activated");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId)
                ?? throw new Exception("User not found");
            ucode.IsActivated = true;
            ucode.UserId = user.Id;
            user.TotalAmount += ucode.Amount;

            await _context.SaveChangesAsync();

            return user.TotalAmount;
        }
    }
}