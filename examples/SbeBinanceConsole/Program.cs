using Binance.Stream;
using Binance.Stream.Runtime;
using System.CommandLine;
using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;

namespace SbeBinanceConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Argument<string> argumentKey = new("key")
            {
                Description = "Binance API Key (Ed25519)"
            };
            Argument<string> instrument = new("instrument")
            {
                Description = "Instrument"
            };

            CancellationTokenSource source = new CancellationTokenSource();
            Console.CancelKeyPress += (_, _) =>
            {
                Console.WriteLine("Cancel requested");
                source.Cancel();
            };
            RootCommand rootCommand = new("Example app to Binance SBE subscription");

            AddCommand(rootCommand,
                "bestBidAsk", "Subscribe to BestBidAsk", BestBidAsk,
                argumentKey, instrument);

            AddCommand(rootCommand,
                "trade", "Subscribe to Trade", Trade,
                argumentKey, instrument);

            AddCommand(rootCommand,
                "depth", "Subscribe to OrderBook", OrderBook,
                argumentKey, instrument);

            await rootCommand.Parse(args).InvokeAsync(cancellationToken: source.Token);
        }

        private static void AddCommand(RootCommand rootCommand,
            string commandName, string commandDescription,
            Func<string, string, CancellationToken, Task> func,
            Argument<string> argumentKey, Argument<string> instrument)
        {
            var command = new Command(
                commandName,
                commandDescription)
            {
                argumentKey,
                instrument
            };
            command.SetAction((parseResult, token) => func(
                parseResult.GetRequiredValue(argumentKey),
                parseResult.GetRequiredValue(instrument),

                token
            ));
            rootCommand.Subcommands.Add(command);
        }

        private static async Task SubscribeAsync(string? key, string? subscription, string? instrument, Action<MessageHeader, SpanReader> action, CancellationToken token)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", key);
            Console.WriteLine("Connecting to Binance WebSocket");
            await ws.ConnectAsync(new Uri($"wss://stream-sbe.binance.com/ws/{instrument}@{subscription}"), token);
            Console.WriteLine("Connected to Binance WebSocket");
            var buffer = new Memory<byte>(new byte[8192]);
            while (!token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("WebSocket closed");
                    break;
                }
                var span = buffer.Span.Slice(0, result.Count);
                var reader = new SpanReader(span);
                if(!reader.TryRead<MessageHeader>(out var header))
                {
                    continue;
                }
                action(header, reader);
            }
        }
        private static Task Trade(string key, string instrument, CancellationToken token)
        {
            return SubscribeAsync(key, "trade", instrument,
                (header, reader) =>
                {
                    switch (header.TemplateId)
                    {
                        case TradesStreamEventData.MESSAGE_ID:
                            if (reader.TryRead<TradesStreamEventData>(out var trades))
                            {
                                TradesStreamEventData.TradesData lastTrade = default;
                                decimal qtyMultiplier = (decimal)Math.Pow(10, trades.QtyExponent.Value);
                                decimal priceMultiplier = (decimal)Math.Pow(10, trades.PriceExponent.Value);
                                string? symbol = null;
                                
                                trades.ConsumeVariableLengthSegments(ref reader,
                                trade =>
                                    {
                                        lastTrade = trade;
                                    },
                                    s =>
                                    {
                                        symbol = Encoding.ASCII.GetString(s.VarData[..s.Length]);
                                    });
                                Console.WriteLine($"s:{symbol}, id: {lastTrade.Id.Value}, q: {lastTrade.Qty.Value * priceMultiplier}, p: {lastTrade.Price.Value * priceMultiplier}");
                            }
                            break;
                    }
                }, token);
        }
        private static Task BestBidAsk(string key, string instrument, CancellationToken token)
        {
            return SubscribeAsync(key, "bestBidAsk", instrument,
                (header, reader) =>
                {
                    switch (header.TemplateId)
                    {
                        case BestBidAskStreamEventData.MESSAGE_ID:
                            if (reader.TryRead<BestBidAskStreamEventData>(out var bestBid))
                            {
                                string symbol = "";
                                decimal qtyMultiplier = (decimal)Math.Pow(10, bestBid.QtyExponent.Value);
                                decimal priceMultiplier = (decimal)Math.Pow(10, bestBid.PriceExponent.Value);
                                bestBid.ConsumeVariableLengthSegments(ref reader,
                                   s =>
                                   {
                                       symbol = Encoding.ASCII.GetString(s.VarData[..s.Length]);
                                   });
                                Console.WriteLine($"s: {symbol}, t: {bestBid.EventTime.Value}, id:{bestBid.BookUpdateId.Value}, bq: {bestBid.BidQty.Value * qtyMultiplier}, bp: {bestBid.BidPrice.Value * priceMultiplier}, ap: {bestBid.AskPrice.Value * priceMultiplier}, aq: {bestBid.AskQty.Value * qtyMultiplier}");
                            }
                            break;
                        default:
                            Console.WriteLine(header);
                            break;
                    }
                }, token);
        }

        private static async Task OrderBook(string key, string instrument, CancellationToken token)
        {
            ManualResetEventSlim mres = new ManualResetEventSlim();
            List<OrderBookEntry> bids = new List<OrderBookEntry>(5000);
            List<OrderBookEntry> asks = new List<OrderBookEntry>(5000);
            var bidsComparer = new BidsComparer();
            var asksComparer = new AsksComparer();
            long lastUpdateId = 0L;
            var task = SubscribeAsync(key, "depth", instrument,
                (header, reader) =>
                {
                    switch (header.TemplateId)
                    {
                        case DepthDiffStreamEventData.MESSAGE_ID:
                            if (lastUpdateId == 0)
                            {
                                mres.Wait();
                            }
                            if (reader.TryRead<DepthDiffStreamEventData>(out var depth))
                            {
                                if (depth.FirstBookUpdateId.Value > lastUpdateId)
                                {
                                    int minUpdatedIndex = int.MaxValue;
                                    string symbol = "";
                                    decimal qtyMultiplier = (decimal)Math.Pow(10, depth.QtyExponent.Value);
                                    decimal priceMultiplier = (decimal)Math.Pow(10, depth.PriceExponent.Value);
                                    depth.ConsumeVariableLengthSegments(ref reader,
                                        cb =>
                                        {
                                                var entry = new OrderBookEntry
                                                {
                                                    Price = cb.Price.Value * priceMultiplier,
                                                    Quantity = cb.Qty.Value * qtyMultiplier
                                                };
                                                int index = bids.BinarySearch(entry, bidsComparer);
                                                if (index < 0)
                                                {
                                                    if (entry.Quantity != 0)
                                                    {
                                                        bids.Insert(~index, entry);
                                                        minUpdatedIndex = int.Min(minUpdatedIndex, ~index);
                                                    }
                                                }
                                                else
                                                {
                                                    if (entry.Quantity == 0)
                                                        bids.RemoveAt(index);
                                                    else
                                                        bids[index].Quantity = entry.Quantity;
                                                    minUpdatedIndex = int.Min(minUpdatedIndex, index);
                                                }
                                        },
                                        ca =>
                                        {
                                            var entry = new OrderBookEntry
                                            {
                                                Price = ca.Price.Value * priceMultiplier,
                                                Quantity = ca.Qty.Value * qtyMultiplier
                                            };
                                            int index = asks.BinarySearch(entry, asksComparer);
                                            if (index < 0)
                                            {
                                                if (entry.Quantity != 0)
                                                {
                                                    asks.Insert(~index, entry);
                                                    minUpdatedIndex = int.Min(minUpdatedIndex, ~index);
                                                }
                                            }
                                            else
                                            {
                                                if (entry.Quantity == 0)
                                                    asks.RemoveAt(index);
                                                else
                                                    asks[index].Quantity = entry.Quantity;
                                                minUpdatedIndex = int.Min(minUpdatedIndex, index);
                                            }
                                        },
                                        s =>
                                        {
                                            symbol = Encoding.ASCII.GetString(s.VarData[..s.Length]);
                                        });
                                    lastUpdateId = depth.LastBookUpdateId.Value;
                                    if (minUpdatedIndex <= 10)
                                    {
                                        Console.SetCursorPosition(0, 0);
                                        for (int i = 0; i < 10; i++)
                                        {
                                            Console.WriteLine($"{bids[i].Quantity} {bids[i].Price} - {asks[i].Price} {asks[i].Quantity}");
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            Console.WriteLine(header);
                            break;
                    }
                }, token);
            var client = new HttpClient();
            var response = await client.GetAsync($"https://api.binance.com/api/v3/depth?symbol={instrument.ToUpperInvariant()}&limit=5000");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var c = await response.Content.ReadAsStringAsync();
                Console.WriteLine(c);
            }
            var content = await response.Content.ReadFromJsonAsync<OrderBookSnapshot>(token);
            if (content != null)
            {
                foreach (var item in content.Bids)
                {
                    bids.Add(new OrderBookEntry
                    {
                        Price = item[0],
                        Quantity = item[1]
                    });
                }
                foreach (var item in content.Asks)
                {
                    asks.Add(new OrderBookEntry
                    {
                        Price = item[0],
                        Quantity = item[1]
                    });
                }
                Console.WriteLine("snapshot ok");
                Console.Clear();
                lastUpdateId = content.LastUpdateId;
                mres.Set();
            }
            await task;
        }

    }
    public class BidsComparer : IComparer<OrderBookEntry>
    {
        public int Compare(OrderBookEntry? x, OrderBookEntry? y)
        {
            return decimal.Compare(y!.Price, x!.Price);
        }
    }
    public class AsksComparer : IComparer<OrderBookEntry>
    {
        public int Compare(OrderBookEntry? x, OrderBookEntry? y)
        {
            return decimal.Compare(x!.Price, y!.Price);
        }
    }
    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
    public class OrderBookSnapshot
    {
        public long LastUpdateId { get; set; }
        public required decimal[][] Bids { get; set; }
        public required decimal[][] Asks { get; set; }
    }
}
