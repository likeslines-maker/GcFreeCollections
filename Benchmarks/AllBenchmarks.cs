using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GcFreeCollections;

namespace Benchmarks;

[Config(typeof(BenchConfig))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
public class AllBenchmarks
{
    [Params(1_000, 10_000)]
    public int N;

    private int[] _data = Array.Empty<int>();
    private string[] _keys = Array.Empty<string>();

    [GlobalSetup]
    public void Setup()
    {
        _data = new int[N];
        for (int i = 0; i < N; i++) _data[i] = i;

        _keys = new string[N];
        for (int i = 0; i < N; i++) _keys[i] = "k" + i;

        // Режим "hot-first"
        PooledGlobals.KeepBuffersOnPooledObjects = true;
        PooledGlobals.MaxRetainedBufferLength = 4096;
        PooledGlobals.AutoMaintain = false;

        WarmupPools();
    }

    private static void WarmupPools()
    {
        using (var l = PooledList<int>.Create(16)) { l.Add(1); }

        using (var d = PooledDictionary<string, int>.Create(16)) { d["a"] = 1; }
        using (var sd = PooledSwissDictionary<string, int>.Create(16)) { sd["a"] = 1; }

        using (var s = PooledHashSet<int>.Create(16)) { s.Add(1); }
        using (var ss = PooledSwissHashSet<int>.Create(16)) { ss.Add(1); }

        // прогрев string path
        {
            using var sb = PooledStringBuilder.CreatePooled(64);
            sb.Append("x");
            _ = sb.ToString();
        }
    }

    // -------------------- LIST --------------------

    [BenchmarkCategory("List")]
    [Benchmark(Description = "List<int>:Add + foreach sum")]
    public int List_Add_Iterate()
    {
        var list = new List<int>(N);
        for (int i = 0; i < N; i++) list.Add(_data[i]);

        int sum = 0;
        foreach (var x in list) sum += x;
        return sum;
    }

    [BenchmarkCategory("List")]
    [Benchmark(Description = "PooledList<int>:Add + foreach sum")]
    public int PooledList_Add_Iterate()
    {
        using var list = PooledList<int>.Create(N);
        for (int i = 0; i < N; i++) list.Add(_data[i]);

        int sum = 0;
        foreach (var x in list) sum += x;

        // обслуживание quarantine (в реальном проекте — раз в кадр)
        PooledGlobals.Maintain();
        return sum;
    }

    [BenchmarkCategory("List")]
    [Benchmark(Description = "List<int>:Add + Sort + sum")]
    public int List_Add_Sort_Iterate()
    {
        var list = new List<int>(N);
        for (int i = 0; i < N; i++) list.Add(N - i);

        list.Sort();

        int sum = 0;
        for (int i = 0; i < list.Count; i++) sum += list[i];
        return sum;
    }

    [BenchmarkCategory("List")]
    [Benchmark(Description = "PooledList<int>:Add + Sort + sum")]
    public int PooledList_Add_Sort_Iterate()
    {
        using var list = PooledList<int>.Create(N);
        for (int i = 0; i < N; i++) list.Add(N - i);

        list.Sort();

        int sum = 0;
        for (int i = 0; i < list.Count; i++) sum += list[i];

        PooledGlobals.Maintain();
        return sum;
    }

    // -------------------- DICTIONARY --------------------

    [BenchmarkCategory("Dictionary")]
    [Benchmark(Description = "Dictionary<string,int>:Add + TryGetValue hits")]
    public int Dictionary_Add_Lookup()
    {
        var dict = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++) dict[_keys[i]] = _data[i];

        int acc = 0;
        for (int i = 0; i < N; i++)
        {
            if (dict.TryGetValue(_keys[i], out var v))
                acc += v;
        }

        return acc;
    }

    [BenchmarkCategory("Dictionary")]
    [Benchmark(Description = "PooledDictionary<string,int>:Add + TryGetValue hits")]
    public int PooledDictionary_Add_Lookup()
    {
        using var dict = PooledDictionary<string, int>.Create(N);
        for (int i = 0; i < N; i++) dict[_keys[i]] = _data[i];

        int acc = 0;
        for (int i = 0; i < N; i++)
        {
            if (dict.TryGetValue(_keys[i], out var v))
                acc += v;
        }

        PooledGlobals.Maintain();
        return acc;
    }

    [BenchmarkCategory("Dictionary")]
    [Benchmark(Description = "PooledSwissDictionary<string,int>:Add + TryGetValue hits")]
    public int PooledSwissDictionary_Add_Lookup()
    {
        using var dict = PooledSwissDictionary<string, int>.Create(N);
        for (int i = 0; i < N; i++) dict[_keys[i]] = _data[i];

        int acc = 0;
        for (int i = 0; i < N; i++)
        {
            if (dict.TryGetValue(_keys[i], out var v))
                acc += v;
        }

        PooledGlobals.Maintain();
        return acc;
    }

    [BenchmarkCategory("Dictionary")]
    [Benchmark(Description = "Dictionary<string,int>:iterate pairs sum")]
    public int Dictionary_Iterate()
    {
        var dict = new Dictionary<string, int>(N);
        for (int i = 0; i < N; i++) dict[_keys[i]] = _data[i];

        int sum = 0;
        foreach (var kv in dict)
            sum += kv.Value;

        return sum;
    }

    [BenchmarkCategory("Dictionary")]
    [Benchmark(Description = "PooledDictionary<string,int>:iterate pairs sum")]
    public int PooledDictionary_Iterate()
    {
        using var dict = PooledDictionary<string, int>.Create(N);
        for (int i = 0; i < N; i++) dict[_keys[i]] = _data[i];

        int sum = 0;
        foreach (var kv in dict)
            sum += kv.Value;

        PooledGlobals.Maintain();
        return sum;
    }

    [BenchmarkCategory("Dictionary")]
    [Benchmark(Description = "PooledSwissDictionary<string,int>:iterate pairs sum")]
    public int PooledSwissDictionary_Iterate()
    {
        using var dict = PooledSwissDictionary<string, int>.Create(N);
        for (int i = 0; i < N; i++) dict[_keys[i]] = _data[i];

        int sum = 0;
        foreach (var kv in dict)
            sum += kv.Value;

        PooledGlobals.Maintain();
        return sum;
    }

    // -------------------- HASHSET --------------------

    [BenchmarkCategory("HashSet")]
    [Benchmark(Description = "HashSet<int>:Add + Contains hits")]
    public int HashSet_Add_Contains()
    {
        var set = new HashSet<int>();
        for (int i = 0; i < N; i++) set.Add(_data[i]);

        int acc = 0;
        for (int i = 0; i < N; i++)
            if (set.Contains(_data[i]))
                acc++;

        return acc;
    }

    [BenchmarkCategory("HashSet")]
    [Benchmark(Description = "PooledHashSet<int>:Add + Contains hits")]
    public int PooledHashSet_Add_Contains()
    {
        using var set = PooledHashSet<int>.Create(N);
        for (int i = 0; i < N; i++) set.Add(_data[i]);

        int acc = 0;
        for (int i = 0; i < N; i++)
            if (set.Contains(_data[i]))
                acc++;

        PooledGlobals.Maintain();
        return acc;
    }

    [BenchmarkCategory("HashSet")]
    [Benchmark(Description = "PooledSwissHashSet<int>:Add + Contains hits")]
    public int PooledSwissHashSet_Add_Contains()
    {
        using var set = PooledSwissHashSet<int>.Create(N);
        for (int i = 0; i < N; i++) set.Add(_data[i]);

        int acc = 0;
        for (int i = 0; i < N; i++)
            if (set.Contains(_data[i]))
                acc++;

        PooledGlobals.Maintain();
        return acc;
    }

    // -------------------- LINQ --------------------

    [BenchmarkCategory("LINQ")]
    [Benchmark(Description = "LINQ:Where/Select/Take/ToList")]
    public int Linq_Where_Select_Take_ToList()
    {
        var list = new List<int>(N);
        for (int i = 0; i < N; i++) list.Add(_data[i]);

        var res = list
        .Where(x => x > 10)
        .Select(x => x * 2)
        .Take(256)
        .ToList();

        int sum = 0;
        for (int i = 0; i < res.Count; i++) sum += res[i];
        return sum;
    }

    [BenchmarkCategory("LINQ")]
    [Benchmark(Description = "PooledQuery(fused):Where/Select/Take/ToPooledList")]
    public int PooledQuery_Where_Select_Take_ToPooledList()
    {
        using var list = PooledList<int>.Create(N);
        for (int i = 0; i < N; i++) list.Add(_data[i]);

        using var res = list
        .Where(x => x > 10)
        .Select(x => x * 2)
        .Take(256)
        .ToPooledList(capacityHint: 256);

        int sum = 0;
        foreach (var x in res) sum += x;

        PooledGlobals.Maintain();
        return sum;
    }

    // -------------------- STRING --------------------

    [BenchmarkCategory("String")]
    [Benchmark(Description = "StringBuilder:build string")]
    public string StringBuilder_Build()
    {
        var sb = new StringBuilder(capacity: 128);
        sb.Append("Player ");
        sb.Append(12345);
        sb.Append(" HP / ");
        sb.Append(99999);
        sb.Append(" Max");
        return sb.ToString();
    }

    [BenchmarkCategory("String")]
    [Benchmark(Description = "PooledStringBuilder(ref):build string")]
    public string PooledStringBuilder_Build()
    {
        using var sb = PooledStringBuilder.CreatePooled(128);
        sb.Append("Player ");
        sb.Append(12345);
        sb.Append(" HP / ");
        sb.Append(99999);
        sb.Append(" Max");
        return sb.ToString();
    }
}