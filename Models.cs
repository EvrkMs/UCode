namespace Server
{
#pragma warning disable CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
    public class User
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string FirstName { get; set; }
        public int TotalAmount { get; set; } // Изменено с decimal на int
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string PhotoURL { get; set; }
        public ICollection<UCode> UCodes { get; set; }
    }

    public class UCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int Amount { get; set; } // Изменено с decimal на int
        public bool IsActivated { get; set; } = false;
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Новый столбец с дефолтным значением false
        public bool IsTbk { get; set; } = false;
    }
    public class TopUser
    {
        public int Rank { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string PhotoURL { get; set; } = string.Empty;
        public int TotalAmount { get; set; }
    }
#pragma warning restore CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
}