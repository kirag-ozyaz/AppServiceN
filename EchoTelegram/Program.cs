using MihaZupan;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Logger;
using System.Data;
using System.Data.OleDb;
//using System.Timers;
using System.Threading;

namespace EchoTelegram
{
    class Program
    {
        private static ITelegramBotClient botClient;
        private static readonly string filesettings = @"setting.join";
        private static AppSettings settingApp;
        private static DateTime DataStart;
        private static int dateoffset = 4;
        private static DataSet.DataSetSMSGate.SMS_MESSAGESDataTable tableMessages = new DataSet.DataSetSMSGate.SMS_MESSAGESDataTable();
        /// <summary>
        /// строка с которой будем выбирать данные
        /// </summary>
        static int RowIdLast = -1;
        /// <summary>
        /// текущее последнее Id записи, которое было последнее обработано (необходим когда все отправлено)
        /// </summary>
        ////static int RowsIdCurrentLast = -1;
        static void Main(string[] args)
        {
            //settingApp = LoadSettings();
            //ProcessingEchoTelegram($"Тест");
            //return;
#if !DEBUG
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "--initsetting":
                        SaveSettings();
                        Console.WriteLine("Настройки приведены к первоначальным значениям");
                        Log.Write(LogLevel.Info, "ULgesEchoTelegram", "Main", "Настройки приведены к первоначальным значениям", null);
                        return;
                    case "--help":
                        Console.WriteLine($"Входные параметры командной строки: \r\n " +
                            $"--initsetting - настройки приводятся к первональным значениям  \r\n" +
                            $" --help - помощь \r\n" +
                            $" --start - стартуем службу \r\n" +
                            $"");
                        //Console.ReadLine();
                        return;
                    case "--start":
                        Console.WriteLine($"Служба запущена {System.Diagnostics.Process.GetCurrentProcess().ProcessName}");
                        Log.Write(LogLevel.Info, "ULgesEchoTelegram", "Main", $"Служба запущена {System.Diagnostics.Process.GetCurrentProcess().ProcessName}", null);

                        break;
                    default:
                        Console.WriteLine("Неправильные входные параметры. Введите параметр --help");
                        Log.Write(LogLevel.Error, "ULgesEchoTelegram", "Main", "Неправильные входные параметры", null);
                        return;
                }
            }
            else
            {
                Console.WriteLine("Для запуска службы необходим соответсвующий параметр. Введите параметр --help");
                return;
            }
#endif
            #region Загрузим настройки

            try
            {
                settingApp = LoadSettings();
                Log.Write(LogLevel.Info, "ULgesEchoTelegram", "LoadSettings", "Загрузка настроек удачно", null);
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "LoadSettings", "Загрузка настроек ошибка", ex.Message, null);
                return;
            }
