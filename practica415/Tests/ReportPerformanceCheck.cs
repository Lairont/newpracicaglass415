using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using practica415.Data;
using practica415.Models;
using practica415.Services;
using Xunit;
using Xunit.Abstractions;

namespace practica415.Tests;

public sealed class ReportPerformanceCheck(ITestOutputHelper output)
{
    [Trait("Category", "Performance")]
    [Fact]
    public void CompareReportDataLoadingAgainstConnectedDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("practica415-fitstock-dev-2026")
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Assert.False(string.IsNullOrWhiteSpace(connectionString), "The local PostgreSQL connection string is not configured.");

        var options = new DbContextOptionsBuilder<WarehouseDbContext>().UseNpgsql(connectionString).Options;
        using var db = new WarehouseDbContext(options);
        var accounts = new EfAccountService(db, configuration);
        var warehouse = new EfWarehouseService(db);
        var from = DateTime.SpecifyKind(DateTime.Today.AddDays(-29), DateTimeKind.Unspecified);
        var to = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        (int Clients, int Active, int Expiring, int Debtors, decimal StockValue, int MovementCount, int ProductCount) LoadExisting()
        {
            var clients = accounts.GetClients();
            var products = warehouse.GetProducts();
            var movements = warehouse.GetMovements(from, to);
            _ = movements.Where(x => x.Type == MovementType.Receipt).Sum(x => x.Quantity);
            _ = movements.Where(x => x.Type == MovementType.Shipment).Sum(x => x.Quantity);
            _ = movements.Where(x => x.Type == MovementType.WriteOff).Sum(x => x.Quantity);
            _ = movements.Count(x => x.Type == MovementType.Receipt);
            _ = movements.Count(x => x.Type == MovementType.Shipment);
            _ = movements.Count(x => x.Type == MovementType.WriteOff);
            return (
                clients.Count,
                clients.Count(x => x.MembershipEnd >= DateTime.Today),
                clients.Count(x => x.MembershipEnd >= DateTime.Today && x.MembershipEnd <= DateTime.Today.AddDays(1)),
                clients.Count(x => x.MembershipEnd < DateTime.Today),
                products.Sum(x => x.Quantity * x.UnitPrice),
                movements.Count,
                products.Count);
        }

        (int Clients, int Active, int Expiring, int Debtors, decimal StockValue, int MovementCount, int ProductCount) LoadOptimized()
        {
            var clients = accounts.GetClientMembershipSummary();
            var products = warehouse.GetReportProducts();
            var movements = warehouse.GetReportMovements(from, to);
            return (
                clients.ClientCount,
                clients.ActiveMemberships,
                clients.ExpiringMemberships,
                clients.DebtorCount,
                products.Sum(x => x.Quantity * x.UnitPrice),
                movements.Count,
                products.Count);
        }

        var existingFirst = Measure(LoadExisting);
        var optimizedFirst = Measure(LoadOptimized);
        Assert.Equal(existingFirst.Result, optimizedFirst.Result);

        var existing = new List<(double Milliseconds, long Bytes)>();
        var optimized = new List<(double Milliseconds, long Bytes)>();
        for (var i = 0; i < 9; i++)
        {
            if (i % 2 == 0)
            {
                var existingSample = Measure(LoadExisting);
                var optimizedSample = Measure(LoadOptimized);
                existing.Add((existingSample.Milliseconds, existingSample.Bytes));
                optimized.Add((optimizedSample.Milliseconds, optimizedSample.Bytes));
            }
            else
            {
                var optimizedSample = Measure(LoadOptimized);
                var existingSample = Measure(LoadExisting);
                optimized.Add((optimizedSample.Milliseconds, optimizedSample.Bytes));
                existing.Add((existingSample.Milliseconds, existingSample.Bytes));
            }
        }

        output.WriteLine($"Existing report data: median {Median(existing.Select(x => x.Milliseconds)) :F3} ms, allocated {Median(existing.Select(x => (double)x.Bytes)):F0} bytes");
        output.WriteLine($"Optimized report data: median {Median(optimized.Select(x => x.Milliseconds)):F3} ms, allocated {Median(optimized.Select(x => (double)x.Bytes)):F0} bytes");
        output.WriteLine($"Database rows used: {existingFirst.Result.Clients} clients, {existingFirst.Result.ProductCount} products, {existingFirst.Result.MovementCount} movements in the selected period");
    }

    private static (T Result, double Milliseconds, long Bytes) Measure<T>(Func<T> action)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        var result = action();
        stopwatch.Stop();
        return (result, stopwatch.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return ordered[ordered.Length / 2];
    }
}
