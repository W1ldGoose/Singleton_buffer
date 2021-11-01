using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Semaphore_proj
{
    class Program
    {
        static int writersCount = 7;
        static int readersCount = 10;

        static int messagesCount = 500;

        private static int messageLength = 50;

        // массив сообщений писателей
        static string[,] messages = new string[writersCount, messagesCount];

        static string buffer;
        static bool isBufferEmpty = true;
        static bool isBufferFinish = false;
        private static List<string>[] readedMessages = new List<string>[readersCount];
        private static Thread[] writers = new Thread[writersCount];
        private static Thread[] readers = new Thread[readersCount];

        private static Semaphore emptySem = new Semaphore(1,1);
        private static Semaphore fullSem = new Semaphore(0,1);

        // заполнение массива сообщений
        static void FillMessages()
        {
            string tmp = "";
            for (int i = 0; i < writersCount; i++)
            {
                for (int j = 0; j < messagesCount; j++)
                {
                    // строка = номер писателя + номер сообщения 
                    tmp = i.ToString() + "W" + j.ToString() + "M";
                    for (int k = 4; k < messageLength; k++)
                    {
                        tmp += k;
                    }

                    messages[i, j] = tmp;
                }
            }
        }

        static void WriteBuffer(object writerIndex)
        {
            int index = (int) writerIndex;
            int i = 0;
            while (i < messagesCount)
            {
                // дожидаемся, когда буффер пустой
                emptySem.WaitOne();
                buffer = messages[index, i++];
                // увеличиваем значение внутреннего счетчика семафора
                fullSem.Release();
            }
        }

        static void ReadBuffer(object readerIndex)
        {
            int index = (int) readerIndex;
            readedMessages[index] = new List<string>();

            while (!isBufferFinish)
            {
                // дожидаемся, когда буффер заполнинтся
                fullSem.WaitOne();
                if (isBufferFinish)
                {
                    fullSem.Release();
                    break;
                }
                readedMessages[index].Add(buffer);
                emptySem.Release();

            }
        }

        static void Main(string[] args)
        {
            FillMessages();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // запускаем писателей
            for (int i = 0; i < writersCount; i++)
            {
                writers[i] = new Thread(WriteBuffer);
                writers[i].Start(i);
            }

            // запускаем читателей
            for (int i = 0; i < readersCount; i++)
            {
                readers[i] = new Thread(ReadBuffer);
                readers[i].Start(i);
            }

            for (int i = 0; i < writersCount; i++)
            {
                writers[i].Join();
            }

            isBufferFinish = true;
            fullSem.Release();

            for (int i = 0; i < readersCount; i++)
            {
                readers[i].Join();
            }

            stopwatch.Stop();
            TimeSpan timeSpan = stopwatch.Elapsed;

            string[] receivedMessages = readedMessages.SelectMany(x => x)
                .ToArray();
            string[] lostMessages = messages.Cast<string>()
                .Except(readedMessages.SelectMany(x => x)).ToArray();

            var dublicates = receivedMessages.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var v in lostMessages)
            {
                Console.WriteLine(v);
            }

            Console.WriteLine("------------");
            Console.WriteLine("Всего сообщений: " + messages.Length);
            Console.WriteLine("Получено сообщений " + receivedMessages.Length);
            Console.WriteLine("Потеряно сообщений: " + lostMessages.Length);
            Console.WriteLine("Дубликаты сообщений: " + dublicates.Count);
            Console.WriteLine("Время: " + timeSpan.TotalMilliseconds);
        }
    }
}