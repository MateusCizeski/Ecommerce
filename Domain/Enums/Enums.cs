using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Enums;

public enum BillingCycle { Monthly, Yearly }

public enum SubscriptionStatus { Trialing, Active, PastDue, Cancelled, Expired }

public enum ProductStatus { Draft, Active, Archived }

public enum StockMovementType { Purchase, Sale, Return, Adjustment, Transfer }

public enum CartStatus { Active, CheckedOut, Abandoned, Expired }

public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled, Refunded }

public enum PaymentMethod { CreditCard, DebitCard, Pix, BankTransfer, Wallet }

public enum PaymentStatus { Pending, Succeeded, Failed, Refunded, Cancelled }

public enum DiscountType { Percentage, FixedAmount }

