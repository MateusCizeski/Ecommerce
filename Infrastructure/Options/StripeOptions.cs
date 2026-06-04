namespace Infrastructure.Options
{
  /// <summary>
  /// Opções de configuração para Stripe.
  /// </summary>
  public class StripeOptions
  {
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "Stripe";

    /// <summary>Chave secreta da API Stripe.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Chave pública da API Stripe.</summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>Webhook signing secret (opcional).</summary>
    public string? WebhookSigningSecret { get; set; }

    /// <summary>Valida as opções de Stripe.</summary>
    /// <exception cref="ArgumentException">Lançada quando a configuração é inválida.</exception>
    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(SecretKey))
        throw new ArgumentException($"{nameof(SecretKey)} não pode estar vazio.", nameof(SecretKey));

      if (string.IsNullOrWhiteSpace(PublishableKey))
        throw new ArgumentException($"{nameof(PublishableKey)} não pode estar vazio.", nameof(PublishableKey));
    }
  }
}
