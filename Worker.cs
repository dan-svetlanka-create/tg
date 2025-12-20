using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot_Dan.Classes;
using Telegram.Bot.Args; // Для версии 22.5.1

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



        /// <summary>
        /// Метод отправки сообщений
        /// </summary>
        /// <param name="chatId">Чат Id</param>
        /// <param name="typeMessage">Тип сообщения</param>
        public async void SendMessage(long chatId, int typeMessage)
        {
            // Если тип сообщения не равен 3
            if (typeMessage != 3)
            {
                // Отправляем сообщение
                await TelegramBotClient.SendMessage(chatId, Messages[typeMessage], ParseMode.Html, replyMarkup: GetButtons());
            }
            else if (typeMessage == 3)// если тип сообщения 3
            {
                // отправляем сообщение с ошибкой даты
                await TelegramBotClient.SendMessage(chatId,
                    $"Указанное вами время и дата не могут быть установлены, " +
                    $"потому что сейчас уже : {DateTime.Now.ToString("HH.mm dd.MM.yyyy")}");
            }
        }



        /// <summary>
        /// Команды
        /// </summary>
        /// <param name="chatId">Код пользователя</param>
        /// <param name="command">Команда</param>
        public async void Command(long chatId, string command)
        {
            // если команда старт, отправляем 0 сообщение
            if (command.ToLower() == "/start")
                SendMessage(chatId, 0);
            // если команда создание задачи, отправляем 1 сообщение
            else if (command.ToLower() == "/create_task")
                SendMessage(chatId, 1);
            // если команда список задач
            else if (command.ToLower() == "/list_tasks")
            {
                // получаем пользователя, у которого совпадает чат Id
                Users User = Users.Find(x => x.IdUser == chatId);
                // если пользователь не найден, отправляем 4 сообщение
                if (User == null)
                    SendMessage(chatId, 4);
                // если количество уведомлений равно 0, отправляем 4 сообщение
                else if (User.Events.Count == 0)
                    SendMessage(chatId, 4);
                // в противном случае
                else
                {
                    // перебираем уведомления пользователя
                    foreach (Events Event in User.Events)
                    {
                        // отправляем в чат
                        await TelegramBotClient.SendMessage(
                            chatId,
                            $"Уведомить пользователя: {Event.Time.ToString("HH:mm dd.MM.yyyy")}" +
                            $"\nСообщение: {Event.Message}",
                            replyMarkup: DeleteEvent(Event.Message)
                        );
                    }
                }
            }
        }



        //получение сообщений
        private void GetMessages(Message message)
        {
            Console.WriteLine("Получено сообщение: " + message.Text + " от пользователя: " + message.Chat.Username);
            long IdUser = message.Chat.Id;
            string MessageUser = message.Text;
            //SaveToDatabaseAsync(IdUser.ToString(), MessageUser, message.Chat.Username);
            if (message.Text.Contains("/")) Command(message.Chat.Id, message.Text);
            else if (message.Text.Equals("Удалить все задачи"))
            {
                Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                if (User == null) SendMessage(message.Chat.Id, 4);
                else if (User.Events.Count == 0) SendMessage(User.IdUser, 4);
                else
                {
                    User.Events = new List<Events>();
                    SendMessage(User.IdUser, 6);
                }
            }

            else
            {
                Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                if (User == null)
                {
                    User = new Users(message.Chat.Id);
                    Users.Add(User);
                }
                //if (TryParseRepeatTask(MessageUser, out List<DayOfWeek> days, out TimeSpan repeatTime, out string repeatMessage))
               // {
                   // User.RepeatEvents.Add(new RepeatEvent(days, repeatTime, repeatMessage));
                   // TelegramBotClient.SendMessage(message.Chat.Id, "Повторяющееся напоминание добавлено!");
                   // return;
               // }
                string[] Info = message.Text.Split('\n');
                if (Info.Length < 2)
                {
                    SendMessage(message.Chat.Id, 2);
                    return;
                }

                DateTime Time;
                if (CheckFormatDateTime(Info[0], out Time) == false)
                {
                    SendMessage(message.Chat.Id, 2);
                    return;
                }

                if (Time < DateTime.Now) SendMessage(message.Chat.Id, 3);

                User.Events.Add(new Events(Time, message.Text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", "")));

            }
        }

        private async Task HandleUpdateAsync(//обновление информации
            ITelegramBotClient client,
            Update update,
            CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message) GetMessages(update.Message);

            else if (update.Type == UpdateType.CallbackQuery)
            {
                CallbackQuery query = update.CallbackQuery;
                Users User = Users.Find(x => x.IdUser == query.Message.Chat.Id);
                Events Event = User.Events.Find(x => x.Message == query.Data);
                User.Events.Remove(Event);
                SendMessage(query.Message.Chat.Id, 5);

              
            }

        }


        //получение ошибок
        private async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine("ОШИБКА: " + exception.Message);
        }


        /// <summary>
        /// Метод для периодической проверки и отправки напоминаний
        /// </summary>

        public async void Tick(object obj)
        {
            // Получаем текущее время в формате "ЧЧ:мм дд.ММ.гггг"
            // Это будет использоваться для сравнения с временем событий
            string TimeNow = DateTime.Now.ToString("HH:mm dd.MM.yyyy");

            // Перебираем всех пользователей в системе
            foreach (Users User in Users)
            {
                // Перебираем все события текущего пользователя
                // Используем for вместо foreach т.к. планируем удалять элементы
                for (int i = 0; i < User.Events.Count; i++)
                {
                    // Сравниваем время события с текущим временем
                    // Если время не совпадает – переходим к следующему событию (continue)
                    if (User.Events[i].Time.ToString("HH:mm dd.MM.yyyy") != TimeNow)
                        continue;

                    // Время совпало – отправляем напоминание пользователю
                    // Используем await для асинхронной отправки сообщения
                    await TelegramBotClient.SendMessage(
                        User.IdUser, // ID пользователя (чата) для отправки
                        "Напоминание: " + User.Events[i].Message // Текст напоминания
                    );

                    // Удаляем отправленное событие из списка пользователя
                    User.Events.Remove(User.Events[i]);
                    i--; // Уменьшаем счетчик, так как удалили элемент
                }
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Инициализируем телеграмм клиент, указывая токен
            TelegramBotClient = new TelegramBotClient(Token);
            // Запускаем прослушивание сообщений от пользователя
            TelegramBotClient.StartReceiving(
            HandleUpdateAsync, // указываем метод, получающий сообщения и callback
            HandleErrorAsync, // указываем метод, обрабатывающий возникающие ошибки
            null,
            new CancellationTokenSource().Token // Указываем токен
            );
            // Создаём callback метод, срабатывающий на тиканье таймера
            TimerCallback TimerCallback = new TimerCallback(Tick);
            // Запускаем таймер, с переодичностью 1 минуту
            Timer = new Timer(TimerCallback, 0, 0, 60 * 1000);
        }




    }
}


