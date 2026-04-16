namespace Ecommerce.Domain;

public class Cart : TenantEntity
{
  public Guid CustomerId { get; private set; }
  public CartStatus Status { get; private set; } = CartStatus.Active;
  public DateTime? ExpiresAt { get; private set; }

  private readonly List<CartItem> _items = [];
  public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

  public decimal Total => _items.Sum(i => i.UnitPrice * i.Quantity);
  public int ItemCount => _items.Sum(i => i.Quantity);

  protected Cart() { }

  public static Cart Create(Guid tenantId, Guid customerId, int expirationHours = 24) => new()
  {
    TenantId = tenantId,
    CustomerId = customerId,
    ExpiresAt = DateTime.UtcNow.AddHours(expirationHours)
  };

  public CartItem AddItem(ProductVariant variant, int quantity)
  {
    if (Status != CartStatus.Active) throw new DomainException("Cannot modify an inactive cart.");
    if (quantity <= 0) throw new DomainException("Quantity must be positive.");
    if (!variant.HasStock(quantity)) throw new DomainException($"Insufficient stock for '{variant.Name}'.");

    var existing = _items.FirstOrDefault(i => i.ProductVariantId == variant.Id);
    if (existing is not null) { existing.UpdateQuantity(existing.Quantity + quantity); MarkUpdated(); return existing; }

    var item = CartItem.Create(Id, variant.Id, quantity, variant.Price);
    _items.Add(item);
    MarkUpdated();
    return item;
  }

  public void UpdateItemQuantity(Guid variantId, int quantity)
  {
    if (Status != CartStatus.Active) throw new DomainException("Cannot modify an inactive cart.");
    var item = _items.FirstOrDefault(i => i.ProductVariantId == variantId)
        ?? throw new DomainException("Item not found in cart.");
    if (quantity <= 0) _items.Remove(item); else item.UpdateQuantity(quantity);
    MarkUpdated();
  }

  public void RemoveItem(Guid variantId)
  {
    var item = _items.FirstOrDefault(i => i.ProductVariantId == variantId)
        ?? throw new DomainException("Item not found in cart.");
    _items.Remove(item);
    MarkUpdated();
  }

  public void Checkout() => Status = CartStatus.CheckedOut;
  public void Abandon() => Status = CartStatus.Abandoned;
  public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;
}

public class CartItem : BaseEntity
{
  public Guid CartId { get; private set; }
  public Guid ProductVariantId { get; private set; }
  public int Quantity { get; private set; }
  public decimal UnitPrice { get; private set; }
  public decimal LineTotal => UnitPrice * Quantity;

  protected CartItem() { }

  internal static CartItem Create(Guid cartId, Guid variantId, int quantity, decimal unitPrice) => new()
  {
    CartId = cartId,
    ProductVariantId = variantId,
    Quantity = quantity,
    UnitPrice = unitPrice
  };

  internal void UpdateQuantity(int quantity) => Quantity = quantity;
}

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

  public static Order Create(Guid tenantId, Guid customerId, Guid shippingAddressId, string orderNumber,
      IEnumerable<OrderItem> items, decimal shippingAmount, decimal taxAmount,
      decimal discountAmount = 0, Guid? couponId = null, string? notes = null)
  {
    var itemList = items.ToList();
    if (!itemList.Any()) throw new DomainException("Order must have at least one item.");

    var subtotal = itemList.Sum(i => i.TotalPrice);
    var order = new Order
    {
      TenantId = tenantId,
      CustomerId = customerId,
      ShippingAddressId = shippingAddressId,
      OrderNumber = orderNumber,
      Subtotal = subtotal,
      ShippingAmount = shippingAmount,
      TaxAmount = taxAmount,
      DiscountAmount = discountAmount,
      TotalAmount = subtotal + shippingAmount + taxAmount - discountAmount,
      CouponId = couponId,
      Notes = notes
    };
    foreach (var item in itemList) order._items.Add(item);
    order.AddDomainEvent(new OrderCreatedEvent(order.Id, tenantId, customerId, order.TotalAmount));
    return order;
  }

  public void ConfirmPayment()
  {
    if (Status != OrderStatus.Pending) throw new DomainException("Only pending orders can be confirmed.");
    Status = OrderStatus.Processing;
    MarkUpdated();
    AddDomainEvent(new OrderConfirmedEvent(Id, TenantId, CustomerId));
  }

  public void Ship()
  {
    if (Status != OrderStatus.Processing) throw new DomainException("Only processing orders can be shipped.");
    Status = OrderStatus.Shipped;
    MarkUpdated();
  }

  public void Deliver()
  {
    if (Status != OrderStatus.Shipped) throw new DomainException("Only shipped orders can be delivered.");
    Status = OrderStatus.Delivered;
    MarkUpdated();
  }

  public void Cancel(string reason)
  {
    if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
      throw new DomainException("Cannot cancel shipped or delivered orders.");
    Status = OrderStatus.Cancelled;
    Notes = reason;
    MarkUpdated();
    AddDomainEvent(new OrderCancelledEvent(Id, TenantId, CustomerId));
  }

  public Payment AddPayment(PaymentMethod method, decimal amount, string currency = "USD")
  {
    var payment = Payment.Create(Id, method, amount, currency);
    _payments.Add(payment);
    return payment;
  }

  public bool IsFullyPaid() =>
      _payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount) >= TotalAmount;
}

