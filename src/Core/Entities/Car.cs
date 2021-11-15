namespace YA.ServiceTemplate.Core.Entities;

/// <summary>
/// Model object for a car.
/// </summary>
public class Car
{
    public int CarId { get; set; }
    public int Cylinders { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
}
