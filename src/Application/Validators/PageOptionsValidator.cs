using FluentValidation;
using YA.ServiceTemplate.Application.Models.HttpQueryParams;

namespace YA.ServiceTemplate.Application.Validators
{
    public class PageOptionsValidator : AbstractValidator<PageOptionsCursor>
    {
        public PageOptionsValidator()
        {
            RuleFor(e => e.First).InclusiveBetween(1, 20);
            RuleFor(e => e.Last).InclusiveBetween(1, 20);
        }
    }
}
