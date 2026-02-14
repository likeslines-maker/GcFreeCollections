GcFreeCollections — Commercial GC‑Free Collections & Fused Query Pipeline for .NET / Unity

GcFreeCollections is a commercial high‑performance collections library for .NET and Unity focused on:

Zero / near‑zero allocations in hot paths (after warmup)
Predictable latency (fewer GC spikes)
Familiar APIs (List,Dictionary,HashSet) and a LINQ‑like pipeline
Cache‑friendly layouts,minimal overhead,strong real‑time ergonomics

Website:https://principium.pro
GitHub:https://github.com/likeslines-maker/GcFreeCollections
Email:vipvodu@yandex.ru
Telegram:@vipvodu

---

Why this exists

In games,VR/AR,audio,and low‑latency services the biggest problem is often not average throughput,but rare GC pauses that cause:

frame hitches in Unity
tail latency spikes (p99 / p999)
unpredictable “micro freezes”

GcFreeCollections reduces allocation pressure and provides hot‑first memory behavior (pooling + retained buffers + time‑sliced cleanup) while keeping a familiar developer experience.

---

Key features

1) Pooled,GC‑free collections (after warmup)
PooledList<T> — List<T> replacement
PooledDictionary<TKey,TValue> — Dictionary<TKey,TValue> replacement
PooledHashSet<T> — HashSet<T> replacement
PooledQueue<T>,PooledStack<T>,PooledPriorityQueue<T,TPriority>
PooledArray<T>,PooledBuffer<T>
PooledMemoryStream
PooledMultiMap<TKey,TValue>

2) Hot‑first Clear + Reference Quarantine
Clear() is O(1) in hot paths (no linear Array.Clear),and leftover reference cleanup happens gradually via:

PooledGlobals.Maintain() — call once per frame / tick

3) Borrowed views (no-copy) + epoch safety (Debug)
AsSpan(),AsReadOnlySpan()
ReadOnly / ReadOnlySlice
LeaseSpan() (SpanLease) — helps catch use‑after‑return / lifetime issues in DEBUG

4) LINQ‑like pipeline without GC (and faster than LINQ)
.Where(...).Select(...).Take(...).ToPooledList()
Fused fast path for PooledList<T> (tight loop style behavior)

5) Profiling & Debugging
Leak tracking (DEBUG):PooledGlobals.LogLeaks(),PooledGlobals.AssertClosedPool()
HotPath allocation guard (DEBUG / net8):using var hot = HotPath.Enter();

---

Installation



```bash
dotnet add package GcFreeCollections
```

Source
Clone and reference the project:
https://github.com/likeslines-maker/GcFreeCollections

---

Quick start

PooledList<T> (List<T> replacement)
```csharp
using GcFreeCollections;

using var list = PooledList<int>.Create();
list.Add(42);
list.Add(100);

foreach (var x in list) // struct enumerator,no GC
{
 // ...
}

list.Clear(); // hot-first:O(1)
PooledGlobals.Maintain(); // call at end of frame/tick
```

Span for maximum performance
```csharp
using var list = PooledList<float>.Create(1024);

// Direct span
Span<float> s = list.AsSpan();

// Borrowed span (Debug epoch checks)
using var lease = list.LeaseSpan();
lease.Span[0] = 1f;
```

PooledDictionary<TKey,TValue>
```csharp
using var dict = PooledDictionary<string,int>.Create();
dict["health"] = 100;
dict.Add("mana",50);

if (dict.TryGetValue("health",out var hp))
{
 // ...
}

foreach (var kv in dict)
{
 // kv.Key / kv.Value
}
```

PooledHashSet<T>
```csharp
using var set = PooledHashSet<int>.Create();
set.Add(42);

if (set.Contains(42))
{
 // ...
}
```

LINQ-like pipeline (no GC)
```csharp
using var list = PooledList<int>.Create(10_000);
// ... fill

using var result = list
 .Where(x => x > 10)
 .Select(x => x * 2)
 .Take(256)
 .ToPooledList(capacityHint:256);
```

Strings without allocation storms:PooledStringBuilder (ref struct)
```csharp
// pooled buffer variant (convenient with using)
using var sb = PooledStringBuilder.CreatePooled(128);
sb.Append("Player ");
sb.Append(123);
sb.Append(" HP");
string text = sb.ToString(); // only allocation is the final string
```

PooledMemoryStream
```csharp
using var ms = PooledMemoryStream.Create();
ms.Write(Encoding.UTF8.GetBytes("hello"));
ms.Position = 0;
// ...
```

