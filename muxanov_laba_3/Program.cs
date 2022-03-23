using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace muxanov_laba_3
{
    class Producer
    {
        private ChannelWriter<int> Writer;
        public Producer(ChannelWriter<int> _writer, CancellationToken tok)
        {
            Writer = _writer;
            Task.WaitAll(Run(tok));
        }

        private async Task Run(CancellationToken tok)
        {

            var r = new Random();
            //ожидает, когда освободиться место для записи элемента.
            while (await Writer.WaitToWriteAsync())
            {
                if (tok.IsCancellationRequested)
                {
                        Console.WriteLine("Producer Stopped");
                    return;
                }

                Thread.Sleep(50);


                if (Program.flag && Program.count <= 100)
                {
                    var item = r.Next(1, 101);
                    await Writer.WriteAsync(item);
                    Program.count += 1;
                    Console.WriteLine($"Записанные данные: {item}");
                }
            }
        }
    }


    class Consumer
    {
        private ChannelReader<int> Reader;

        public Consumer(ChannelReader<int> _reader, CancellationToken tok)
        {
            Reader = _reader;
            Task.WaitAll(Run(tok));
        }

        private async Task Run(CancellationToken tok)
        {
            // ожидает, когда освободиться место для чтения элемента.
            while (await Reader.WaitToReadAsync())
            {
                Thread.Sleep(50);

                if (Reader.Count != 0)
                {
                    var item = await Reader.ReadAsync();
                    Program.count -= 1;
                    Console.WriteLine($"Полученные данные: {item}");
                }
                if (Reader.Count >= 100 )
                {
                    Program.flag = false;
                }
                else if (Reader.Count <= 80)
                {
                    Program.flag = true;
                }
                if (tok.IsCancellationRequested)
                {
                    if (Reader.Count == 0)
                    {
                        Console.WriteLine("Consumer Stopped");
                        return;
                    }
                }
            }
        }
    }

    class Program
    {
        static public bool flag = true;
        static public int count = 0;

        static void Main(string[] args)
        {
            //создаю общий канал данных
            Channel<int> channel = Channel.CreateBounded<int>(200);
            //создаются производители и потребители
            Task[] streams = new Task[5];


            
            var cts = new CancellationTokenSource();
            

            for (int i = 0; i < 5; i++)
            {
                if (i < 3)
                {
                    streams[i] = Task.Run(() => { new Producer(channel.Writer, cts.Token); }, cts.Token);
                }
                else
                {
                    streams[i] = Task.Run(() => { new Consumer(channel.Reader, cts.Token); }, cts.Token);
                }
            }


            new Thread(() => {
                for (; ; )
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Q)
                    {
                        cts.Cancel();
                    }
                }
            })
            { IsBackground = true }.Start();


            //Ожидает завершения выполнения всех указанных объектов Task 
            Task.WaitAll(streams);
        }
    }
}