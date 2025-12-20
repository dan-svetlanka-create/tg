using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TelegramBot_Dan.Classes
{
    public class Users
    {
        public int Id { get; set; }
        public long IdUser { get; set; }
        public string Username { get; set; } = string.Empty; // <-- Добавьте это свойство и значение по умолчанию
        public virtual List<Events> Events { get; set; } = new List<Events>(); // <-- Инициализирован по умолчанию и добавлено virtual

        public virtual List<RecurrencePattern> RecurrencePattern { get; set; } = new List<RecurrencePattern>(); // <-- Инициализирован по умолчанию и добавлено virtual

        public Users(long idUser)
        {
            IdUser = idUser;
            Events = new List<Events>();
            RecurrencePattern = new List<RecurrencePattern>();
        }
        // Добавьте пустой конструктор для EF
        public Users()
        {
            Events = new List<Events>();
            RecurrencePattern = new List<RecurrencePattern>();
        }
    }
}

