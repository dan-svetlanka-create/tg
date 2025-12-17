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
        /// <summary> Код пользователя
        public long IdUser { get; set; }

        /// <summary> События пользователя
        public List<Events> Events { get; set; }

        /// <summary> Конструктор для класса
        public Users(long idUser)
        {
            IdUser = idUser; // запоминаем Id чата пользователя
            Events = new List<Events>(); // инициализируем список событий
        }
    }
}
}
