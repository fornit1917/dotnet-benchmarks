using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace MyBenchmarks;

[MemoryDiagnoser]
public class MethodCallBenchmark
{
    private readonly SumService _sumService;
    private readonly object _sumSeviceObject;
    private readonly dynamic _sumSeviceDynamic;
    private readonly MethodInfo _cachedMethodInfo;

    public MethodCallBenchmark()
    {
        _sumService = new SumService();
        _sumSeviceObject = _sumService;
        _sumSeviceDynamic = _sumService;
        _cachedMethodInfo = _sumSeviceObject.GetType().GetMethod("Sum", [typeof(long), typeof(long)]);
    }

    [Benchmark]
    public void CompileTime()
    {
        var sum = _sumService.Sum(10, 20);
    }

    [Benchmark]
    public void Reflection()
    {
        var methodInfo = _sumSeviceObject.GetType().GetMethod("Sum", [typeof(long), typeof(long)]);
        long x = 10;
        long y = 20;
        var sum = (long)methodInfo.Invoke(_sumSeviceObject, [x, y]);
    }

    [Benchmark]
    public void Reflection_CachedMethodInfo()
    {
        long x = 10;
        long y = 20;
        var sum = (long)_cachedMethodInfo.Invoke(_sumSeviceObject, [x, y]);
    }

    [Benchmark]
    public void Dynamic()
    {
        long x = 10;
        long y = 20;
        var sum = (long)_sumSeviceDynamic.Sum(x, y);
    }
}

public class SumService()
{
    public long Sum(long x, long y)
    {
        return x + y;
    }
}

public class WrappedLong
{
    public long Value { get; set; }
}
