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

        private static async Task Trade(string? key, string? instrument, CancellationToken token)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", key);
            Console.WriteLine("Connecting to Binance WebSocket");
            await ws.ConnectAsync(new Uri("wss://stream-sbe.binance.com/ws/btcusdt@trade"), token);
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
                var tradesDataBuffer = new TradesStreamEventData.TradesData[256];

                switch (header.TemplateId)
                {
                    case TradesStreamEventData.MESSAGE_ID:
                        if (TradesStreamEventData.TryParse(body, out var trades, out var tradesVariableData))
                        {
                            decimal qtyMultiplier = (decimal)Math.Pow(10, trades.QtyExponent.Value);
                            decimal priceMultiplier = (decimal)Math.Pow(10, trades.PriceExponent.Value);
                            int index = 0;
                            string symbol = null;
                            trades.ConsumeVariableLengthSegments(tradesVariableData,
                               trade =>
                               {
                                   tradesDataBuffer[index++] = trade;
                               },
                               s =>
                               {
                                   symbol = Encoding.ASCII.GetString(s.VarData[..s.Length]);
                               });
                            for (int i = 0; i < index; i++)
                            {
                                var t = tradesDataBuffer[i];
                                Console.WriteLine($"s:{symbol}, id: {t.Id.Value}, q: {t.Qty.Value * priceMultiplier}, p: {t.Price.Value * priceMultiplier}");
                            }
                        }
                        break;
                }
            }        }
        private static async Task BestBidAsk(string? key, string? instrument, CancellationToken token)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", key);
            Console.WriteLine("Connecting to Binance WebSocket");
            await ws.ConnectAsync(new Uri("wss://stream-sbe.binance.com/ws/btcusdt@bestBidAsk"), token);
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
                }
            }
        }
    }
}
