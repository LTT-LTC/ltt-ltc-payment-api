using System;
using System.Collections.Generic;

namespace LTC.PaymentService.Services.Kafka;

public class BookingRequestedEvent
{
    public Guid BookingId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public Guid ShowtimeId { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public List<BookingRequestedItemEvent> Items { get; set; } = [];
}

public class BookingRequestedItemEvent
{
    public string? ItemType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid? VariantId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
