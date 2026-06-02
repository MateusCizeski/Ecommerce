namespace Ecommerce.Domain;

public class Order : TenantEntity
{
    public Guid CustomerId { get; private set; }
    public Guid? CouponId { get; private set; }
    public Guid ShippingAddressId { get; private set; }
    public string OrderNumber { get; private set; } = default!;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime PlacedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<Payment> _payments = [];
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    protected Order() { }

    public static Order Create(
        Guid tenantId,
        Guid customerId,
        Guid shippingAddressId,
        string orderNumber,
        IEnumerable<OrderItem> items,
        decimal shippingAmount,
        decimal taxAmount,
        decimal discountAmount = 0,
        Guid? couponId = null,
        string? notes = null)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            throw new DomainException("Order must have at least one item.");

        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Order number is required.");

        if (shippingAmount < 0) throw new DomainException("Shipping amount cannot be negative.");
        if (taxAmount < 0) throw new DomainException("Tax amount cannot be negative.");
        if (discountAmount < 0) throw new DomainException("Discount amount cannot be negative.");

        var subtotal = itemList.Sum(i => i.TotalPrice);
        var totalAmount = subtotal + shippingAmount + taxAmount - discountAmount;
        if (totalAmount < 0) throw new DomainException("Total amount cannot be negative.");

        var order = new Order
        {
            TenantId = tenantId,
            CustomerId = customerId,
            ShippingAddressId = shippingAddressId,
            OrderNumber = orderNumber.Trim(),
            Subtotal = subtotal,
            ShippingAmount = shippingAmount,
            TaxAmount = taxAmount,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            CouponId = couponId,
            Notes = notes?.Trim()
        };

        foreach (var item in itemList) order._items.Add(item);

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, tenantId, customerId, order.TotalAmount));
        return order;
    }

    public void ConfirmPayment()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be confirmed.");

        Status = OrderStatus.Processing;
        MarkUpdated();
        AddDomainEvent(new OrderConfirmedEvent(Id, TenantId, CustomerId));
    }

    public void Ship()
    {
        if (Status != OrderStatus.Processing)
            throw new DomainException("Only processing orders can be shipped.");

        Status = OrderStatus.Shipped;
        MarkUpdated();
        AddDomainEvent(new OrderShippedEvent(Id, TenantId));
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainException("Only shipped orders can be delivered.");

        Status = OrderStatus.Delivered;
        MarkUpdated();
        AddDomainEvent(new OrderDeliveredEvent(Id, TenantId));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new DomainException("Cannot cancel a shipped or delivered order.");

        var wasProcessing = Status == OrderStatus.Processing;

        Status = OrderStatus.Cancelled;
        Notes = reason?.Trim();
        MarkUpdated();

        AddDomainEvent(new OrderCancelledEvent(Id, TenantId, CustomerId));

        if (wasProcessing)
        {
            var cancelledItems = _items
                .Select(i => new OrderCancelledItem(i.ProductVariantId, i.Quantity))
                .ToList()
                .AsReadOnly();

            AddDomainEvent(new OrderCancelledWithItemsEvent(Id, TenantId, cancelledItems));
        }
    }

    public Payment AddPayment(PaymentMethod method, decimal amount, string currency = "USD")
    {
        var payment = Payment.Create(Id, method, amount, currency);
        _payments.Add(payment);
        return payment;
    }

    public bool IsFullyPaid() =>
        _payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .Sum(p => p.Amount) >= TotalAmount;

    public IReadOnlyCollection<OrderItem> GetItemsForStockDeduction() =>
        _items.AsReadOnly();
}

