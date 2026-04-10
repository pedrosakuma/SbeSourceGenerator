using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Binance.Spot.Stream.V0;
using Binance.Spot.Stream.V0.Runtime;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SbeBinanceConsole;

internal sealed class ConsoleApp : IAsyncDisposable
{
    private static readonly TimeSpan UiRefreshInterval = TimeSpan.FromMilliseconds(150);

    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<InstrumentSubscription> _subscriptions = new();
    private readonly List<string> _feedback = new();
    private readonly StringBuilder _commandBuffer = new();
    private readonly object _sync = new();
    private readonly Channel<bool> _refreshChannel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest
    });

    private readonly Table _instrumentTable = CreateInstrumentTable();
    private readonly Table _tradesTable = CreateTradesTable();
    private readonly Table _bestBidTable = CreateBestBidTable();

    private string? _apiKey;

    public async Task RunAsync(string? initialApiKey, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        var token = linkedCts.Token;

        ConsoleCancelEventHandler onCancel = (_, args) =>
        {
            args.Cancel = true;
            linkedCts.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        try
        {
            _apiKey = !string.IsNullOrWhiteSpace(initialApiKey)
                ? initialApiKey.Trim()
                : PromptForApiKey(token);

            AddFeedback("[grey58]Available commands: add <instrument>, remove <instrument>, api <key>, help, quit.[/]");

            await EnsureInstrumentAndStartAsync("btcusdt", token);
            await EnsureInstrumentAndStartAsync("ethusdt", token);

            await AnsiConsole.Live(Render()).AutoClear(true).StartAsync(async ctx =>
            {
                var commandLoop = Task.Run(() => CommandLoopAsync(linkedCts), CancellationToken.None);

                RequestUiRefresh();

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await WaitForRefreshAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    ctx.UpdateTarget(Render());
                }

                linkedCts.Cancel();

                try
                {
                    await commandLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            });
        }
        finally
        {
            await StopAllAsync();
            Console.CancelKeyPress -= onCancel;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        await StopAllAsync();
        _lifetime.Dispose();
    }

    private static string PromptForApiKey(CancellationToken token)
    {
        while (true)
        {
            var prompt = new TextPrompt<string>("Enter the API key (X-MBX-APIKEY):")
                .Secret()
                .PromptStyle("cyan");

            string key = AnsiConsole.Prompt(prompt);
            token.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(key))
            {
                return key.Trim();
            }

            AnsiConsole.MarkupLine("[yellow]API key cannot be empty.[/]");
        }
    }

    private async Task CommandLoopAsync(CancellationTokenSource cts)
    {
        var token = cts.Token;

        while (!token.IsCancellationRequested)
        {
            if (!Console.KeyAvailable)
            {
                try
                {
                    await Task.Delay(50, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                continue;
            }

            var keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                string command;
                lock (_sync)
                {
                    command = _commandBuffer.ToString();
                    _commandBuffer.Clear();
                }

                RequestUiRefresh();

                if (string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                try
                {
                    await ProcessCommandAsync(command, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AddFeedback($"[red]Erro ao processar comando: {Markup.Escape(ex.Message)}[/]");
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                lock (_sync)
                {
                    if (_commandBuffer.Length > 0)
                    {
                        _commandBuffer.Length--;
                    }
                }

                RequestUiRefresh();
            }
            else if (keyInfo.Key == ConsoleKey.Escape)
            {
                lock (_sync)
                {
                    _commandBuffer.Clear();
                }

                RequestUiRefresh();
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                lock (_sync)
                {
                    _commandBuffer.Append(keyInfo.KeyChar);
                }

                RequestUiRefresh();
            }
        }
    }

    private async Task ProcessCommandAsync(string input, CancellationToken token)
    {
        input = input.Trim();
        if (input.Length == 0)
        {
            return;
        }

        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        switch (command)
        {
            case "add":
                if (string.IsNullOrWhiteSpace(argument))
                {
                    AddFeedback("[yellow]Usage: add <instrument>[/]");
                    return;
                }

                await EnsureInstrumentAndStartAsync(argument.ToLowerInvariant(), token);
                break;

            case "remove":
                if (string.IsNullOrWhiteSpace(argument))
                {
                    AddFeedback("[yellow]Usage: remove <instrument>[/]");
                    return;
                }

                await RemoveInstrumentAsync(argument.ToLowerInvariant());
                break;

            case "api":
                if (string.IsNullOrWhiteSpace(argument))
                {
                    AddFeedback("[yellow]Usage: api <key>[/]");
                    return;
                }

                UpdateApiKey(argument.Trim());
                await RestartAllAsync(token);
                break;

            case "help":
                AddFeedback("[grey58]Commands: add <instrument>, remove <instrument>, api <key>, help, quit[/]");
                break;

            case "quit":
            case "exit":
                _lifetime.Cancel();
                token.ThrowIfCancellationRequested();
                break;

            default:
                AddFeedback($"[yellow]Unknown command: {Markup.Escape(command)}[/]");
                break;
        }
    }

    private void UpdateApiKey(string key)
    {
        lock (_sync)
        {
            _apiKey = key;
        }

        AddFeedback("[green]API key updated. Restarting streams...[/]");
    }

    private async Task EnsureInstrumentAndStartAsync(string symbol, CancellationToken token)
    {
        symbol = symbol.Trim().ToLowerInvariant();
        InstrumentSubscription subscription;
        bool added;

        lock (_sync)
        {
            subscription = _subscriptions.FirstOrDefault(s => string.Equals(s.Symbol, symbol, StringComparison.OrdinalIgnoreCase)) ?? new InstrumentSubscription(symbol);
            added = !_subscriptions.Contains(subscription);
            if (added)
            {
                _subscriptions.Add(subscription);
            }
        }

        if (added)
        {
            AddFeedback($"[green]Instrument {Markup.Escape(symbol.ToUpperInvariant())} added.[/]");
        }
        else
        {
            AddFeedback($"[grey58]Instrument {Markup.Escape(symbol.ToUpperInvariant())} already configured.[/]");
        }

        await StartAllStreamsAsync(subscription, token);
    }

    private async Task RemoveInstrumentAsync(string symbol)
    {
        symbol = symbol.Trim().ToLowerInvariant();
        InstrumentSubscription? subscription;
        var displaySymbol = symbol.ToUpperInvariant();
        bool notFound = false;

        lock (_sync)
        {
            subscription = _subscriptions.FirstOrDefault(s => string.Equals(s.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
            if (subscription is null)
            {
                notFound = true;
            }
            else
            {
                _subscriptions.Remove(subscription);
            }
        }

        if (notFound || subscription is null)
        {
            AddFeedback($"[yellow]Instrument {Markup.Escape(displaySymbol)} not found.[/]");
            return;
        }

        await StopSubscriptionAsync(subscription.Trade);
        await StopSubscriptionAsync(subscription.BestBid);

        AddFeedback($"[green]Instrument {Markup.Escape(displaySymbol)} removed.[/]");
    }

    private async Task RestartAllAsync(CancellationToken token)
    {
        List<InstrumentSubscription> subscriptions;
        lock (_sync)
        {
            subscriptions = _subscriptions.ToList();
        }

        foreach (var subscription in subscriptions)
        {
            await StartAllStreamsAsync(subscription, token);
        }
    }

    private async Task StartAllStreamsAsync(InstrumentSubscription subscription, CancellationToken token)
    {
        await StopSubscriptionAsync(subscription.Trade);
        await StopSubscriptionAsync(subscription.BestBid);

        await StartSubscriptionAsync(subscription, subscription.Trade, SubscriptionKind.Trade, token);
        await StartSubscriptionAsync(subscription, subscription.BestBid, SubscriptionKind.BestBidAsk, token);
    }

    private async Task StartSubscriptionAsync(InstrumentSubscription instrument, SubscriptionState state, SubscriptionKind kind, CancellationToken token)
    {
        string? apiKey;
        lock (_sync)
        {
            if (state.IsActive)
            {
                return;
            }

            apiKey = _apiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            lock (_sync)
            {
                state.Status = "API key required.";
                state.TradeSnapshot = null;
                state.BestBidSnapshot = null;
                state.LastUpdated = DateTimeOffset.UtcNow;
            }

            RequestUiRefresh();
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, token);

        Task worker;
        lock (_sync)
        {
            state.Cancellation = cts;
            state.Status = "Connecting...";
            state.TradeSnapshot = null;
            state.BestBidSnapshot = null;
            state.LastUpdated = DateTimeOffset.UtcNow;
            worker = RunSubscriptionLoopAsync(instrument.Symbol, state, kind, apiKey, cts);
            state.Worker = worker;
        }

        RequestUiRefresh();

        await Task.Yield();
    }

    private async Task StopSubscriptionAsync(SubscriptionState state)
    {
        CancellationTokenSource? cts;
        Task? worker;

        lock (_sync)
        {
            cts = state.Cancellation;
            worker = state.Worker;
            if (cts is null)
            {
                return;
            }

            state.Status = "Stopping...";
        }

        RequestUiRefresh();

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        if (worker is not null)
        {
            try
            {
                await worker.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        lock (_sync)
        {
            if (ReferenceEquals(state.Cancellation, cts))
            {
                state.Status = "Inactive";
                state.LastUpdated = DateTimeOffset.UtcNow;
                state.Cancellation = null;
                state.Worker = null;
                state.TradeSnapshot = null;
                state.BestBidSnapshot = null;
            }
        }

        RequestUiRefresh();
    }

    private async Task RunSubscriptionLoopAsync(string instrument, SubscriptionState state, SubscriptionKind kind, string apiKey, CancellationTokenSource cts)
    {
        try
        {
            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("X-MBX-APIKEY", apiKey);
            var streamName = kind == SubscriptionKind.Trade ? "trade" : "bestBidAsk";
            var endpoint = new Uri($"wss://stream-sbe.binance.com/ws/{instrument.ToLowerInvariant()}@{streamName}");

            await ws.ConnectAsync(endpoint, cts.Token).ConfigureAwait(false);

            lock (_sync)
            {
                state.Status = "Connected";
                state.LastUpdated = DateTimeOffset.UtcNow;
            }

            RequestUiRefresh();

            var buffer = new byte[8192];
            var segment = new ArraySegment<byte>(buffer);

            while (!cts.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(segment, cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var closeInfo = result.CloseStatus.HasValue
                        ? $"{result.CloseStatus} {result.CloseStatusDescription}".Trim()
                        : "Server closed the session.";

                    lock (_sync)
                    {
                        state.Status = $"Closed by server: {closeInfo}";
                        state.TradeSnapshot = null;
                        state.BestBidSnapshot = null;
                        state.LastUpdated = DateTimeOffset.UtcNow;
                    }

                    AddFeedback($"[yellow]Stream {instrument.ToUpperInvariant()} closed by server: {Markup.Escape(closeInfo)}[/]");
                    RequestUiRefresh();
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Binary)
                {
                    continue;
                }

                var reader = new SpanReader(buffer.AsSpan(0, result.Count));

                if (!reader.TryRead<MessageHeader>(out var header))
                {
                    continue;
                }

                if (kind == SubscriptionKind.Trade && header.TemplateId == TradesStreamEventData.MESSAGE_ID)
                {
                    if (TryExtractTradeSnapshot(instrument, ref reader, out var snapshot, out var messageTime))
                    {
                        UpdateTrade(state, snapshot, messageTime);
                    }
                }
                else if (kind == SubscriptionKind.BestBidAsk && header.TemplateId == BestBidAskStreamEventData.MESSAGE_ID)
                {
                    if (TryExtractBestBidSnapshot(instrument, ref reader, out var snapshot, out var messageTime))
                    {
                        UpdateBestBid(state, snapshot, messageTime);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            lock (_sync)
            {
                state.Status = "Canceled";
                state.TradeSnapshot = null;
                state.BestBidSnapshot = null;
                state.LastUpdated = DateTimeOffset.UtcNow;
            }

            RequestUiRefresh();
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                state.Status = $"Error: {ex.Message}";
                state.TradeSnapshot = null;
                state.BestBidSnapshot = null;
                state.LastUpdated = DateTimeOffset.UtcNow;
            }

            AddFeedback($"[red]Stream {instrument.ToUpperInvariant()} error: {Markup.Escape(ex.Message)}[/]");
            RequestUiRefresh();
        }
        finally
        {
            cts.Dispose();

            lock (_sync)
            {
                if (ReferenceEquals(state.Cancellation, cts))
                {
                    state.Cancellation = null;
                    state.Worker = null;
                }
            }

            RequestUiRefresh();
        }
    }

    private void UpdateTrade(SubscriptionState state, TradeSnapshot snapshot, DateTimeOffset eventTime)
    {
        lock (_sync)
        {
            state.Status = "Streaming";
            state.TradeSnapshot = snapshot;
            state.LastUpdated = eventTime;
        }

        RequestUiRefresh();
    }

    private void UpdateBestBid(SubscriptionState state, BestBidAskSnapshot snapshot, DateTimeOffset eventTime)
    {
        lock (_sync)
        {
            state.Status = "Streaming";
            state.BestBidSnapshot = snapshot;
            state.LastUpdated = eventTime;
        }

        RequestUiRefresh();
    }

    private async ValueTask StopAllAsync()
    {
        List<InstrumentSubscription> subscriptions;
        lock (_sync)
        {
            subscriptions = _subscriptions.ToList();
        }

        foreach (var subscription in subscriptions)
        {
            await StopSubscriptionAsync(subscription.Trade).ConfigureAwait(false);
            await StopSubscriptionAsync(subscription.BestBid).ConfigureAwait(false);
        }
    }

    private IRenderable Render()
    {
        string? apiKey;
        List<SubscriptionSnapshot> snapshot;
        string[] feedback;
        string commandBuffer;

        lock (_sync)
        {
            apiKey = _apiKey;
            snapshot = _subscriptions
                .Select(s => new SubscriptionSnapshot(
                    s.Symbol,
                    CreateStateSnapshot(s.Trade),
                    CreateStateSnapshot(s.BestBid)))
                .ToList();
            feedback = _feedback.ToArray();
            commandBuffer = _commandBuffer.ToString();
        }

        var header = new Panel(new Markup($"[bold]Binance SBE Streams[/]\nAPI Key: {FormatApiKey(apiKey)}"))
            .BorderColor(Color.Grey62)
            .Expand();

        IRenderable instrumentsRenderable;
        if (snapshot.Count == 0)
        {
            _instrumentTable.Rows.Clear();
            instrumentsRenderable = new Markup("[grey58]No instruments configured. Use add <instrument>.\n[/]");
        }
        else
        {
            UpdateInstrumentTable(snapshot);
            instrumentsRenderable = _instrumentTable;
        }

        UpdateTradesTable(snapshot);
        UpdateBestBidTable(snapshot);

        var instrumentsPanel = new Panel(instrumentsRenderable)
            .Header("Instruments", Justify.Center)
            .BorderColor(Color.Grey50)
            .Expand();

        var tradesPanel = new Panel(_tradesTable)
            .Header("Trades", Justify.Center)
            .BorderColor(Color.Grey50)
            .Expand();

        var bestBidPanel = new Panel(_bestBidTable)
            .Header("Best Bid/Ask", Justify.Center)
            .BorderColor(Color.Grey50)
            .Expand();

        IRenderable feedbackRenderable = feedback.Length == 0
            ? new Markup("[grey54]No messages.[/]")
            : new Rows(feedback.Select(message => (IRenderable)new Markup(message)).ToArray());

        var feedbackPanel = new Panel(feedbackRenderable)
            .Header("Messages", Justify.Center)
            .BorderColor(Color.Grey42)
            .Expand();

        return new Rows(
            header,
            BuildCommandPanel(commandBuffer),
            instrumentsPanel,
            tradesPanel,
            bestBidPanel,
            feedbackPanel);
    }

    private static Table CreateInstrumentTable()
    {
        var table = new Table().Expand();
        table.AddColumn(new TableColumn("Instrument").Centered());
        table.AddColumn(new TableColumn("Trade").Centered());
        table.AddColumn(new TableColumn("Best Bid/Ask").Centered());
        return table;
    }

    private static Table CreateTradesTable()
    {
        var table = new Table().Expand();
        table.AddColumns("Instrument", "Trade Id", "Quantity", "Price", "Aggressor");
        return table;
    }

    private static Table CreateBestBidTable()
    {
        var table = new Table().Expand();
        table.AddColumns("Instrument", "Bid", "Bid Qty", "Ask", "Ask Qty");
        return table;
    }

    private void UpdateInstrumentTable(IEnumerable<SubscriptionSnapshot> snapshot)
    {
        _instrumentTable.Rows.Clear();
        foreach (var item in snapshot)
        {
            _instrumentTable.AddRow(
                Markup.FromInterpolated($"[white]{Markup.Escape(item.Symbol.ToUpperInvariant())}[/]"),
                Markup.FromInterpolated($"[grey78]{Markup.Escape(FormatStatus(item.Trade))}[/]"),
                Markup.FromInterpolated($"[grey78]{Markup.Escape(FormatStatus(item.BestBid))}[/]"));
        }
    }

    private void UpdateTradesTable(IEnumerable<SubscriptionSnapshot> snapshot)
    {
        _tradesTable.Rows.Clear();
        foreach (var item in snapshot)
        {
            var trade = item.Trade.TradeSnapshot;
            _tradesTable.AddRow(
                ToCellContent(item.Symbol.ToUpperInvariant(), "{0,-20}"),
                ToCellContent(trade?.TradeId, "{0,18}"),
                ToCellContent(trade?.Quantity, "{0,18}"),
                ToCellContent(trade?.Price, "{0,18}"),
                ToCellContent(trade?.Aggressor, "{0,-12}"));
        }
    }

    private void UpdateBestBidTable(IEnumerable<SubscriptionSnapshot> snapshot)
    {
        _bestBidTable.Rows.Clear();
        foreach (var item in snapshot)
        {
            var bestBid = item.BestBid.BestBidSnapshot;
            _bestBidTable.AddRow(
                ToCellContent(item.Symbol.ToUpperInvariant(), "{0,-20}"),
                ToCellContent(bestBid?.BidPrice, "{0,18}"),
                ToCellContent(bestBid?.BidQuantity, "{0,18}"),
                ToCellContent(bestBid?.AskPrice, "{0,18}"),
                ToCellContent(bestBid?.AskQuantity, "{0,18}"));
        }
    }

    private static Panel BuildCommandPanel(string commandBuffer)
    {
        var escapedBuffer = Markup.Escape(commandBuffer);
        IRenderable promptBody = new Rows(
            new Markup("[grey46]Commands: add <instrument>, remove <instrument>, api <key>, help, quit[/]"),
            new Markup($"[silver]> {escapedBuffer}_[/]")
        );

        return new Panel(promptBody)
            .Header("Input", Justify.Center)
            .BorderColor(Color.Grey50)
            .Expand();
    }

    private async Task WaitForRefreshAsync(CancellationToken token)
    {
        while (await _refreshChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
        {
            while (_refreshChannel.Reader.TryRead(out _))
            {
                // drain channel
            }
            break;
        }

        await Task.Delay(UiRefreshInterval, token).ConfigureAwait(false);
    }

    private void RequestUiRefresh()
    {
        _refreshChannel.Writer.TryWrite(true);
    }

    private void AddFeedback(string message)
    {
        lock (_sync)
        {
            _feedback.Add(message);
            if (_feedback.Count > 5)
            {
                _feedback.RemoveAt(0);
            }
        }

        RequestUiRefresh();
    }

    private static string FormatApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "[red]Not configured[/]";
        }

        var trimmed = apiKey.Trim();
        if (trimmed.Length <= 4)
        {
            return "****";
        }

        var masked = new string('*', trimmed.Length - 4) + trimmed[^4..];
        return Markup.Escape(masked);
    }

    private static string FormatStatus(StateSnapshot state)
    {
        return state.LastUpdated.HasValue
            ? $"{state.Status} - {state.LastUpdated.Value.ToLocalTime():HH:mm:ss}"
            : state.Status;
    }

    private static StateSnapshot CreateStateSnapshot(SubscriptionState state)
    {
        return new StateSnapshot(
            state.Label,
            state.Status,
            state.LastUpdated,
            state.TradeSnapshot,
            state.BestBidSnapshot);
    }

    private static bool TryExtractTradeSnapshot(string instrument, ref SpanReader reader, out TradeSnapshot snapshot, out DateTimeOffset eventTime)
    {
        snapshot = default;
        eventTime = default;

        if (!TradesStreamEventData.TryParseWithReader(ref reader, TradesStreamEventData.BLOCK_LENGTH, out var tradesReader))
        {
            return false;
        }

        ref readonly var trades = ref tradesReader.Data;
        var symbol = instrument;
        var hasTrade = false;
        TradesStreamEventData.TradesData lastTrade = default;

        tradesReader.ReadGroups(
            (in TradesStreamEventData.TradesData data) =>
            {
                hasTrade = true;
                lastTrade = data;
            },
            s => symbol = Encoding.UTF8.GetString(s.VarData[..s.Length]));
        reader.TrySkip(tradesReader.BytesConsumed);

        if (!hasTrade)
        {
            return false;
        }

        var qtyMultiplier = Pow10(trades.QtyExponent.Value);
        var priceMultiplier = Pow10(trades.PriceExponent.Value);

        var qty = lastTrade.Qty.Value * qtyMultiplier;
        var price = lastTrade.Price.Value * priceMultiplier;
        var aggressor = lastTrade.IsBuyerMaker == BoolEnum.True ? "Buyer maker" : "Seller maker";

        snapshot = new TradeSnapshot(
            symbol.ToUpperInvariant(),
            lastTrade.Id.Value,
            qty,
            price,
            aggressor);
        eventTime = ToDateTime(trades.EventTime.Value);
        return true;
    }

    private static bool TryExtractBestBidSnapshot(string instrument, ref SpanReader reader, out BestBidAskSnapshot snapshot, out DateTimeOffset eventTime)
    {
        snapshot = default;
        eventTime = default;

        if (!BestBidAskStreamEventData.TryParseWithReader(ref reader, BestBidAskStreamEventData.BLOCK_LENGTH, out var bestBidReader))
        {
            return false;
        }

        ref readonly var bestBid = ref bestBidReader.Data;
        var symbol = instrument;

        bestBidReader.ReadGroups(
            s => symbol = Encoding.UTF8.GetString(s.VarData[..s.Length]));
        reader.TrySkip(bestBidReader.BytesConsumed);

        var qtyMultiplier = Pow10(bestBid.QtyExponent.Value);
        var priceMultiplier = Pow10(bestBid.PriceExponent.Value);

        snapshot = new BestBidAskSnapshot(
            symbol.ToUpperInvariant(),
            bestBid.BidPrice.Value * priceMultiplier,
            bestBid.BidQty.Value * qtyMultiplier,
            bestBid.AskPrice.Value * priceMultiplier,
            bestBid.AskQty.Value * qtyMultiplier);
        eventTime = ToDateTime(bestBid.EventTime.Value);
        return true;
    }

    private static string ToCellContent(string? value, string format)
    {
        var content = string.IsNullOrEmpty(value) ? "-" : value;
        return Markup.Escape(string.Format(CultureInfo.InvariantCulture, format, content));
    }

    private static string ToCellContent(long? value, string format) =>
        value.HasValue
            ? Markup.Escape(string.Format(CultureInfo.InvariantCulture, format, value.Value))
            : "-";

    private static string ToCellContent(decimal? value, string format) =>
        value.HasValue
            ? Markup.Escape(string.Format(CultureInfo.InvariantCulture, format, value.Value))
            : "-";

    private static DateTimeOffset ToDateTime(long microseconds) =>
        DateTimeOffset.UnixEpoch.AddTicks(microseconds * 10);

    private static decimal Pow10(sbyte exponent)
    {
        if (exponent == 0)
        {
            return 1m;
        }

        var result = 1m;
        var steps = Math.Abs(exponent);
        for (var i = 0; i < steps; i++)
        {
            result = exponent > 0 ? result * 10m : result / 10m;
        }

        return result;
    }

    private sealed class InstrumentSubscription
    {
        public InstrumentSubscription(string symbol)
        {
            Symbol = symbol;
            Trade = new SubscriptionState("Trade", "trade");
            BestBid = new SubscriptionState("Best Bid/Ask", "bestBidAsk");
        }

        public string Symbol { get; }
        public SubscriptionState Trade { get; }
        public SubscriptionState BestBid { get; }
    }

    private sealed class SubscriptionState
    {
        public SubscriptionState(string label, string streamName)
        {
            Label = label;
            StreamName = streamName;
        }

        public string Label { get; }
        public string StreamName { get; }
        public string Status { get; set; } = "Inactive";
        public DateTimeOffset? LastUpdated { get; set; }
        public CancellationTokenSource? Cancellation { get; set; }
        public Task? Worker { get; set; }
        public TradeSnapshot? TradeSnapshot { get; set; }
        public BestBidAskSnapshot? BestBidSnapshot { get; set; }
        public bool IsActive => Cancellation is { IsCancellationRequested: false };
    }

    private sealed record StateSnapshot(
        string Label,
        string Status,
        DateTimeOffset? LastUpdated,
        TradeSnapshot? TradeSnapshot,
        BestBidAskSnapshot? BestBidSnapshot);

    private sealed record SubscriptionSnapshot(
        string Symbol,
        StateSnapshot Trade,
        StateSnapshot BestBid);

    private readonly record struct TradeSnapshot(string Symbol, long TradeId, decimal Quantity, decimal Price, string Aggressor);

    private readonly record struct BestBidAskSnapshot(string Symbol, decimal BidPrice, decimal BidQuantity, decimal AskPrice, decimal AskQuantity);

    private enum SubscriptionKind
    {
        Trade,
        BestBidAsk
    }
}
