using GoldLedger.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace GoldLedger.API.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult(this Result result)
        {
            if (result.IsSuccess)
                return new OkObjectResult(result);

            return result.ErrorType switch
            {
                ErrorType.Validation => new UnprocessableEntityObjectResult(result),
                ErrorType.NotFound => new NotFoundObjectResult(result),
                ErrorType.Conflict => new ConflictObjectResult(result),
                ErrorType.ServerError => new ObjectResult(result) { StatusCode = StatusCodes.Status500InternalServerError },
                _ => new BadRequestObjectResult(result)
            };
        }

        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                return new OkObjectResult(result);

            return result.ErrorType switch
            {
                ErrorType.Validation => new UnprocessableEntityObjectResult(result),
                ErrorType.NotFound => new NotFoundObjectResult(result),
                ErrorType.Conflict => new ConflictObjectResult(result),
                ErrorType.ServerError => new ObjectResult(result) { StatusCode = StatusCodes.Status500InternalServerError },
                _ => new BadRequestObjectResult(result)
            };
        }

        public static IActionResult ToCreatedActionResult<T>(this Result<T> result, string uri = "")
        {
            if (!result.IsSuccess)
                return result.ToActionResult();

            return new CreatedResult(uri, result);
        }
    }
}
