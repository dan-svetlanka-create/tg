using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot_Dan.Classes
{
    public class RecurrencePattern
    {
        public int Id { get; set; } // <-- ДОБАВЬТЕ ЭТУ СТРОКУ
        public List<DayOfWeek> Days { get; set; } = new List<DayOfWeek>(); // <-- Инициализирован по умолчанию
        public TimeSpan Time { get; set; }
        public string Message { get; set; } = string.Empty; // <-- Добавлено значение по умолчанию

        public RecurrencePattern(List<DayOfWeek> days, TimeSpan time, string message)
        {
            Days = days ?? new List<DayOfWeek>();
            Time = time;
            Message = message ?? string.Empty;
        }
        // Добавьте пустой конструктор для Entity Framework
        public RecurrencePattern() { } // <-- ДОБАВЬТЕ ЭТУ СТРОКУ
    }
}
