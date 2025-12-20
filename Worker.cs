using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot_Dan.Classes;

namespace TelegramBot_Dan
{
    public class Worker : BackgroundService
    {

        /// <summary> Токен телеграмм бота
        readonly string Token = "8335328272:AAFDyJHuPn_EMZ4ngQcrgYORj0esLU-NIy8";

        /// <summary> Клиент через который будет происходить взаимодействие с TelegramBotClient
        TelegramBotClient TelegramBotClient;
        //private readonly IServiceScopeFactory _scopeFactory;
        /// <summary> Список пользователей
        List<Users> Users = new List<Users>();

        /// <summary> Таймер для рассылки напоминаний
        Timer Timer;

        /// <summary> Список сообщений, отправляемых пользователю
        List<string> Messages = new List<string>() 
        {

            "Здравствуйте!" +
            "\nРад приветствовать вас в Telegram-боте «Напоминатор»!" +
            "\nНаш бот создан для того, чтобы напоминать вам о важных событиях и мероприятиях. С ним вы точно не пропустите ничего важного!" +
            "\nНе забудьте добавить бота в список своих контактов и настроить уведомления. Тогда вы всегда будете в курсе событий!",

            "Укажите дату и время напоминания в следующем формате" +
            "\n<b>12:51 26.04.2025</b>" +
            "\n<b>Напомни о том что я хотел сходить в магазин.</i>" ,

            "Кажется, что-то не получилось." + 
            "\nУкажите дату и время напоминания в следующем формате:" +
            "\n</i><b>12:51 26.04.2025</b>" +
            "\n<b>Напомни о том что я хотел сходить в магазин.</i>",//

            "",
            "Задачи пользователя не найдены.",
            "Событие удалено.",
            "Все события удалены."
        };

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
        /// <summary>
        /// Проверка корректности ввода даты и времени
        /// </summary>
        /// <param name="value">Значение которое будет преобразовано в дату</param>
        /// <param name="time">Значение в которое будет записана скорректированная дата и время</param>
        /// <returns>Результат преобразования значения в дату и время</returns>
        public bool CheckFormatDateTime(string value, out DateTime time)
        {
            return DateTime.TryParse(value, out time);
        }


        /// <summary>
        /// Создание кнопки для удаления всех задач
        /// </summary>
        /// <returns>Список кнопки</returns>
        private static ReplyKeyboardMarkup GetButtons()
        {
            // Создаём список кнопок
            List<KeyboardButton> keyboardButtons = new List<KeyboardButton>();
            // Добавляем в список одну кнопку
            keyboardButtons.Add(new KeyboardButton("Удалить все задачи"));
            // Возвращаем список в списке
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    keyboardButtons
                }
            };
        }


        /// <summary>
        /// Создание кнопки для удаления конкретного события
        /// </summary>
        /// <param name="Message">Сообщение</param>
        /// <returns>Список кнопок</returns>
        public static InlineKeyboardMarkup DeleteEvent(string Message)
        {
            // Создаём список кнопок
            List<InlineKeyboardButton> inlineKeyboards = new List<InlineKeyboardButton>();
            // Добавляем кнопку удалить, прикрепляя к ней текст, который необходимо удалить
            inlineKeyboards.Add(new InlineKeyboardButton("Удалить", Message));
            // Возвращаем список кнопок
            return new InlineKeyboardMarkup(inlineKeyboards);
        }


    }
}


