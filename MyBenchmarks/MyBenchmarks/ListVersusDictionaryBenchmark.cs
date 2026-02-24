using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;

namespace MyBenchmarks;

[MemoryDiagnoser]
public class ListVersusDictionaryBenchmark
{
    private Dictionary<string, string> _dictionary;
    private List<string> _list;
    
    [Params(2, 4, 10, 100)]
    public int Count { get; set; }
    
    [Params(false, true)]
    public bool Existing { get; set; }


    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, string>();
        _dictionary.ToFrozenDictionary();
        _list = new List<string>();
        for (int i = 0; i < Count; i++)
        {
            var str = Guid.NewGuid().ToString();
            _dictionary.Add(str, str);
            _list.Add(str);
        }
    }

    [Benchmark]
    [IterationCount(10)]
    public void SearchInDictionary()
    {
        for (int op = 0; op < 1; op++)
        {
            var valueForSearch = Existing ? _list[Count / 2] : Guid.NewGuid().ToString();
            _dictionary.TryGetValue(valueForSearch, out var str);   
        }
    }

    [Benchmark]
    [IterationCount(10)]
    public void SearchInList()
    {
        for (int op = 0; op < 1; op++)
        {
            var valueForSearch = Existing ? _list[Count / 2] : Guid.NewGuid().ToString();
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i] == valueForSearch)
                {
                    break;
                }
            }            
        }
    }
    
}