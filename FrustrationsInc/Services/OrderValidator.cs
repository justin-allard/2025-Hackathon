using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

public static class OrderValidator
{
    public static (List<Order> validOrders, List<string> invalidOrders, Dictionary<string, int> ruleFailures)
        ValidateOrders(List<Order> orders, SqlConnection connection)
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

        foreach (var order in orders)
        {
            bool isValid = true;
            var reasons = new List<string>();

            if (order.Currency != "USD" && order.Currency != "EUR")
            {
                ruleFailures["Invalid Currency"]++;
                isValid = false;
                reasons.Add($"Invalid Currency ({order.Currency})");
            }

            if (order.OrderAmount <= 0)
            {
                ruleFailures["Invalid Amount"]++;
                isValid = false;
                reasons.Add($"Invalid Amount ({order.OrderAmount})");
            }

            if (!DateTime.TryParseExact(order.OrderDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
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
            var customerIds = new HashSet<string>();
            using var cmd = new SqlCommand("SELECT CustomerId FROM Customers", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                customerIds.Add(reader.GetString(0));
            }

            // Then check in-memory
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
