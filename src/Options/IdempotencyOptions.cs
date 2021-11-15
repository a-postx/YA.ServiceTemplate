namespace YA.ServiceTemplate.Options;

public class IdempotencyOptions
{
    public bool? IdempotencyFilterEnabled { get; set; }
    public string IdempotencyHeader { get; set; }
}
