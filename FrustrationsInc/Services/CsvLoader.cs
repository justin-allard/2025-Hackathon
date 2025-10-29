using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;

public static class CsvLoader
{
    public static async Task<(List<Order> validOrders, List<string> invalidOrders, Dictionary<string, int> ruleFailures)>
        LoadAndValidateOrdersAsync(string filePath, SqlConnection connection, HashSet<string> customerIds)
    {
        var validOrders = new List<Order>();
        var invalidOrders = new List<string>();
        var ruleFailures = new Dictionary<string, int>
        {
            ["Invalid Currency"] = 0,
            ["Invalid Amount"] = 0,
            ["Invalid Date"] = 0,
            ["Customer Not Found"] = 0
        };

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            ReadingExceptionOccurred = args =>
            {
                Console.WriteLine($"Skipping row: {args.Exception.Message}");
                return false; // skip row
            }
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        await foreach (var order in csv.GetRecordsAsync<Order>())
        {
            bool isValid = true;
            var reasons = new List<string>();

            // Currency
            if (order.Currency != "USD" && order.Currency != "EUR")
            {
                ruleFailures["Invalid Currency"]++;
                isValid = false;
                reasons.Add($"Invalid Currency ({order.Currency})");
            }

            // Amount
            if (order.OrderAmount <= 0)
            {
                ruleFailures["Invalid Amount"]++;
                isValid = false;
                reasons.Add($"Invalid Amount ({order.OrderAmount})");
            }

            // Date
            if (!DateTime.TryParseExact(order.OrderDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                ruleFailures["Invalid Date"]++;
                isValid = false;
                reasons.Add($"Invalid Date ({order.OrderDate})");
            }
            else if (parsedDate > DateTime.UtcNow)
            {
                ruleFailures["Invalid Date"]++;
                isValid = false;
                reasons.Add($"Date in Future ({order.OrderDate})");
            }
            else
            {
                order.OrderDateParsed = parsedDate;
            }

            // Customer
            if (!customerIds.Contains(order.CustomerId))
            {
                ruleFailures["Customer Not Found"]++;
                isValid = false;
                reasons.Add("Customer Not Found");
            }

            if (isValid)
                validOrders.Add(order);
            else
                invalidOrders.Add($"OrderId {order.OrderId}: {string.Join(", ", reasons)}");
        }

        return (validOrders, invalidOrders, ruleFailures);
    }
}
