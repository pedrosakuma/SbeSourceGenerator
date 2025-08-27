using Binance.Stream;
using Microsoft.Extensions.Configuration;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SbeBinanceConsole
{
    internal class Program
    {
        static long bookUpdateIdSbe;
        static long bookUpdateIdJson;
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<Program>() 
                .Build();
            var key = builder["BinanceApiKey"];
            CancellationTokenSource cts = new CancellationTokenSource();
            await Task.WhenAll(
                WatchSbeBestBid(key, cts),
                WatchJsonBestBid(cts),
                WatchWinner(cts)
            );
        }

        private static async Task WatchWinner(CancellationTokenSource cts)
        {
            var (x, _) = Console.GetCursorPosition();
            while (!cts.Token.IsCancellationRequested)
            {
                Console.SetCursorPosition(x, 0);
                if (bookUpdateIdSbe > bookUpdateIdJson)
                {
                    Console.Write("SBE  win");
                }
                else 
                { 
                    Console.Write("Json win");
                }
                await Task.Delay(100);
            }
        }

        private static async Task WatchSbeBestBid(string key, CancellationTokenSource cts)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", key);
            await ws.ConnectAsync(new Uri("wss://stream-sbe.binance.com/ws/btcusdt@bestBidAsk"), cts.Token);

            //Console.WriteLine("Connected to Binance WebSocket");
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192 + sizeof(ushort)]);
            var parser = new MessageParser(
                (ref readonly PacketHeader header, ReadOnlySpan<byte> data) => true
            )
            {
                TradesStreamEventMessageReceived = (ref readonly TradesStreamEventData msg, ReadOnlySpan<byte> extra) =>
                {
                    //decimal qtyMultiplier = (decimal)Math.Pow(10, msg.QtyExponent.Value);
                    //decimal priceMultiplier = (decimal)Math.Pow(10, msg.PriceExponent.Value);
                    //Console.WriteLine($"TradesStreamEventMessageReceived: ");
                    //msg.ConsumeVariableLengthSegments(extra,
                    //    trade =>
                    //    {
                    //        Console.WriteLine($"{trade.Id.Value} - q: {trade.Qty.Value * qtyMultiplier}, p: {trade.Price.Value * priceMultiplier}");
                    //    },
                    //    symbol =>
                    //    {
                    //        Console.WriteLine(Encoding.ASCII.GetString(symbol.VarData));
                    //    });
                },
                BestBidAskStreamEventMessageReceived = (ref readonly BestBidAskStreamEventData msg, ReadOnlySpan<byte> extra) =>
                {
                    bookUpdateIdSbe = msg.BookUpdateId.Value;
                    //Console.WriteLine($"BestBidAskStreamEventMessageReceived: ");

                    //decimal qtyMultiplier = (decimal)Math.Pow(10, msg.QtyExponent.Value);
                    //decimal priceMultiplier = (decimal)Math.Pow(10, msg.PriceExponent.Value);

                    //Console.WriteLine($"t: {msg.EventTime.Value}, id:{msg.BookUpdateId.Value}, bq: {msg.BidQty.Value * qtyMultiplier}, bp: {msg.BidPrice.Value * priceMultiplier}, ap: {msg.AskPrice.Value * priceMultiplier}, aq: {msg.AskQty.Value * qtyMultiplier}");
                    //msg.ConsumeVariableLengthSegments(extra,
                    //    symbol =>
                    //    {
                    //        Console.WriteLine(Encoding.ASCII.GetString(symbol.VarData));
                    //    });
                }
            };

            while (!cts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer.Slice(sizeof(ushort)), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("WebSocket closed");
                    break;
                }
                else
                {
                    // Prepend the length prefix
                    BitConverter.TryWriteBytes(buffer, (ushort)result.Count + sizeof(ushort));
                    parser.Parse(buffer.Slice(0, result.Count + sizeof(ushort)));
                }
            }
        }
        private static async Task WatchJsonBestBid(CancellationTokenSource cts)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", "ALqcBxuufVcSfB4Q6MuHTRrVqj9ezYeXRCTagm6tW9EA5XDUyxaNdBGdGMS8BBGA");
            await ws.ConnectAsync(new Uri("wss://stream.binance.com/ws/btcusdt@bookTicker"), cts.Token);

            //Console.WriteLine("Connected to Binance WebSocket");
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);
            while (!cts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("WebSocket closed");
                    break;
                }
                else
                {
                    ParseJsonBookUpdateId(buffer, result);
                }
            }
        }

        private static void ParseJsonBookUpdateId(ArraySegment<byte> buffer, WebSocketReceiveResult result)
        {
            var reader = new Utf8JsonReader(buffer.Slice(0, result.Count));
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "u")
                {
                    reader.Read();
                    bookUpdateIdJson = reader.GetInt64();
                }
            }
        }
    }
}
