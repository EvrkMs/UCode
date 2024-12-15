using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Server
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(UserService userService, IHubContext<TopUsersHub> hubContext, ILogger<UsersController> logger) : ControllerBase
    {
        private readonly UserService _userService = userService;
        private readonly IHubContext<TopUsersHub> _hubContext = hubContext;
        private readonly ILogger<UsersController> _logger = logger;

        private const string ApiKey = "your-secure-api-key"; // Ваш секретный API-ключ

        // Метод для проверки X-API-KEY
        private static bool IsApiKeyValid(HttpRequest request)
        {
            return request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey) && extractedApiKey == ApiKey;
        }

        [HttpPost("addUCode")]
        public async Task<IActionResult> AddUCode([FromBody] AmountRequest request)
        {
            // Проверка X-API-KEY
            if (!IsApiKeyValid(Request))
            {
                return Unauthorized(new { error = "Invalid API Key" });
            }

            if (request == null || request.Amount <= 0)
            {
                return BadRequest("Amount must be a positive number.");
            }

            // Генерация нового кода
            var newCode = await _userService.GenerateUCodeAsync(request.Amount);

            // Возвращаем только строку "code"
            return Ok(new { code = newCode.Code });
        }

        [HttpPost("identify")]
        public async Task<IActionResult> Identify([FromBody] JsonElement requestBody)
        {
            // Проверка X-API-KEY
            if (!IsApiKeyValid(Request))
            {
                return Unauthorized(new { error = "Invalid API Key" });
            }

            try
            {
                if (!requestBody.TryGetProperty("TelegramId", out JsonElement telegramUser))
                {
                    _logger.LogWarning("Telegram user data is missing in request: {TelegramUser}", telegramUser);
                    return BadRequest(new { error = "Telegram user data is missing." });
                }

                long telegramId = telegramUser.GetProperty("id").GetInt64();
                string? firstName = telegramUser.GetProperty("first_name").GetString();
                string? photoUrl = telegramUser.GetProperty("photo_url").GetString();

                if (telegramId <= 0 || string.IsNullOrWhiteSpace(firstName))
                {
                    _logger.LogWarning("Invalid Telegram user data: TelegramId = {TelegramId}, FirstName = {FirstName}", telegramId, firstName);
                    return BadRequest(new { error = "Invalid Telegram user data.", receivedData = telegramUser });
                }

                var user = await _userService.IdentifyUserAsync(telegramId, firstName, photoUrl);

                return Ok(new { user.TotalAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, new { error = "Failed to identify user.", exception = ex.Message });
            }
        }

        [HttpPost("activateUCode")]
        public async Task<IActionResult> ActivateUCode([FromBody] ActivateCodeRequest request)
        {
            // Проверка X-API-KEY
            if (!IsApiKeyValid(Request))
            {
                return Unauthorized(new { error = "Invalid API Key" });
            }

            if (string.IsNullOrWhiteSpace(request.Code) || request.TelegramId <= 0)
            {
                return BadRequest("Invalid activation data.");
            }

            try
            {
                var updatedAmount = await _userService.ActivateUCodeAsync(request.Code, request.TelegramId);

                var topUsers = await _userService.GetTopUsersAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveTopUsers", topUsers);

                return Ok(new { TotalAmount = updatedAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка активации UCode.");
                return StatusCode(500, new { error = "Failed to activate UCode.", exception = ex.Message });
            }
        }
        [HttpGet("getUCodesLastHour")]
        public async Task<IActionResult> GetUCodesLastHour()
        {
            // Проверка X-API-KEY
            if (!IsApiKeyValid(Request))
            {
                return Unauthorized(new { error = "Invalid API Key" });
            }

            try
            {
                // Получаем список промокодов, созданных за последний час
                var uCodes = await _userService.GetUCodesLastHourAsync();

                // Возвращаем пустой массив, если промокоды не найдены
                return Ok(uCodes);  // даже если uCodes пуст, будет возвращён пустой массив
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching promo codes.");
                return StatusCode(500, new { error = "An error occurred while fetching promo codes." });
            }
        }
    }

    public class AmountRequest
    {
        public int Amount { get; set; }
    }

    public class ActivateCodeRequest
    {
        public string? Code { get; set; }
        public long TelegramId { get; set; }
    }
}
