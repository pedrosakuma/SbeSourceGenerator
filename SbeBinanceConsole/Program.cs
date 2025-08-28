using Binance.Stream;
using Microsoft.Extensions.Configuration;
using System.Net.WebSockets;
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
            Console.Clear();
            await Task.WhenAll(
                WatchSbeBestBid(key, cts),
                WatchJsonBestBid(key, cts),
                WatchWinner(cts)
            );
        }

        private static async Task WatchWinner(CancellationTokenSource cts)
        {
            while (!cts.Token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, 0);
                if (bookUpdateIdSbe == bookUpdateIdJson)
                {
                    Console.Write("Tie     ");

                }
                else if (bookUpdateIdSbe > bookUpdateIdJson)
                {
                    Console.Write($"SBE  win lag: {bookUpdateIdSbe - bookUpdateIdJson}");
                }
                else
                {
                    Console.Write($"Json win lag: {bookUpdateIdJson - bookUpdateIdSbe}");
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
            var parser = new MessageParser()
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
                    //string symbol = "";
                    //decimal qtyMultiplier = (decimal)Math.Pow(10, msg.QtyExponent.Value);
                    //decimal priceMultiplier = (decimal)Math.Pow(10, msg.PriceExponent.Value);
                    //msg.ConsumeVariableLengthSegments(extra,
                    //    s =>
                    //    {
                    //        symbol = Encoding.ASCII.GetString(s.VarData);
                    //    });
                    //Console.WriteLine($"s: {symbol}, t: {msg.EventTime.Value}, id:{msg.BookUpdateId.Value}, bq: {msg.BidQty.Value * qtyMultiplier}, bp: {msg.BidPrice.Value * priceMultiplier}, ap: {msg.AskPrice.Value * priceMultiplier}, aq: {msg.AskQty.Value * qtyMultiplier}");
                }
            };
            var buffer = new Memory<byte>(new byte[8192]);
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("WebSocket closed");
                    break;
                }
                else
                {
                    parser.Parse(buffer.Span.Slice(0, result.Count));
                }
            }
        }
        private static async Task WatchJsonBestBid(string key, CancellationTokenSource cts)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", key);
            await ws.ConnectAsync(new Uri("wss://stream.binance.com/ws/btcusdt@bookTicker"), cts.Token);

            //Console.WriteLine("Connected to Binance WebSocket");
            var buffer = new Memory<byte>(new byte[8192 + sizeof(ushort)]);
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("WebSocket closed");
                    break;
                }
                else
                {
                    ParseJsonBookUpdateId(buffer.Span, result);
                }
            }
        }

        private static void ParseJsonBookUpdateId(Span<byte> buffer, ValueWebSocketReceiveResult result)
        {
            var reader = new Utf8JsonReader(buffer.Slice(0, result.Count));
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "u")
                {
                    reader.Read();
                    bookUpdateIdJson = reader.GetInt64();
                    break;
                }
            }
        }
    }
}