---

Recommended Unity / real-time pattern

At end of frame (or end of tick):
```csharp
PooledGlobals.Maintain(); // time-sliced cleanup of reference debt
PooledGlobals.AssertClosedPool(); // DEBUG:ensure all pooled objects were returned
```

---

Benchmarks (real results)

Machine:Windows 11,i5‑11400F,.NET 8.0.24,BenchmarkDotNet 0.15.8
Parameters:N = 1000 and N = 10000.

Speedup = baseline / library (higher is better)
Alloc gain = baseline allocated / library allocated (higher is better)

List<int> — Add + iterate
| N | Baseline | Mean | Alloc | Library | Mean | Alloc | Speedup | Alloc gain |
|---:|---|---:|---:|---|---:|---:|---:|---:|
| 1000 | List<int> | 1665.48 ns | 4056 B | PooledList<int> | 1502.93 ns | 56 B | 1.11× | 72× |
| 10000 | List<int> | 16311.98 ns | 40056 B | PooledList<int> | 14318.61 ns | 56 B | 1.14× | 715× |

LINQ — Where/Select/Take/ToList
| N | Baseline | Mean | Alloc | Library | Mean | Alloc | Speedup | Alloc gain |
|---:|---|---:|---:|---|---:|---:|---:|---:|
| 1000 | LINQ | 2366.75 ns | 6496 B | PooledQuery (fused) | 1844.75 ns | 112 B | 1.28× | 58× |
| 10000 | LINQ | 12521.78 ns | 42496 B | PooledQuery (fused) | 10725.60 ns | 112 B | 1.17× | 379× |

HashSet<int> — Add + Contains
| N | Baseline | Mean | Alloc | Library | Mean | Alloc | Speedup | Alloc gain |
|---:|---|---:|---:|---|---:|---:|---:|---:|
| 1000 | HashSet<int> | 12518.11 ns | 58664 B | PooledSwissHashSet<int> | 6377.21 ns | 72 B | 1.96× | 815× |
| 10000 | HashSet<int> | 177439.05 ns | 538656 B | PooledSwissHashSet<int> | 55426.60 ns | 72 B | 3.20× | 7481× |

Dictionary<string,int> — Add + TryGetValue hits
| N | Baseline | Mean | Alloc | Library | Mean | Alloc | Speedup | Alloc gain |
|---:|---|---:|---:|---|---:|---:|---:|---:|
| 1000 | Dictionary | 21292.64 ns | 31016 B | PooledDictionary | 25406.97 ns | 88 B | 0.84× | 352× |
| 10000 | Dictionary | 348706.07 ns | 283042 B | PooledDictionary | 310080.51 ns | 88 B | 1.12× | 3216× |

Strings — build string
| N | Baseline | Mean | Alloc | Library | Mean | Alloc | Speedup | Alloc gain |
|---:|---|---:|---:|---|---:|---:|---:|---:|
| 1000 | StringBuilder | 36.92 ns | 408 B | PooledStringBuilder | 49.33 ns | 80 B | 0.75× | 5.1× |

Note: PooledStringBuilder currently reduces allocations significantly,but is not yet faster than StringBuilder for very small strings. This is expected for pool/guarded designs and is being optimized over time.

---

Commercial license & pricing

GcFreeCollections is a commercial library.

Pricing (popular,affordable)
Indie — $49 / year
 1 developer seat,1 commercial project.
Pro — $199 / year
 Up to 3 seats,multiple projects.
Studio — $499 / year
 Up to 10 seats.
Enterprise — contact us
 SLA,priority support,custom features.

To purchase / request invoice:
Email:vipvodu@yandex.ru
Telegram:@vipvodu

---

Obfuscation notes (for NuGet packaging)

Obfuscation usually works fine if you follow these rules:
Prefer rename-only / “mild” obfuscation. Avoid heavy control-flow virtualization.
Keep public API names stable (or minimally renamed). Breaking public symbols makes upgrades painful.
Avoid obfuscation that changes method bodies too aggressively (can reduce inlining/perf).
Smoke-test after obfuscation:
 - run benchmarks
 - run a small Unity test scene (if targeting Unity)

Leak logs in DEBUG may show obfuscated type names; you can exclude some types from renaming if you want readable diagnostics.

---

Roadmap
Faster Dictionary variant for small sizes (Swiss/Robin-Hood refinement)
Roslyn analyzer:compile-time guard against allocations in hot paths
Unity Jobs handoff fences (RO/RW safety)
SIMD operators for bulk operations

---

License
See LICENSE.txt.
