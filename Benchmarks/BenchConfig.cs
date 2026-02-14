using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;

namespace Benchmarks;

public sealed class BenchConfig : ManualConfig
{
    public BenchConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(CsvExporter.Default);

        AddLogger(ConsoleLogger.Default);

        AddColumn(TargetMethodColumn.Method);
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);

        AddColumn(CategoriesColumn.Default);

        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical));
        WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(50));
    }
}
