using FluentValidation;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.Validators
{
    public class CarSmValidator : AbstractValidator<CarSm>
    {
        public CarSmValidator()
        {
            RuleFor(e => e.Cylinders).InclusiveBetween(1, 20);
            RuleFor(e => e.Brand).NotEmpty();
            RuleFor(e => e.Model).NotEmpty();
        }
    }
}
