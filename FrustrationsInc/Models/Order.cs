using System;
using System.Collections.Generic;

public class Order
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public string OrderDate { get; set; } // raw CSV value
    public decimal OrderAmount { get; set; }
    public string Currency { get; set; }

    public DateTime OrderDateParsed { get; set; } // parsed date after validation
}

public class OrderPayload
{
    public List<Order> Orders { get; set; }
}
