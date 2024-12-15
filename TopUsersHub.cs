using Microsoft.AspNetCore.SignalR;

namespace Server
{
    public class TopUsersHub(UserService userService, ILogger<TopUsersHub> logger) : Hub
    {
        private readonly UserService _userService = userService;
        private readonly ILogger<TopUsersHub> _logger = logger;

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Клиент подключен: {ConnectionId}", Context.ConnectionId);

            // Получаем список пользователей и отправляем клиенту
            var topUsers = await _userService.GetTopUsersAsync();
            await Clients.Caller.SendAsync("ReceiveTopUsers", topUsers);

            await base.OnConnectedAsync();
        }

        public async Task BroadcastTopUsers()
        {
            _logger.LogInformation("Начало трансляции данных (BroadcastTopUsers).");

            var topUsers = await _userService.GetTopUsersAsync();
            foreach (var (user, rank) in topUsers.Select((user, index) => (user, index + 1)))
            {
                _logger.LogInformation("Место: {Rank}, Имя: {FirstName}, Баллы: {TotalAmount}, Фото: {PhotoURL}",
                    rank, user.FirstName, user.TotalAmount, user.PhotoURL);
            }

            await Clients.All.SendAsync("ReceiveTopUsers", topUsers);

            _logger.LogInformation("Данные отправлены клиентам.");
        }
    }
}