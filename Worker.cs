using Telegram.Bot;
using TelegramBot_Dan.Classes;

namespace TelegramBot_Dan
{
    public class Worker : BackgroundService
    {

        /// <summary> Токен телеграмм бота
        readonly string token = "полученный телеграмм токен";

        /// <summary> Клиент через который будет происходить взаимодействие с TelegramBotClient
        TelegramBotClient TelegramBotClient;

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
            "\n<b>Напомни о том что я хотел сходить в магазин.</i>",

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
    }
}
