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
                var span = buffer.Span.Slice(0, result.Count);
                if (!MessageHeader.TryParse(span, out var header, out var body))
                {
                    continue;
                }

                switch (header.TemplateId)
                {
                    case BestBidAskStreamEventData.MESSAGE_ID:
                        if (BestBidAskStreamEventData.TryParse(body, out var bestBid, out var bestBidVariableData))
                        {
                            bookUpdateIdSbe = bestBid.BookUpdateId.Value;
                            //Console.WriteLine($"BestBidAskStreamEventMessageReceived: ");
                            //string symbol = "";
                            //decimal qtyMultiplier = (decimal)Math.Pow(10, bestBid.QtyExponent.Value);
                            //decimal priceMultiplier = (decimal)Math.Pow(10, bestBid.PriceExponent.Value);
                            //bestBid.ConsumeVariableLengthSegments(bestBidVariableData,
                            //    s =>
                            //    {
                            //        symbol = Encoding.ASCII.GetString(s.VarData[..s.Length]);
                            //    });
                            //Console.WriteLine($"s: {symbol}, t: {bestBid.EventTime.Value}, id:{bestBid.BookUpdateId.Value}, bq: {bestBid.BidQty.Value * qtyMultiplier}, bp: {bestBid.BidPrice.Value * priceMultiplier}, ap: {bestBid.AskPrice.Value * priceMultiplier}, aq: {bestBid.AskQty.Value * qtyMultiplier}");
                        }
                        break;
                    case TradesStreamEventData.MESSAGE_ID:
                        if (TradesStreamEventData.TryParse(body, out var trades, out var tradesVariableData))
                        {
                            //decimal qtyMultiplier = (decimal)Math.Pow(10, trades.QtyExponent.Value);
                            //decimal priceMultiplier = (decimal)Math.Pow(10, trades.PriceExponent.Value);
                            //Console.WriteLine($"TradesStreamEventMessageReceived: ");
                            //trades.ConsumeVariableLengthSegments(tradesVariableData,
                            //    trade =>
                            //    {
                            //        Console.WriteLine($"{trade.Id.Value} - q: {trade.Qty.Value * qtyMultiplier}, p: {trade.Price.Value * priceMultiplier}");
                            //    },
                            //    symbol =>
                            //    {
                            //        Console.WriteLine(Encoding.ASCII.GetString(symbol.VarData[..symbol.Length]));
                            //    });
                        }
                        break;
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
