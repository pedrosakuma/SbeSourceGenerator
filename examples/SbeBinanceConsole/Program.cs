using Binance.Stream;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

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

            RootCommand rootCommand = new("Example app to Binance SBE subscription");

            var bestBidAskCommand = new Command(
                "bestBidAsk",
                "Subscribe to BestBidAsk")
            {
                argumentKey,
                instrument
            };
            rootCommand.Subcommands.Add(bestBidAskCommand);

            var tradeCommand = new Command(
                "trade",
                "Subscribe to Trade")
            {
                argumentKey,
                instrument
            };
            rootCommand.Subcommands.Add(tradeCommand);

            CancellationTokenSource source = new CancellationTokenSource();
            bestBidAskCommand.SetAction(parseResult => BestBidAsk(
                parseResult.GetValue(argumentKey),
                parseResult.GetValue(instrument),

                source.Token
            ));

            tradeCommand.SetAction(parseResult => Trade(
                parseResult.GetValue(argumentKey),
                parseResult.GetValue(instrument),
                source.Token
            ));
            await rootCommand.Parse(args).InvokeAsync();
        }
        private static async Task SubscribeAsync(string? key, string? subscription, string? instrument, Action<MessageHeader, ReadOnlySpan<byte>> action, CancellationToken token)
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
                if (!MessageHeader.TryParse(span, out var header, out var body))
                {
                    continue;
                }
                action(header, body);
            }
        }
        private static Task Trade(string? key, string? instrument, CancellationToken token)
        {
            return SubscribeAsync(key, "trade", instrument,
                (header, body) =>
                {
                    switch (header.TemplateId)
                    {
                        case TradesStreamEventData.MESSAGE_ID:
                            if (TradesStreamEventData.TryParse(body, out var trades, out var tradesVariableData))
                            {
                                TradesStreamEventData.TradesData lastTrade = default;
                                decimal qtyMultiplier = (decimal)Math.Pow(10, trades.QtyExponent.Value);
                                decimal priceMultiplier = (decimal)Math.Pow(10, trades.PriceExponent.Value);
                                string? symbol = null;
                                trades.ConsumeVariableLengthSegments(tradesVariableData,
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
        private static Task BestBidAsk(string? key, string? instrument, CancellationToken token)
        {
            return SubscribeAsync(key, "bestBidAsk", instrument,
                (header, body) =>
                {
                    switch (header.TemplateId)
                    {
                        case BestBidAskStreamEventData.MESSAGE_ID:
                            if (BestBidAskStreamEventData.TryParse(body, out var bestBid, out var bestBidVariableData))
                            {
                                string symbol = "";
                                decimal qtyMultiplier = (decimal)Math.Pow(10, bestBid.QtyExponent.Value);
                                decimal priceMultiplier = (decimal)Math.Pow(10, bestBid.PriceExponent.Value);
                                bestBid.ConsumeVariableLengthSegments(bestBidVariableData,
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
    }
}
