namespace Ecommerce.Domain;

public enum BillingCycle { Monthly, Yearly }
public enum SubscriptionStatus { Trialing, Active, PastDue, Cancelled, Expired }
public enum ProductStatus { Draft, Active, Archived }
public enum StockMovementType { Purchase, Sale, Return, Adjustment, Transfer }
public enum CartStatus { Active, CheckedOut, Abandoned, Expired }
public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled, Refunded }
public enum PaymentMethod { CreditCard, DebitCard, Pix, BankTransfer, Wallet }
public enum PaymentStatus
{
  Pending,
  Succeeded,
  Failed,
  Refunded,
  PartiallyRefunded,
  Cancelled
}
public enum DiscountType { Percentage, FixedAmount }

