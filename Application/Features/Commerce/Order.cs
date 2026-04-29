using Application.Exceptions;
using Application.Interfaces;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application;

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

public record ConfirmOrderPaymentCommand(Guid OrderId, string PaymentIntentId) : IRequest;

public class ConfirmOrderPaymentCommandHandler(IOrderRepository orderRepo, IPaymentGateway paymentGateway, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<ConfirmOrderPaymentCommand>
{
    public async Task Handle(ConfirmOrderPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct) ?? throw new NotFoundException(nameof(Order), cmd.OrderId);
        if (order.TenantId != tenant.TenantId) throw new TenantAccessException();
        var result = await paymentGateway.ConfirmPaymentAsync(cmd.PaymentIntentId, ct);
        var payment = order.Payments.FirstOrDefault(p => p.StripePaymentIntentId == cmd.PaymentIntentId)
            ?? throw new DomainException("Payment intent not found on this order.");
        if (result.Succeeded) { payment.MarkSucceeded(result.ChargeId, result.GatewayResponse); order.ConfirmPayment(); }
        else { payment.MarkFailed(result.GatewayResponse); throw new PaymentException("Payment confirmation failed.", result.GatewayResponse); }
        await uow.CommitAsync(ct);
    }
}

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator() { RuleFor(x => x.OrderId).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); }
}

public class CancelOrderCommandHandler(IOrderRepository orderRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct) ?? throw new NotFoundException(nameof(Order), cmd.OrderId);
        if (order.TenantId != tenant.TenantId) throw new TenantAccessException();
        order.Cancel(cmd.Reason);
        await uow.CommitAsync(ct);
    }
}

public record OrderListItemDto(Guid Id, string OrderNumber, string Status, decimal TotalAmount, DateTime PlacedAt, int ItemCount);
public record OrderDetailDto(Guid Id, string OrderNumber, string Status, decimal Subtotal, decimal DiscountAmount, decimal ShippingAmount, decimal TaxAmount, decimal TotalAmount, DateTime PlacedAt, string? Notes, IEnumerable<OrderItemDto> Items, IEnumerable<PaymentDto> Payments);
public record OrderItemDto(Guid VariantId, string ProductName, string SKU, int Quantity, decimal UnitPrice, decimal TotalPrice);
public record PaymentDto(Guid Id, string Method, string Status, decimal Amount, string Currency, DateTime? PaidAt);

public record GetOrdersQuery(int Page = 1, int PageSize = 20, string? Status = null) : IRequest<PagedResult<OrderListItemDto>>;

public class GetOrdersQueryHandler(IOrderRepository orderRepo, ITenantContext tenant) : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var query = orderRepo.Query(tenant.TenantId);
        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<OrderStatus>(q.Status, true, out var status))
            query = query.Where(o => o.Status == status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.PlacedAt).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(o => new OrderListItemDto(o.Id, o.OrderNumber, o.Status.ToString(), o.TotalAmount, o.PlacedAt, o.Items.Count))
            .ToListAsync(ct);
        return new PagedResult<OrderListItemDto>(items, total, q.Page, q.PageSize);
    }
}

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public class GetOrderByIdQueryHandler(IOrderRepository orderRepo, ITenantContext tenant) : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var o = await orderRepo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException("Order", q.Id);
        if (o.TenantId != tenant.TenantId) throw new TenantAccessException();
        return new OrderDetailDto(o.Id, o.OrderNumber, o.Status.ToString(), o.Subtotal, o.DiscountAmount, o.ShippingAmount, o.TaxAmount, o.TotalAmount, o.PlacedAt, o.Notes,
            o.Items.Select(i => new OrderItemDto(i.ProductVariantId, i.ProductNameSnapshot, i.SKUSnapshot, i.Quantity, i.UnitPrice, i.TotalPrice)),
            o.Payments.Select(p => new PaymentDto(p.Id, p.Method.ToString(), p.Status.ToString(), p.Amount, p.Currency, p.PaidAt)));
    }
}