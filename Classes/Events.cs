using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot_Dan.Classes
{
    public class Events
    {
        public int Id { get; set; } // <-- ДОБАВЬТЕ ЭТУ СТРОКУ

        /// <summary> Время в которое необходимо уведомить пользователя
        public DateTime Time { get; set; }

        /// <summary> Сообщение которое будет отправлено пользователем
        public string Message { get; set; } = string.Empty; // <-- Добавлено значение по умолчанию

        /// <summary> Конструктор для класса
        public Events(DateTime time, string message)
        {
            Time = time; // запоминаем время когда необходимо уведомить пользователя
            Message = message ?? string.Empty; // запоминаем сообщение
        }

        // Добавьте пустой конструктор для Entity Framework
        public Events() { } // <-- ДОБАВЬТЕ ЭТУ СТРОКУ
    }
}
