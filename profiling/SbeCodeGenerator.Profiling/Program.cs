using Microsoft.CodeAnalysis;
using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using System.Diagnostics;

var profiler = new GeneratorProfiler();
return profiler.Run();

class GeneratorProfiler
{
    const int Iterations = 500;
    const int WarmupIterations = 50;

    public int Run()
    {
        var schemaDir = Path.Combine(AppContext.BaseDirectory, "Schemas");
        var schemaFiles = Directory.GetFiles(schemaDir, "*.xml");

        if (schemaFiles.Length == 0)
        {
            Console.Error.WriteLine($"No XML schemas found in {schemaDir}");
            return 1;
        }

        Console.WriteLine($"Found {schemaFiles.Length} schemas. Warmup: {WarmupIterations}, Measured: {Iterations}");
        Console.WriteLine(new string('-', 60));

        var schemas = new List<(string path, string content)>();
        foreach (var file in schemaFiles)
            schemas.Add((file, File.ReadAllText(file)));

        // Warmup
        for (int i = 0; i < WarmupIterations; i++)
            RunFullPipeline(schemas);

        // Measured — total
        var swTotal = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
            RunFullPipeline(schemas);
        swTotal.Stop();

        Console.WriteLine($"\nTotal: {swTotal.Elapsed.TotalMilliseconds:F1}ms for {Iterations} iterations ({swTotal.Elapsed.TotalMilliseconds / Iterations:F3}ms/iter)");

        // Phase breakdown
        Console.WriteLine("\n--- Phase Breakdown (per iteration avg) ---");

        double parseMs = MeasureParsePhase(schemas, Iterations);
        double typesMs = MeasureGeneratorPhase(schemas, Iterations, PhaseTypes);
        double messagesMs = MeasureGeneratorPhase(schemas, Iterations, PhaseMessages);
        double utilitiesMs = MeasureGeneratorPhase(schemas, Iterations, PhaseUtilities);

        Console.WriteLine($"  XML Parse:    {parseMs / Iterations:F3}ms");
        Console.WriteLine($"  Types Gen:    {typesMs / Iterations:F3}ms");
        Console.WriteLine($"  Messages Gen: {messagesMs / Iterations:F3}ms");
        Console.WriteLine($"  Utilities:    {utilitiesMs / Iterations:F3}ms");

        Console.WriteLine("\nDone. If running under dotnet-trace, stop now (Ctrl+C).");
        return 0;
    }

    void RunFullPipeline(List<(string path, string content)> schemas)
    {
        var sourceContext = default(SourceProductionContext);
        var emittedRuntimeNamespaces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (path, content) in schemas)
        {
            var parsed = SchemaReader.Parse(content);
            var context = new SchemaContext("profiling", emittedRuntimeNamespaces);

            if (!string.IsNullOrEmpty(parsed.ByteOrder))
                context.ByteOrder = parsed.ByteOrder;

            ICodeGenerator[] generators =
            [
                new TypesCodeGenerator(),
                new MessagesCodeGenerator(),
                new UtilitiesCodeGenerator(),
                new ValidationGenerator(),
            ];

            foreach (var gen in generators)
            {
                foreach (var item in gen.Generate("Profiling.Test", parsed, context, sourceContext))
                    _ = item.content.Length;
            }
        }
    }

    double MeasureParsePhase(List<(string path, string content)> schemas, int iterations)
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            foreach (var (_, content) in schemas)
                _ = SchemaReader.Parse(content);
        }
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    double MeasureGeneratorPhase(List<(string path, string content)> schemas, int iterations,
        Action<ParsedSchema, SchemaContext, SourceProductionContext> phase)
    {
        var prepared = new List<(ParsedSchema parsed, SchemaContext ctx)>();
        foreach (var (_, content) in schemas)
        {
            var parsed = SchemaReader.Parse(content);
            var ctx = new SchemaContext("profiling", new HashSet<string>(StringComparer.Ordinal));
            if (!string.IsNullOrEmpty(parsed.ByteOrder))
                ctx.ByteOrder = parsed.ByteOrder;
            // Run types first to populate context for downstream phases
            var typesGen = new TypesCodeGenerator();
            foreach (var item in typesGen.Generate("Profiling.Test", parsed, ctx, default))
                _ = item.content.Length;
            prepared.Add((parsed, ctx));
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            foreach (var (parsed, ctx) in prepared)
                phase(parsed, ctx, default);
        }
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    void PhaseTypes(ParsedSchema parsed, SchemaContext ctx, SourceProductionContext sc)
    {
        var gen = new TypesCodeGenerator();
        foreach (var item in gen.Generate("Profiling.Test", parsed, ctx, sc))
            _ = item.content.Length;
    }

    void PhaseMessages(ParsedSchema parsed, SchemaContext ctx, SourceProductionContext sc)
    {
        var gen = new MessagesCodeGenerator();
        foreach (var item in gen.Generate("Profiling.Test", parsed, ctx, sc))
            _ = item.content.Length;
    }

    void PhaseUtilities(ParsedSchema parsed, SchemaContext ctx, SourceProductionContext sc)
    {
        var gen = new UtilitiesCodeGenerator();
        foreach (var item in gen.Generate("Profiling.Test", parsed, ctx, sc))
            _ = item.content.Length;
    }
}
