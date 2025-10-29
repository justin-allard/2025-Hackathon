using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public static class OrderSender
{
    public static async Task SendBatchesAsync(List<Order> orders, int batchSize, int maxConcurrency = 5)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };


        // Split into batches
        var batches = orders
            .Select((order, index) => new { order, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.order).ToList())
            .ToList();

        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = batches.Select(async (batch, batchIndex) =>
        {
            await semaphore.WaitAsync();
            try
            {
                var payload = new OrderPayload { Orders = batch };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    "https://hackathondatatester.azurewebsites.net/api/orders/intake?code=sl6R6uywGifJPrCmkpdemXGwVa1UvQolENj6yZuIJ1foAzFu4XwW_w==",
                    content);

                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"Batch {batchIndex + 1}/{batches.Count} sent ({batch.Count} orders).");
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API error in batch {batchIndex + 1}: {response.StatusCode} - {error}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
    }
}