#endregion

            //DataStart = new DateTime(2020, 11, 04, 0, 0, 0);
            DataStart = DateTime.Now;
            // 1. Получим последнее ID в базе данных по дате (потом использовать надо)
            if (LoadTableSMSGate(DataSelection.moreDateTime))
            {
                if (tableMessages.Rows.Count > 0)
                    foreach (DataRow row in tableMessages.OrderBy(od => od.ID))
                    {
                        RowIdLast = Convert.ToInt32(row["ID"]);
                        break;
                    }
                else
                {
                    // 2. если на текущее время или после последней выборки нет данных,
                    // то надо получить последнее ID в базе (его как бы в будущем использовать не надо)
                    // для будущих выборок
                    if (LoadTableSMSGate(DataSelection.idLastRowOfTables))
                    {
                        if (tableMessages.Rows.Count > 0)
                            foreach (DataRow row in tableMessages.OrderByDescending(od => od.ID))
                            {
                                // добавим единицу к текущему Ид, и с него будем выбирать данные
                                RowIdLast = Convert.ToInt32(row["ID"]) + 1;
                                break;
                            }

                        else
                        {
                            Log.Write(LogLevel.Error, "ULgesEchoTelegram", "Main", "LoadSettings", "При первом запуске данных нет", null);
                            ////return;
                        }
                    }
                }
            }
            else {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "Main", "LoadSettings", "Запрос не удачный", null);
                return;
            }
            if (RowIdLast == -1)
            {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "Main", "LoadSettings", "Id строки последней строки не найден", null);
                //return;
            }
            int periodTimerCallback = settingApp.timerCallback.period;

            TimerCallback timeSG = new TimerCallback(ProcessingData);
            Timer time = new Timer(timeSG, null, 0, periodTimerCallback);
            
            Console.WriteLine("(!) Нажми чтоб выйти");
            Console.ReadLine();

            ////////////
            //// ProcessingEchoTelegram();
        }
        /// <summary>
        /// статус, что запрос к таблице LoadTableSMSGate(DataSelection.moreIdRows, id) отработан
        /// </summary>
        static bool isQueryWorkedGlobal = true;
        /// <summary>
        /// Обработка данных
        /// </summary>
        /// <param name="obj"></param>
        public static void ProcessingData(object obj)
        {
            int id = RowIdLast;
            Console.WriteLine($"(!) Начали считывать данные с {id}");
            // если ид=-1, то ищем данные по записей
            // 3. выбираем данные от последней строки
            if (isQueryWorkedGlobal)
            {
                isQueryWorkedGlobal = false;
                if (LoadTableSMSGate(DataSelection.moreIdRows, id))
                {
                    if (tableMessages.Rows.Count > 0)
                    {
                        string SMS_PHONE = "";
                        int RowIdCurrent = -1;
                        string TextMessage = "";
                        DateTime DateTimeMessage = DateTime.Now;
                        foreach (DataRow row in tableMessages.OrderBy(od => od.ID))
                        {
                            SMS_PHONE = Convert.ToString(row["SMS_PHONE"]);
                            RowIdCurrent = Convert.ToInt32(row["ID"]);
                            TextMessage = Convert.ToString(row["SMS_TEXT"]);
                            DateTimeMessage = Convert.ToDateTime(row["SMS_TIME"]).AddHours(dateoffset);
                            Console.WriteLine($"(!) обработали  RowIdCurrent {RowIdCurrent} для сообщения от {DateTimeMessage}: {TextMessage}");
                            ProcessingEchoTelegram($"{SMS_PHONE}: {DateTimeMessage}: {TextMessage}");
                        }
                        // добавим единицу к текущему Ид, и с него будем выбирать данные
                        RowIdLast = RowIdCurrent + 1;
                        Console.WriteLine($"(!) увеличили RowIdLast: {RowIdLast}");
                    }
                    isQueryWorkedGlobal = true;
                }
                else
                {
                    Log.Write(LogLevel.Error, "ULgesEchoTelegram", "ProcessingData", "LoadSettings", "Ошибка получения данных в таймере", null);
                    //return;
                }
            }
        }

        /// <summary>
        /// Сохранение настроек
        /// </summary>
        private static void SaveSettings()
        {
            settingApp = new AppSettings
            {
                telegramsettings = new AppSettings.TelegrammSetting { },
                sqlsettings = new AppSettings.SQLSettings { },
                settingsSMSGate = new AppSettings.SettingsSMSGate { },
                timerCallback = new AppSettings.TimerCallback { }
            };

            //var jsonFile = JsonConvert.SerializeObject(settings, Formatting.Indented);
            //System.IO.File.WriteAllText(filesettings, jsonFile);

            using (StreamWriter file = System.IO.File.CreateText(filesettings))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, settingApp);
            }
        }
        /// <summary>
        /// Загрузка настроек
        /// </summary>
        /// <returns></returns>
        private static AppSettings LoadSettings()
        {
            AppSettings settings;
            using (StreamReader file = System.IO.File.OpenText(filesettings))
            {
                JsonSerializer serializer = new JsonSerializer();
                settings = (AppSettings)serializer.Deserialize(file, typeof(AppSettings));
            }
            return settings;

        }

        /// <summary>
        /// Процесс работы с ботом
        /// </summary>
        private static void ProcessingEchoTelegram(string Message = "")
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            // https://api.telegram.org/bot1452444783:AAE9eb9yYnz0Mdg4E26OfQWNeSHBLOEi4CQ/getMe
            // https://api.telegram.org/bot1452444783:AAE9eb9yYnz0Mdg4E26OfQWNeSHBLOEi4CQ/getUpdates
            // t.me/UlgesEchoBot
            // Use this token to access the HTTP API:
            // string token = "1452444783:AAE9eb9yYnz0Mdg4E26OfQWNeSHBLOEi4CQ"
            string token = settingApp.telegramsettings.token;
            Log.Write(LogLevel.Info, "ULgesEchoTelegram", "ProcessingEchoTelegram", "Hello World!", null);
            //Console.WriteLine("Hello World!");
            //var proxy = new HttpToSocks5Proxy("207.97.174.134", 1080);
            //var proxy = new WebProxy("34.67.171.155", 8080);
            //botClient = new TelegramBotClient("1452444783:AAE9eb9yYnz0Mdg4E26OfQWNeSHBLOEi4CQ", proxy);
            try
            {
                botClient = new TelegramBotClient(token) { Timeout = TimeSpan.FromSeconds(10) };
                var me = botClient.GetMeAsync().Result;
                //Console.WriteLine($"Bot id: {me.Id}. Bot Name {me.FirstName}");
                Log.Write(LogLevel.Info, "ULgesEchoTelegram", "ProcessingEchoTelegram", $"Connect to Bot id: {me.Id}. Bot Name {me.FirstName}", null);
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "ProcessingEchoTelegram", $"Error connect to Bot id: {settingApp.telegramsettings.botAdmin}. Bot Name {settingApp.telegramsettings.botName}", ex.Message, null);
            }
            //
            long userId = settingApp.telegramsettings.userId;
            try
            {
                botClient.SendTextMessageAsync(userId, $"Hello world {Message}");// 465686572
                Log.Write(LogLevel.Info, "ULgesEchoTelegram", "ProcessingEchoTelegram", $"Connect UserId {userId} and send message", null);
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "ProcessingEchoTelegram", $"Connect UserId {userId}", ex.Message, null);
            }


            // События контроля событий
            //botClient.OnMessage += Bot_OnMessage1;
            //botClient.OnMessage += (sender, messageEventArgs) =>
            //{
            //    long chatId = messageEventArgs.Message.Chat.Id;
            //    botClient.SendTextMessageAsync(chatId, $"Hello world {chatId}");

            //};
            //botClient.StartReceiving();

            //Console.ReadKey();

        }

        private static async void Bot_OnMessage1(object sender, MessageEventArgs e)
        {
            //   throw new NotImplementedException();
            var text = e?.Message?.Text;
            if (text == null)
                return;
            Console.WriteLine($"recived text message '{text}' in chat '{e.Message.Chat.Id}' ");

            await botClient.SendTextMessageAsync(
                chatId: e.Message.Chat,
                text: $"You said '{text}' id {e.Message.Chat.Id} name {e.Message.Chat.FirstName}"
                ).ConfigureAwait(false);
        }
        /// <summary>
        /// получение данных с базы данных
        /// </summary>
        /// <returns></returns>
        private static bool LoadTableSMSGate(DataSelection dataSelection = DataSelection.moreDateTime, int IdRow = -1)
        {
            bool resultat = false;
            tableMessages.Clear();
            if (dataSelection == DataSelection.moreIdRows && IdRow == -1)
            {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "LoadTableSMSGate", "Ошибка ", $"IdRow = -1", null);
                return resultat;
            }
            string connectString = settingApp.settingsSMSGate.connectString;
            string numberTelefones = settingApp.settingsSMSGate.numberSelectTelefones;
            OleDbConnection ConnectionSMSGate = new OleDbConnection(connectString);
            // открываем соединение с БД
            try
            {
                ConnectionSMSGate.Open();
            }
            catch (OleDbException ex)
            {
                Log.Write(LogLevel.Error, "ULgesEchoTelegram", "LoadTableSMSGate", "Ошибка открытия базы данных:", ex.Message, null);
                return resultat;
            }
            // 
            System.Collections.Generic.List<char> arrDirect = new System.Collections.Generic.List<char>();
            string SMS_DIRECT = "";
            var strDirect = settingApp.settingsSMSGate.DIRECT;
            if (strDirect.InputMessage) { arrDirect.Add('0'); }
            if (strDirect.OutgoingMessage) { arrDirect.Add('1'); }
            SMS_DIRECT = string.Join(",", arrDirect);

            string query = $"SELECT ";
            if (dataSelection == DataSelection.idLastRowOfTables)
            {
                query += " TOP 1 ";
            }
            query += $" * FROM {tableMessages.TableName}   ";
            string where = "";
            if (SMS_DIRECT != "") { where += $"SMS_DIRECT in ({SMS_DIRECT})"; }
            where += numberTelefones != "" ? $" and SMS_PHONE in ({numberTelefones})" : "";
            if (dataSelection == DataSelection.moreDateTime)
            {
                string DataStartStr = DataStart.ToString("#MM/dd/yyyy HH:mm#").Replace('.', '/');
                //string DataStartStr = $"#11/04/2020 00:00#";
                where += $" and DATEADD('h', {dateoffset}, SMS_TIME) >={DataStartStr}";
            }
            if (dataSelection == DataSelection.moreIdRows)
            {
                where += $" and ID >= {IdRow}";
            }
            if (where != "") query = String.Concat( query, " WHERE ", where);
            if (dataSelection == DataSelection.idLastRowOfTables)
                query += " ORDER BY ID DESC ";

            OleDbCommand command = new OleDbCommand(query, ConnectionSMSGate);

            using (OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter(query, ConnectionSMSGate))
            {
                oleDbDataAdapter.SelectCommand.CommandTimeout = 0;
                try
                {
                    oleDbDataAdapter.Fill(tableMessages);
                }
                catch (InvalidOperationException ex)
                {
                    Log.Write(LogLevel.Error, "ULgesEchoTelegram", "LoadTableSMSGate", "Ошибка получения данных (заполения таблицы):", ex.Message, null);
                    return resultat;
                }
                resultat = true;
            }
            ConnectionSMSGate.Close();
            return resultat;
        }
        /// <summary>
        /// способы выборки данных
        /// </summary>
        enum DataSelection
        {
            /// <summary>
            /// Выборка значений больше определенной даты и времени
            /// </summary>
            moreDateTime = 0,
            /// <summary>
            /// Выборка значений больше или равно определенной ид в таблице
            /// </summary>
            moreIdRows = 1,
            /// <summary>
            /// Получение последнего (максимального) ид таблицы согласно текущему отбору
            /// </summary>
            idLastRowOfTables = 2

        }
    }
}