public class OrderItem : BaseEntity
{
  public Guid OrderId { get; private set; }
  public Guid ProductVariantId { get; private set; }
  public string SKUSnapshot { get; private set; } = default!;
  public string ProductNameSnapshot { get; private set; } = default!;
  public int Quantity { get; private set; }
  public decimal UnitPrice { get; private set; }
  public decimal TotalPrice { get; private set; }

  protected OrderItem() { }

  public static OrderItem Create(Guid variantId, string sku, string productName, int quantity, decimal unitPrice) => new()
  {
    ProductVariantId = variantId,
    SKUSnapshot = sku,
    ProductNameSnapshot = productName,
    Quantity = quantity,
    UnitPrice = unitPrice,
    TotalPrice = unitPrice * quantity
  };
}

public class Payment : BaseEntity
{
  public Guid OrderId { get; private set; }
  public PaymentMethod Method { get; private set; }
  public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
  public decimal Amount { get; private set; }
  public string Currency { get; private set; } = default!;
  public string? StripePaymentIntentId { get; private set; }
  public string? StripeChargeId { get; private set; }
  public string? GatewayResponse { get; private set; }
  public DateTime? PaidAt { get; private set; }
  public DateTime? RefundedAt { get; private set; }

  protected Payment() { }

  internal static Payment Create(Guid orderId, PaymentMethod method, decimal amount, string currency) => new()
  {
    OrderId = orderId,
    Method = method,
    Amount = amount,
    Currency = currency
  };

  public void MarkSucceeded(string chargeId, string gatewayResponse)
  {
    Status = PaymentStatus.Succeeded; StripeChargeId = chargeId;
    GatewayResponse = gatewayResponse; PaidAt = DateTime.UtcNow;
  }
  public void MarkFailed(string gatewayResponse) { Status = PaymentStatus.Failed; GatewayResponse = gatewayResponse; }
  public void Refund()
  {
    if (Status != PaymentStatus.Succeeded) throw new DomainException("Only succeeded payments can be refunded.");
    Status = PaymentStatus.Refunded; RefundedAt = DateTime.UtcNow;
  }
  public void SetStripePaymentIntentId(string id) => StripePaymentIntentId = id;
}

public class Coupon : TenantEntity
{
  public string Code { get; private set; } = default!;
  public DiscountType DiscountType { get; private set; }
  public decimal DiscountValue { get; private set; }
  public decimal? MinOrderValue { get; private set; }
  public int? MaxUses { get; private set; }
  public int UsedCount { get; private set; }
  public DateTime? ValidFrom { get; private set; }
  public DateTime? ValidUntil { get; private set; }
  public bool IsActive { get; private set; } = true;

  protected Coupon() { }

  public static Coupon Create(Guid tenantId, string code, DiscountType discountType, decimal discountValue,
      decimal? minOrderValue = null, int? maxUses = null, DateTime? validFrom = null, DateTime? validUntil = null)
  {
    if (discountValue <= 0) throw new DomainException("Discount value must be positive.");
    if (discountType == DiscountType.Percentage && discountValue > 100)
      throw new DomainException("Percentage discount cannot exceed 100.");

    return new Coupon
    {
      TenantId = tenantId,
      Code = code.Trim().ToUpperInvariant(),
      DiscountType = discountType,
      DiscountValue = discountValue,
      MinOrderValue = minOrderValue,
      MaxUses = maxUses,
      ValidFrom = validFrom,
      ValidUntil = validUntil
    };
  }

  public bool IsValid(decimal orderTotal)
  {
    if (!IsActive) return false;
    if (ValidFrom.HasValue && DateTime.UtcNow < ValidFrom) return false;
    if (ValidUntil.HasValue && DateTime.UtcNow > ValidUntil) return false;
    if (MaxUses.HasValue && UsedCount >= MaxUses) return false;
    if (MinOrderValue.HasValue && orderTotal < MinOrderValue) return false;
    return true;
  }

  public decimal CalculateDiscount(decimal orderTotal)
  {
    if (!IsValid(orderTotal)) return 0;
    return DiscountType == DiscountType.Percentage
        ? orderTotal * (DiscountValue / 100)
        : Math.Min(DiscountValue, orderTotal);
  }

  public void Redeem()
  {
    if (MaxUses.HasValue && UsedCount >= MaxUses) throw new DomainException("Coupon usage limit reached.");
    UsedCount++;
  }
}