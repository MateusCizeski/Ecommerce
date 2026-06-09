using Application.Common.Interfaces.Payments;

namespace Application.Features.Commerce.Orders;

public record PlaceOrderCommand(
    Guid CartId,
    Guid ShippingAddressId,
    string PaymentMethod,
    string? CouponCode,
    string? Notes) : IRequest<PlaceOrderResult>;

public record PlaceOrderResult(Guid OrderId, string OrderNumber, decimal TotalAmount, string PaymentClientSecret);

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public class PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty().WithMessage("O carrinho associado é obrigatório.");
        RuleFor(x => x.ShippingAddressId).NotEmpty().WithMessage("O endereço de entrega é obrigatório.");
        RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("O método de pagamento é obrigatório.");
    }
}

public class PlaceOrderCommandHandler(
    ICartRepository cartRepo,
    IOrderRepository orderRepo,
    ICouponRepository couponRepo,
    ICustomerRepository customerRepo,
    IProductVariantRepository variantRepo,
    IPaymentService paymentService,
    IOrderNumberGenerator orderNumberGen,
    IUnitOfWork uow,
    ITenantContext tenant) : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct)
            ?? throw new NotFoundException("Carrinho", cmd.CartId);

        if (cart.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        if (cart.IsExpired())
            throw new ConflictException("O carrinho fornecido já expirou.");

        if (!cart.Items.Any())
            throw new ConflictException("Não é possível fechar um pedido com o carrinho vazio.");

        var customer = await customerRepo.GetByIdAsync(cart.CustomerId, ct)
            ?? throw new NotFoundException("Cliente", cart.CustomerId);

        var orderItems = new List<OrderItem>();
        foreach (var cartItem in cart.Items)
        {
            var variant = await variantRepo.GetByIdAsync(cartItem.ProductVariantId, ct)
                ?? throw new NotFoundException("Variante do Produto", cartItem.ProductVariantId);

            orderItems.Add(OrderItem.Create(variant.Id, variant.SKU, variant.Product.Name, cartItem.Quantity, cartItem.UnitPrice));
        }

        decimal discountAmount = 0;
        Guid? couponId = null;
        if (!string.IsNullOrWhiteSpace(cmd.CouponCode))
        {
            var coupon = await couponRepo.GetByCodeAsync(tenant.TenantId, cmd.CouponCode, ct)
                ?? throw new ConflictException($"Cupom '{cmd.CouponCode}' não foi encontrado.");

            if (!coupon.IsValid(cart.Total))
                throw new ConflictException($"O cupom '{cmd.CouponCode}' não é elegível para este pedido.");

            discountAmount = coupon.CalculateDiscount(cart.Total);
            couponId = coupon.Id;
            coupon.Redeem();
        }

        var orderNumber = await orderNumberGen.GenerateAsync(tenant.TenantId, ct);

        var order = Order.Create(
            tenant.TenantId, cart.CustomerId, cmd.ShippingAddressId,
            orderNumber, orderItems, shippingAmount: 0m, taxAmount: 0m, discountAmount, couponId, cmd.Notes);

        var externalCustomerId = customer.ExternalCustomerId
            ?? await paymentService.CreateOrGetCustomerAsync(customer.Email, customer.FullName, ct);

        if (customer.ExternalCustomerId is null)
            customer.SetExternalCustomerId(externalCustomerId);

        var intent = await paymentService.CreatePaymentIntentAsync(order.TotalAmount, "BRL", externalCustomerId, ct);

        if (!Enum.TryParse<PaymentMethod>(cmd.PaymentMethod, true, out var method))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                { nameof(cmd.PaymentMethod), ["Método de pagamento inválido."] }
            });
        }

        var payment = order.AddPayment(method, order.TotalAmount);
        payment.SetExternalPaymentIntentId(intent.PaymentIntentId);

        cart.Checkout();

        await orderRepo.AddAsync(order, ct);
        await uow.CommitAsync(ct);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.TotalAmount, intent.ClientSecret);
    }
}