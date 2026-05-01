using Application.Interfaces;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application.Features.Commerce.Orders;

public record PlaceOrderCommand(Guid CartId, Guid ShippingAddressId, string PaymentMethod, string? CouponCode, string? Notes) : IRequest<PlaceOrderResult>;
public record PlaceOrderResult(Guid OrderId, string OrderNumber, decimal TotalAmount, string PaymentClientSecret);

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.ShippingAddressId).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty();
    }
}

public class PlaceOrderCommandHandler(ICartRepository cartRepo, IOrderRepository orderRepo, ICouponRepository couponRepo,
    ICustomerRepository customerRepo, IProductVariantRepository variantRepo, IPaymentGateway paymentGateway,
    IOrderNumberGenerator orderNumberGen, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct)
            ?? throw new NotFoundException(nameof(Cart), cmd.CartId);
        if (cart.TenantId != tenant.TenantId) throw new TenantAccessException();
        if (cart.IsExpired()) throw new DomainException("Cart has expired.");
        if (!cart.Items.Any()) throw new DomainException("Cart is empty.");

        var customer = await customerRepo.GetByIdAsync(cart.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), cart.CustomerId);

        var orderItems = new List<OrderItem>();
        foreach (var cartItem in cart.Items)
        {
            var variant = await variantRepo.GetByIdAsync(cartItem.ProductVariantId, ct)
                ?? throw new NotFoundException(nameof(ProductVariant), cartItem.ProductVariantId);
            orderItems.Add(OrderItem.Create(variant.Id, variant.SKU, variant.Product.Name, cartItem.Quantity, cartItem.UnitPrice));
        }

        decimal discountAmount = 0;
        Guid? couponId = null;
        if (!string.IsNullOrWhiteSpace(cmd.CouponCode))
        {
            var coupon = await couponRepo.GetByCodeAsync(tenant.TenantId, cmd.CouponCode, ct)
                ?? throw new DomainException($"Coupon '{cmd.CouponCode}' not found.");
            if (!coupon.IsValid(cart.Total)) throw new DomainException($"Coupon '{cmd.CouponCode}' is not valid for this order.");
            discountAmount = coupon.CalculateDiscount(cart.Total);
            couponId = coupon.Id;
            coupon.Redeem();
        }

        var orderNumber = await orderNumberGen.GenerateAsync(tenant.TenantId, ct);
        var order = Order.Create(tenant.TenantId, cart.CustomerId, cmd.ShippingAddressId,
            orderNumber, orderItems, shippingAmount: 0m, taxAmount: 0m, discountAmount, couponId, cmd.Notes);

        var stripeCustomerId = customer.StripeCustomerId
            ?? await paymentGateway.CreateOrGetCustomerAsync(customer.Email, customer.FullName, ct);
        if (customer.StripeCustomerId is null) customer.SetStripeCustomerId(stripeCustomerId);

        var intent = await paymentGateway.CreatePaymentIntentAsync(order.TotalAmount, "USD", stripeCustomerId, ct);
        var payment = order.AddPayment(Enum.Parse<PaymentMethod>(cmd.PaymentMethod, true), order.TotalAmount);
        payment.SetStripePaymentIntentId(intent.PaymentIntentId);

        cart.Checkout();
        await orderRepo.AddAsync(order, ct);
        await uow.CommitAsync(ct);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.TotalAmount, intent.ClientSecret);
    }
}