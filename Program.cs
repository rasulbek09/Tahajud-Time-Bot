using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace TahajudTimeBot
{
    class Program
    {
        static TelegramBotClient Bot;
        const int minpday = 1440;
        const int minphour = 60;

        static Dictionary<long, string> LastWord = new Dictionary<long, string>();

        const string COMMAND_LIST =
            @"Бот предназначен для расчёта начала последней трети ночи.
Введите время магриба и фаджра через точку и пробел, например 20.08 2.42
";
        static Update temp = new Update();
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                String text;
                var message = update.Message;
                temp = update;
                if (message.Text.ToLower() == "/start")
                {
                    ////await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать на борт, добрый путник!");
                    await botClient.SendTextMessageAsync(message.Chat, COMMAND_LIST);
                    return;
                }
                else if (message.Text.Contains(' '))
                {
                    text = Calculations(message.Text);
                    await botClient.SendTextMessageAsync(message.From.Id, text);
                }
                //await botClient.SendTextMessageAsync(message.Chat, "Привет-привет!!");

                
            }
        }


        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            var message = temp.Message;
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            await botClient.SendTextMessageAsync(message.From.Id, "Неверный формат данных");
            
        }



        static void Main(string[] args)
        {
            Bot = new TelegramBotClient("5461351679:AAF9SqAd42e1EK3tYwDED4gsnJkoMXz9xMk");

            //var me = Bot.GetMeAsync().Result;
            //Console.WriteLine(me.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };

            Bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();

        }


        private static string Calculations(string message)
        {
            var timesArr = message.Split(' ');
            // получаем магриба и фаджра и парсим ввод пользователя в массивы "часы минуты"
            int[] HourMinsFromUserInpMagr = GetHourMinsFromUserInp(timesArr[0]);
            int[] HourMinsFromUserInpFajr = GetHourMinsFromUserInp(timesArr[1]);
            //int[] error = new int[HourMinsFromUserInpMagr.Length];

            if (HourMinsFromUserInpMagr == null || HourMinsFromUserInpFajr == null)
            {
                return "Неверный формат!";
            }
            // считаем кол-во минут от магриба до полуночи
            int mintomidn = GetToMinToMidn(minpday, HourMinsFromUserInpMagr);
            // считаем кол-во минут от полуночи до утреннего
            int mintofajr = GetToMinTofajr(HourMinsFromUserInpFajr);
            // суммируем минуты и делим на 3, получаем размер трети в минутах
            int thirdpart = GetToThirdPart(mintomidn, mintofajr);
            // полученную треть отнимаем от кол-ва минут до утреннего и получаем начало посл. трети
            // thirdBegin - кол-во минут от полуночи до начала трети
            int thirdBegin = GetToThirdBegin(mintofajr, thirdpart);
            // проверяем thirdBegin на отрицательность
            if (CheckNegativeThirdBegin(thirdBegin))
                thirdBegin = minpday + thirdBegin;
            // переводим количество минут трети в часы минуты
            int h = thirdpart / minphour;
            int m = thirdpart % minphour;
            // переводим количество минут начала трети в часы минуты
            int hSt = thirdBegin / minphour;
            int mSt = thirdBegin % minphour;

            string hmSt;
            if (mSt < 10)
            {
                hmSt = $"Время начала трети ночи {hSt} : 0{mSt}";
            }
            else
            {
                //string hmSt = $"Время начала трети ночи {hSt} : {mSt}";
                hmSt = "Время начала трети ночи " + hSt.ToString() + ':' + mSt.ToString();
            }
            return hmSt;
        }

        private static bool CheckNegativeThirdBegin(int thirdBegin)
        {
            if (thirdBegin < 0)
                return true;
            else
                return false;
        }

        private static int GetToThirdBegin(int mintofajr, int thirdpart)
        {
            return mintofajr - thirdpart;
        }

        //private static int[] GetHourMinsFromUserInpFajr(string usinput)
        //{

        //    string[] fajrArr = usinput.Split('.');
        //    int[] fajrHM = new int[fajrArr.Length];
        //    for (int i = 0; i < fajrArr.Length; i++)
        //        fajrHM[i] = Convert.ToInt32(fajrArr[i]);

        //    return fajrHM;
        //}

        private static int[] GetHourMinsFromUserInp(string usinput)
        {
            //string[] magrArr = usinput.Split('.');
            //int[] magrHM = new int[magrArr.Length];
            //for (int i = 0; i < magrArr.Length; i++)
            //    magrHM[i] = Convert.ToInt32(magrArr[i]);

            //return magrHM;
            try
            {
                string[] magrArr = usinput.Split('.');
                int[] magrHM = new int[magrArr.Length];
                for (int i = 0; i < magrArr.Length; i++)
                    magrHM[i] = Convert.ToInt32(magrArr[i]);
                
                return magrHM;
            }
            catch
            {
                return null;
            }
        }

        private static int GetToMinToMidn(int minpday, int[] magrHM)
        {
            return minpday - (magrHM[0] * minphour + magrHM[1]);
        }

        private static int GetToThirdPart(int mintomidn, int mintofajr)
        {
            return (mintomidn + mintofajr) / 3;
        }

        private static int GetToMinTofajr(int[] fajrHM)
        {
            return fajrHM[0] * minphour + fajrHM[1];
        }
    }
    
}
