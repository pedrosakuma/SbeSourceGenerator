using System;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;

namespace SbeBinanceConsole
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            await using var app = new ConsoleApp();
            var initialKey = args.FirstOrDefault();

            try
            {
                await app.RunAsync(initialKey).ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Erro fatal: {Markup.Escape(ex.Message)}[/]");
                return 1;
            }
        }

    }
}
