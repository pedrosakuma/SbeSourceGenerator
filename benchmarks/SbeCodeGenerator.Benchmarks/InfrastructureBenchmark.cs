using BenchmarkDotNet.Attributes;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Placeholder benchmark demonstrating the infrastructure.
/// Actual benchmarks require generated code from SBE schemas.
/// 
/// To enable full benchmarks:
/// 1. Ensure the source generator is working
/// 2. Rename .disabled files to .cs
/// 3. Build and run: dotnet run -c Release
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class InfrastructureBenchmark
{
    private byte[] _buffer = null!;
    private const int BufferSize = 1024;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[BufferSize];
    }

    [Benchmark(Description = "Array Allocation", Baseline = true)]
    public byte[] AllocateArray()
    {
        return new byte[BufferSize];
    }

    [Benchmark(Description = "Stack Allocation")]
    public int StackAllocation()
    {
        Span<byte> buffer = stackalloc byte[BufferSize];
        return buffer.Length;
    }

    [Benchmark(Description = "Buffer Write")]
    public void BufferWrite()
    {
        for (int i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = (byte)(i % 256);
        }
    }

    [Benchmark(Description = "Buffer Read")]
    public long BufferRead()
    {
        long sum = 0;
        for (int i = 0; i < _buffer.Length; i++)
        {
            sum += _buffer[i];
        }
        return sum;
    }
}
