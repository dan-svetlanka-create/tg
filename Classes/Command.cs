using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot_Dan.Classes
{
    public class Command
    {
        public int Id { get; set; }
        public string User { get; set; } = string.Empty; // <-- Добавлено значение по умолчанию
        public string Commands { get; set; } = string.Empty; // <-- Добавлено значение по умолчанию


    }
}
