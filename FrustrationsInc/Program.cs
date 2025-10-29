using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {

        var stopwatch = Stopwatch.StartNew(); //Start Timing

        string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Frustrations;Trusted_Connection=True;";
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // Cache all customer IDs
        var customerIds = new HashSet<string>();
        using (var cmd = new SqlCommand("SELECT CustomerId FROM Customers", connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                customerIds.Add(reader.GetString(0));
        }

       // var (validOrders, invalidOrders, ruleFailures) = await CsvLoader.LoadAndValidateOrdersAsync("orders_1000.csv", connection, customerIds);
       // var (validOrders, invalidOrders, ruleFailures) = await CsvLoader.LoadAndValidateOrdersAsync("orders_10K.csv", connection, customerIds);
        var (validOrders, invalidOrders, ruleFailures) = await CsvLoader.LoadAndValidateOrdersAsync("orders_1M.csv", connection, customerIds);

        Console.WriteLine($"Valid records to send: {validOrders.Count}");
        Console.WriteLine($"Invalid records skipped: {invalidOrders.Count}");

        // Send batches in parallel
        await OrderSender.SendBatchesAsync(validOrders, batchSize: 50000, maxConcurrency: 5);

        // Final report
        Console.WriteLine("Final Report:");
        Console.WriteLine($"Total records processed: {validOrders.Count + invalidOrders.Count}");
        Console.WriteLine($"Valid records sent to API: {validOrders.Count}");
        Console.WriteLine($"Invalid records skipped: {invalidOrders.Count}");
        foreach (var rule in ruleFailures)
            Console.WriteLine($"{rule.Key}: {rule.Value}");

        stopwatch.Stop(); // Stop timing
        Console.WriteLine($"Total elapsed time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}
