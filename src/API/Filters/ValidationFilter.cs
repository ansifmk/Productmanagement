using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using System.Threading.Tasks;

namespace ProductManagement.API.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var value in context.ActionArguments.Values)
            {
                if (value == null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    var validationContext = new ValidationContext<object>(value);
                    var validationResult = await validator.ValidateAsync(validationContext);

                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(validationResult.Errors);
                    }
                }
            }

            await next();
        }
    }
}
