using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot_Dan.Classes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace TelegramBot_Dan
{
    public class Worker : BackgroundService
    {
        /// <summary> Токен телеграмм бота
        readonly string Token = "8230262157:AAEReyPCEK42em_gzsAhSPqZ-1kCMu5aqSQ";

        /// <summary> Клиент через который будет происходить взаимодействие с TelegramBotClient
        TelegramBotClient TelegramBotClient;

        private readonly IServiceScopeFactory _scopeFactory;
        /// <summary> Список пользователей
        List<Users> Users = new List<Users>();

        /// <summary> Таймер для рассылки напоминаний
        Timer Timer;

        /// <summary> Список сообщений, отправляемых пользователю
        List<string> Messages = new List<string>()
        {
           // 0
           "Здравствуйте!" +
           "\nРад приветствовать вас в Telegram-боте «Напоминатор»!" +
           "\nНаш бот создан для того, чтобы напоминать вам о важных событиях и мероприятиях. С ним вы точно не пропустите ничего важного!" +
           "\nНе забудьте добавить бота в список своих контактов и настроить уведомления. Тогда вы всегда будете в курсе событий!",

           // 1
           "Укажите дату и время напоминания в следующем формате" +
           "\n<b>12:51 26.04.2025</b>" +
           "\n<b>Напомни о том что я хотел сходить в магазин.</b>",

           // 2
           "Кажется, что-то не получилось." +
           "\nУкажите дату и время напоминания в следующем формате:" +
           "\n<b>12:51 26.04.2025</b>" +
           "\n<b>Напомни о том что я хотел сходить в магазин.</b>",

           // 3
           "Указанное вами время и дата не могут быть установлены, " +
           "потому что сейчас уже: {0}",

           // 4
           "Задачи пользователя не найдены.",

           // 5
           "Событие удалено.",

           // 6
           "Все события удалены.",

           // 7
           "",

           // 8
           "Создание повторяющейся задачи!" +
           "\nУкажите дни недели и время в формате:" +
           "\n<b>Каждую среду и воскресенье в 21:00 Полить цветы</b>" +
           "\n\nДоступные дни недели: понедельник, вторник, среда, четверг, пятница, суббота, воскресенье"
        };

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Вспомогательный метод для FirstOrDefault в старой версии EF
        /// </summary>
        private async Task<T> FirstOrDefaultAsync<T>(IQueryable<T> query, Func<T, bool> predicate)
        {
            var list = await query.ToListAsync();
            return list.FirstOrDefault(predicate);
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
        /// Проверка формата повторяющейся задачи
        /// </summary>
        public bool CheckRepeatTask(string input, out List<DayOfWeek> days, out TimeSpan time, out string message)
        {
            days = new List<DayOfWeek>();
            time = TimeSpan.Zero;
            message = string.Empty;

            try
            {
                // Пример: "каждую среду и воскресенье в 21:00 полить цветы"
                if (!input.ToLower().Contains("каждую") && !input.ToLower().Contains("каждый"))
                    return false;

                // Находим время
                var timeMatch = Regex.Match(input, @"\b(\d{1,2}):(\d{2})\b");
                if (!timeMatch.Success)
                    return false;

                int hours = int.Parse(timeMatch.Groups[1].Value);
                int minutes = int.Parse(timeMatch.Groups[2].Value);
                time = new TimeSpan(hours, minutes, 0);

                // Находим дни недели
                var dayNames = new Dictionary<string, DayOfWeek>
                {
                    ["понедельник"] = DayOfWeek.Monday,
                    ["вторник"] = DayOfWeek.Tuesday,
                    ["среда"] = DayOfWeek.Wednesday,
                    ["четверг"] = DayOfWeek.Thursday,
                    ["пятница"] = DayOfWeek.Friday,
                    ["суббота"] = DayOfWeek.Saturday,
                    ["воскресенье"] = DayOfWeek.Sunday
                };

                string lowerInput = input.ToLower();
                foreach (var dayPair in dayNames)
                {
                    if (lowerInput.Contains(dayPair.Key))
                    {
                        days.Add(dayPair.Value);
                    }
                }

                if (days.Count == 0)
                    return false;

                // Находим сообщение (все что после "в XX:XX")
                int timeIndex = input.IndexOf(timeMatch.Value);
                if (timeIndex + timeMatch.Value.Length + 1 < input.Length)
                {
                    message = input.Substring(timeIndex + timeMatch.Value.Length + 1).Trim();
                }
                else
                {
                    message = "Напоминание";
                }

                return true;
            }
            catch
            {
                return false;
            }
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
        /// <param name="eventId">ID события</param>
        /// <returns>Список кнопок</returns>
        public static InlineKeyboardMarkup DeleteEvent(int eventId)
        {
            // Создаём список кнопок
            List<InlineKeyboardButton> inlineKeyboards = new List<InlineKeyboardButton>();
            // Добавляем кнопку удалить, прикрепляя к ней ID события
            inlineKeyboards.Add(new InlineKeyboardButton("Удалить") { CallbackData = $"delete_{eventId}" });
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
            // Проверяем, что индекс в пределах списка
            if (typeMessage < 0 || typeMessage >= Messages.Count)
            {
                Console.WriteLine($"ОШИБКА: Попытка отправить сообщение с неверным индексом: {typeMessage}");
                await TelegramBotClient.SendMessage(chatId, "Произошла ошибка. Попробуйте еще раз.");
                return;
            }

            // Если тип сообщения не равен 3
            if (typeMessage != 3)
            {
                // Отправляем сообщение
                await TelegramBotClient.SendMessage(chatId, Messages[typeMessage], ParseMode.Html, replyMarkup: GetButtons());
            }
            else if (typeMessage == 3) // если тип сообщения 3
            {
                // отправляем сообщение с ошибкой даты с подстановкой текущего времени
                string message = string.Format(Messages[typeMessage], DateTime.Now.ToString("HH.mm dd.MM.yyyy"));
                await TelegramBotClient.SendMessage(chatId, message, ParseMode.Html, replyMarkup: GetButtons());
            }
        }

        /// <summary>
        /// Команды
        /// </summary>
        /// <param name="chatId">Код пользователя</param>
        /// <param name="command">Команда</param>
        public async void Command(long chatId, string command)
        {
            Console.WriteLine($"Выполнение команды: {command} для пользователя {chatId}");

            // если команда старт, отправляем 0 сообщение
            if (command.ToLower() == "/start")
                SendMessage(chatId, 0);
            // если команда создание задачи, отправляем 1 сообщение
            else if (command.ToLower() == "/create_task")
                SendMessage(chatId, 1);
            // если команда создание повторяющейся задачи, отправляем 8 сообщение
            else if (command.ToLower() == "/repeat_task")
                SendMessage(chatId, 8);
            // если команда список задач
            else if (command.ToLower() == "/list_tasks")
            {
                // получаем пользователя, у которого совпадает чат Id
                Users User = Users.Find(x => x.IdUser == chatId);
                // если пользователь не найден, отправляем 4 сообщение
                if (User == null)
                    SendMessage(chatId, 4);
                // если количество уведомлений равно 0, отправляем 4 сообщение
                else if (User.Events.Count == 0 && User.RecurrencePattern.Count == 0)
                    SendMessage(chatId, 4);
                // в противном случае
                else
                {
                    // проверяем есть ли обычные задачи
                    if (User.Events.Count > 0)
                    {
                        await TelegramBotClient.SendMessage(chatId, "?? <b>Обычные задачи:</b>", ParseMode.Html);
                        // перебираем уведомления пользователя
                        foreach (Events Event in User.Events)
                        {
                            // отправляем в чат
                            await TelegramBotClient.SendMessage(
                                chatId,
                                $"? Время: {Event.Time.ToString("HH:mm dd.MM.yyyy")}" +
                                $"\n?? Сообщение: {Event.Message}",
                                replyMarkup: DeleteEvent(Event.Id)
                            );
                        }
                    }

                    // проверяем есть ли повторяющиеся задачи
                    if (User.RecurrencePattern.Count > 0)
                    {
                        await TelegramBotClient.SendMessage(chatId, "?? <b>Повторяющиеся задачи:</b>", ParseMode.Html);
                        // перебираем повторяющиеся задачи пользователя
                        foreach (RecurrencePattern Pattern in User.RecurrencePattern)
                        {
                            // отправляем в чат
                            await TelegramBotClient.SendMessage(
                                chatId,
                                $"?? Дни: {string.Join(", ", Pattern.Days.Select(d => d.ToString()))}" +
                                $"\n? Время: {Pattern.Time.ToString("hh\\:mm")}" +
                                $"\n?? Сообщение: {Pattern.Message}"
                            );
                        }
                    }
                }
            }
            else
            {
                // Если команда не распознана
                await TelegramBotClient.SendMessage(chatId, "Неизвестная команда. Используйте /start для начала работы.");
            }
        }

        // Сохранение команды в базу данных
        private async Task SaveCommandToDatabase(string username, string commandText)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                // Сохраняем команду
                var command = new Command
                {
                    User = !string.IsNullOrEmpty(username) ? username : "Unknown",
                    Commands = !string.IsNullOrEmpty(commandText) ? commandText : "Empty"
                };
                db.CommandUser.Add(command);
                await db.SaveChangesAsync();
                Console.WriteLine($"Команда сохранена в БД: {commandText}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения команды в БД: " + ex.Message);
            }
        }

        // Сохранение пользователя в базу данных
        private async Task SaveUserToDatabase(long userId, string username)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                // Проверяем, есть ли уже пользователь (используем вспомогательный метод)
                var existingUser = await FirstOrDefaultAsync(db.Users, u => u.IdUser == userId);
                if (existingUser == null)
                {
                    var user = new Users(userId)
                    {
                        Username = !string.IsNullOrEmpty(username) ? username : "User_" + userId
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"Пользователь {userId} сохранен в БД");

                    // Добавляем пользователя в локальный список
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения пользователя в БД: " + ex.Message);
            }
        }

        // Сохранение события в базу данных
        private async Task SaveEventToDatabase(Events ev, long userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                // Находим пользователя в БД
                var dbUser = await FirstOrDefaultAsync(db.Users, u => u.IdUser == userId);
                if (dbUser == null)
                {
                    // Если пользователя нет, создаем его
                    dbUser = new Users(userId)
                    {
                        Username = "User_" + userId
                    };
                    db.Users.Add(dbUser);
                    await db.SaveChangesAsync();
                }

                // Устанавливаем связь с пользователем
                var entry = db.Entry(ev);
                entry.Property("UserId").CurrentValue = dbUser.Id; // <-- УСТАНАВЛИВАЕМ ВНЕШНИЙ КЛЮЧ

                // Сохраняем событие
                db.Events.Add(ev);
                await db.SaveChangesAsync();
                Console.WriteLine($"Событие сохранено в БД: {ev.Message} для пользователя {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения события в БД: " + ex.Message);
            }
        }

        // Сохранение повторяющейся задачи в базу данных
        private async Task SaveRecurringToDatabase(RecurrencePattern pattern, long userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                // Находим пользователя в БД
                var dbUser = await FirstOrDefaultAsync(db.Users, u => u.IdUser == userId);
                if (dbUser == null)
                {
                    // Если пользователя нет, создаем его
                    dbUser = new Users(userId)
                    {
                        Username = "User_" + userId
                    };
                    db.Users.Add(dbUser);
                    await db.SaveChangesAsync();
                }

                // Устанавливаем связь с пользователем
                var entry = db.Entry(pattern);
                entry.Property("UserId").CurrentValue = dbUser.Id; // <-- УСТАНАВЛИВАЕМ ВНЕШНИЙ КЛЮЧ

                // Сохраняем повторяющуюся задачу
                db.RecurrencePattern.Add(pattern);
                await db.SaveChangesAsync();
                Console.WriteLine($"Повторяющаяся задача сохранена в БД: {pattern.Message} для пользователя {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения повторяющейся задачи в БД: " + ex.Message);
            }
        }

        // Удаление события из базы данных по ID события
        private async Task DeleteEventFromDatabaseById(int eventId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                var dbEvent = await FirstOrDefaultAsync(db.Events, e => e.Id == eventId);
                if (dbEvent != null)
                {
                    db.Events.Remove(dbEvent);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"Событие удалено из БД по ID: {eventId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка удаления события из БД по ID: " + ex.Message);
            }
        }

        // Удаление всех событий пользователя из базы данных
        private async Task DeleteAllEventsFromDatabase(long userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                // Находим пользователя (используем вспомогательный метод)
                var user = await FirstOrDefaultAsync(db.Users, u => u.IdUser == userId);
                if (user != null)
                {
                    // Удаляем все события этого пользователя
                    var userEvents = await db.Events.Where(e => EF.Property<int>(e, "UserId") == user.Id).ToListAsync();
                    var userRecurring = await db.RecurrencePattern.Where(r => EF.Property<int>(r, "UserId") == user.Id).ToListAsync();

                    if (userEvents != null && userEvents.Any())
                        db.Events.RemoveRange(userEvents);

                    if (userRecurring != null && userRecurring.Any())
                        db.RecurrencePattern.RemoveRange(userRecurring);

                    // Удаляем пользователя из БД
                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"Пользователь {userId} и все его задачи удалены из БД");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка удаления всех событий из БД: " + ex.Message);
            }
        }

        // Загрузка данных из базы данных при запуске
        private async Task LoadDataFromDatabase()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<DbConfig>();

                // Загружаем всех пользователей с их событиями и повторяющимися задачами
                var usersFromDb = await db.Users
                    .Include(u => u.Events) // <-- ВАЖНО: загружаем связанные события
                    .Include(u => u.RecurrencePattern) // <-- ВАЖНО: загружаем связанные повторяющиеся задачи
                    .ToListAsync();

                Users.Clear();

                if (usersFromDb != null && usersFromDb.Any())
                {
                    foreach (var dbUser in usersFromDb)
                    {
                        try
                        {
                            // Исправляем возможные NULL значения
                            if (string.IsNullOrEmpty(dbUser.Username))
                                dbUser.Username = "User_" + dbUser.IdUser;

                            // Проверяем, что коллекции инициализированы
                            if (dbUser.Events == null)
                                dbUser.Events = new List<Events>();

                            if (dbUser.RecurrencePattern == null)
                                dbUser.RecurrencePattern = new List<RecurrencePattern>();

                            Users.Add(dbUser);
                            Console.WriteLine($"Загружен пользователь {dbUser.IdUser} с {dbUser.Events.Count} событиями и {dbUser.RecurrencePattern.Count} повторяющимися задачами");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка обработки пользователя {dbUser.IdUser}: {ex.Message}");
                            // Создаем нового пользователя с минимальными данными
                            var newUser = new Users(dbUser.IdUser);
                            Users.Add(newUser);
                        }
                    }

                    Console.WriteLine($"Успешно загружено {Users.Count} пользователей из БД");
                }
                else
                {
                    Console.WriteLine("В базе данных нет пользователей");
                    Users = new List<Users>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки данных из БД: " + ex.Message);
                // Создаем пустой список, чтобы бот работал
                Users = new List<Users>();
                Console.WriteLine("Создан пустой список пользователей для продолжения работы");
            }
        }

        //получение сообщений
        private async void GetMessages(Message message)
        {
            Console.WriteLine("Получено сообщение: " + message.Text + " от пользователя: " + message.Chat.Username);
            long chatId = message.Chat.Id;
            string text = message.Text;

            // Сохраняем команду в базу данных
            await SaveCommandToDatabase(message.Chat.Username ?? message.Chat.FirstName, text);

            if (text.Contains("/"))
            {
                Command(chatId, text);
            }
            else if (text.Equals("Удалить все задачи"))
            {
                Users User = Users.Find(x => x.IdUser == chatId);
                if (User == null)
                {
                    SendMessage(chatId, 4);
                }
                else if (User.Events.Count == 0 && User.RecurrencePattern.Count == 0)
                {
                    SendMessage(User.IdUser, 4);
                }
                else
                {
                    // Удаляем из памяти
                    User.Events.Clear();
                    User.RecurrencePattern.Clear();

                    // Удаляем из БД
                    await DeleteAllEventsFromDatabase(chatId);

                    SendMessage(User.IdUser, 6);
                }
            }
            else
            {
                Users User = Users.Find(x => x.IdUser == chatId);
                if (User == null)
                {
                    User = new Users(chatId);
                    Users.Add(User);

                    // Сохраняем нового пользователя в БД
                    await SaveUserToDatabase(chatId, message.Chat.Username ?? message.Chat.FirstName);
                }

                // ПРОВЕРЯЕМ ПОВТОРЯЮЩУЮСЯ ЗАДАЧУ (только если пользователь отправил не команду)
                if (CheckRepeatTask(text, out List<DayOfWeek> days, out TimeSpan repeatTime, out string repeatMessage))
                {
                    Console.WriteLine($"Добавляем повторяющуюся задачу: {repeatMessage}");

                    // Добавляем повторяющуюся задачу в память
                    var repeatPattern = new RecurrencePattern(days, repeatTime, repeatMessage);
                    User.RecurrencePattern.Add(repeatPattern);

                    // Сохраняем в БД
                    await SaveRecurringToDatabase(repeatPattern, chatId);

                    // Отправляем сообщение о добавлении
                    await TelegramBotClient.SendMessage(chatId,
                        $"? <b>Повторяющаяся задача добавлена!</b>\n" +
                        $"?? <b>Дни:</b> {string.Join(", ", days.Select(d => d.ToString()))}\n" +
                        $"? <b>Время:</b> {repeatTime.ToString("hh\\:mm")}\n" +
                        $"?? <b>Задача:</b> {repeatMessage}",
                        ParseMode.Html);
                    return;
                }

                // Обработка обычной задачи (если это не повторяющаяся задача)
                string[] Info = text.Split('\n');
                if (Info.Length < 2)
                {
                    SendMessage(chatId, 2);
                    return;
                }

                DateTime Time;
                if (CheckFormatDateTime(Info[0], out Time) == false)
                {
                    SendMessage(chatId, 2);
                    return;
                }

                if (Time < DateTime.Now)
                {
                    SendMessage(chatId, 3);
                    return;
                }

                var newEvent = new Events(Time, text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", ""));
                User.Events.Add(newEvent);

                // Сохраняем событие в БД
                await SaveEventToDatabase(newEvent, chatId);

                Console.WriteLine($"Добавлено событие для пользователя {chatId}: {newEvent.Message} в {newEvent.Time}");

                // Подтверждение пользователю
                await TelegramBotClient.SendMessage(chatId, "? Задача добавлена!");
            }
        }

        private async Task HandleUpdateAsync(
      ITelegramBotClient client,
      Update update,
      CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message)
                {
                    GetMessages(update.Message);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    CallbackQuery query = update.CallbackQuery;
                    Console.WriteLine($"CallbackQuery получен: {query.Data}");

                    // Обрабатываем callback данные для удаления события
                    if (query.Data.StartsWith("delete_"))
                    {
                        string[] parts = query.Data.Split('_');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int eventId))
                        {
                            Users User = Users.Find(x => x.IdUser == query.Message.Chat.Id);
                            if (User != null)
                            {
                                Events Event = User.Events.Find(x => x.Id == eventId);
                                if (Event != null)
                                {
                                    User.Events.Remove(Event);

                                    // Удаляем из БД по ID
                                    await DeleteEventFromDatabaseById(eventId);

                                    // Отправляем сообщение об удалении
                                    await TelegramBotClient.SendMessage(query.Message.Chat.Id, Messages[5]); // "Событие удалено"

                                    // Не используем AnswerCallbackQueryAsync, просто игнорируем
                                    Console.WriteLine("Callback обработан, задача удалена");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в HandleUpdateAsync: {ex.Message}");
            }
        }

        //получение ошибок
        private async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine("ОШИБКА: " + exception.Message);
        }

        /// <summary>
        /// Метод для периодической проверки и отправки напоминаний
        /// </summary>
        public async void Tick(object obj)
        {
            try
            {
                Console.WriteLine($"Проверка напоминаний в {DateTime.Now.ToString("HH:mm:ss")}");

                string TimeNow = DateTime.Now.ToString("HH:mm dd.MM.yyyy");
                var currentTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);
                var currentDay = DateTime.Now.DayOfWeek;

                // Перебираем всех пользователей
                foreach (Users User in Users)
                {
                    // Проверяем разовые задачи
                    for (int i = 0; i < User.Events.Count; i++)
                    {
                        if (User.Events[i].Time.ToString("HH:mm dd.MM.yyyy") == TimeNow)
                        {
                            await TelegramBotClient.SendMessage(
                                User.IdUser,
                                $"?? <b>Напоминание:</b> {User.Events[i].Message}",
                                ParseMode.Html
                            );

                            User.Events.RemoveAt(i);
                            i--;
                        }
                    }

                    // Проверяем повторяющиеся задачи
                    foreach (var pattern in User.RecurrencePattern)
                    {
                        if (pattern.Days.Contains(currentDay) &&
                            pattern.Time.Hours == currentTime.Hours &&
                            pattern.Time.Minutes == currentTime.Minutes)
                        {
                            await TelegramBotClient.SendMessage(
                                User.IdUser,
                                $"?? <b>Повторяющееся напоминание:</b> {pattern.Message}",
                                ParseMode.Html
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка в Tick методе: " + ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Бот запускается...");

            try
            {
                // Загружаем данные из БД вместо очистки
                await LoadDataFromDatabase();
                Console.WriteLine("Данные загружены из БД");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при запуске: {ex.Message}");
                // Создаем пустой список пользователей для продолжения работы
                Users = new List<Users>();
                Console.WriteLine("Создан пустой список пользователей для продолжения работы");
            }

            // Инициализируем телеграмм клиент
            TelegramBotClient = new TelegramBotClient(Token);

            // Запускаем прослушивание сообщений
            TelegramBotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                null,
                new CancellationTokenSource().Token
            );

            Console.WriteLine("Бот запущен и слушает сообщения");

            // Запускаем таймер для проверки напоминаний
            TimerCallback TimerCallback = new TimerCallback(Tick);
            Timer = new Timer(TimerCallback, 0, 0, 60 * 1000);

            Console.WriteLine("Таймер запущен (интервал 1 минута)");
        }
    }
}