using System.Net;
using System.Reflection;

namespace PcapMarketReplayConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var addresses = new PcapReplayConfig[] {
                new (Environment.GetEnvironmentVariable("Eqt__instrumentDefinition")!, IPEndPoint.Parse("224.100.33.1:10101")),
                new (Environment.GetEnvironmentVariable("Eqt__snapshot")!, IPEndPoint.Parse("224.100.33.2:10102")),
                new (Environment.GetEnvironmentVariable("Eqt__incrementalsA")!, IPEndPoint.Parse("224.100.33.3:10103")),
                new (Environment.GetEnvironmentVariable("Eqt__incrementalsB")!, IPEndPoint.Parse("224.100.33.4:10104")),
                new (Environment.GetEnvironmentVariable("Drv__instrumentDefinition")!, IPEndPoint.Parse("224.200.33.1:10201")),
                new (Environment.GetEnvironmentVariable("Drv__snapshot")!, IPEndPoint.Parse("224.200.33.2:10202")),
                new (Environment.GetEnvironmentVariable("Drv__incrementalsA")!, IPEndPoint.Parse("224.200.33.3:10203")),
                new (Environment.GetEnvironmentVariable("Drv__incrementalsB")!, IPEndPoint.Parse("224.200.33.4:10204"))
            };

            var replayers = addresses.Select(a => new PcapReplayer(a)).ToArray();
            foreach (var replayer in replayers)
            {
                Console.WriteLine("Starting {0}", replayer.Path);
                replayer.Start();
            }

            Task.Run(() =>
            {
                Console.Clear();
                while (replayers.All(r => r.Connected))
                {
                    Console.SetCursorPosition(0, 0);
                    foreach (var item in replayers)
                    {
                        Console.WriteLine("{0}\t{1}", item.Path, item.MessagesConsumed);
                    }
                    Thread.Sleep(300);
                }
            });
            Console.ReadLine();
        }
    }
}
