using Microsoft.AspNetCore.Mvc;

namespace Server
{
    [Route("api/[controller]")]
    [ApiController]
    public class UCodeController(UserService userService) : ControllerBase
    {

        // API метод для изменения IsTbk на заданное значение (true или false)
        [HttpPost("setIsTbk")]
        public async Task<IActionResult> SetIsTbk([FromBody] SetIsTbkRequest request)
        {
            // Проверяем, что пришел корректный запрос
            if (request == null || request.Id <= 0)
            {
                return BadRequest(new { error = "Invalid request" });
            }

            // Вызываем сервис для изменения IsTbk на заданное значение
            var result = await userService.SetIsTbkAsync(request.Id, request.Action);

            if (!result)
            {
                // Если код не найден, возвращаем ошибку
                return NotFound(new { error = "UCode not found" });
            }

            // Возвращаем успешный ответ
            return Ok(new { message = $"IsTbk successfully set to {request.Action}" });
        }

        [HttpGet("get")]
        public async Task<IActionResult> Get([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] bool? isActivated,[FromQuery] bool? isTbk)
        {
            // Получаем данные из сервиса
            var data = await userService.GetUCodeByDateAsync(fromDate, toDate, isActivated, isTbk);

            // Если записи отсутствуют, возвращаем 404
            if (data.Count == 0)
            {
                return NotFound(new { error = "No UCodes found for the given criteria" });
            }

            return Ok(data);
        }
    }
    public class SetIsTbkRequest
    {
        public int Id { get; set; }
        public bool Action { get; set; }
    }
}
