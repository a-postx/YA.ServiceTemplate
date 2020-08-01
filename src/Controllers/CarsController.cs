using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using YA.ServiceTemplate.Application.Commands;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Application.Models.ViewModels;
using Swashbuckle.AspNetCore.Annotations;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Application.ActionFilters;

namespace YA.ServiceTemplate.Controllers
{
    /// <summary>
    /// Control requests handling for car objects.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [ApiVersion(ApiVersionName.V1)]
    [ServiceFilter(typeof(ApiRequestFilter))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, HttpCodeMessages.Code500ErrorMessage, typeof(ProblemDetails))]
    public class CarsController : ControllerBase
    {
        /// <summary>
        /// Return Allow HTTP header with allowed HTTP methods.
        /// </summary>
        /// <returns>200 OK response.</returns>
        [HttpOptions(Name = RouteNames.OptionsCars)]
        [SwaggerResponse(StatusCodes.Status200OK, "Allowed HTTP methods.")]
        public IActionResult Options()
        {
            HttpContext.Response.Headers.AppendCommaSeparatedValues(
                HeaderNames.Allow,
                HttpMethods.Get,
                HttpMethods.Head,
                HttpMethods.Options,
                HttpMethods.Post);
            return Ok();
        }

        /// <summary>
        /// Return Allow HTTP header with allowed HTTP methods for a car with the specified unique identifier.
        /// </summary>
        /// <param name="carId">Cars unique identifier.</param>
        /// <returns>200 OK response.</returns>
        [HttpOptions("{carId}", Name = RouteNames.OptionsCar)]
        [SwaggerResponse(StatusCodes.Status200OK, "Allowed HTTP methods.")]
        public IActionResult Options(int carId)
        {
            HttpContext.Response.Headers.AppendCommaSeparatedValues(
                HeaderNames.Allow,
                HttpMethods.Delete,
                HttpMethods.Get,
                HttpMethods.Head,
                HttpMethods.Options,
                HttpMethods.Patch,
                HttpMethods.Post,
                HttpMethods.Put);
            return Ok();
        }

        /// <summary>
        /// Delete car with a specified unique identifier.
        /// </summary>
        /// <param name="command">Action command.</param>
        /// <param name="carId">Cars unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the HTTP request.</param>
        /// <returns>204 No Content response if the car was deleted or 404 Not Found if a car with the specified
        /// unique identifier was not found.</returns>
        [HttpDelete("{carId}", Name = RouteNames.DeleteCar)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Car with the specified unique identifier was deleted.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Car with the specified unique identifier was not found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Duplicate request.", typeof(ProblemDetails))]
        public Task<IActionResult> DeleteAsync(
            [FromServices] IDeleteCarCommand command,
            int carId,
            CancellationToken cancellationToken)
        {
            return command.ExecuteAsync(carId, cancellationToken);
        }

        /// <summary>
        /// Get a car with the specified unique identifier.
        /// </summary>
        /// <param name="command">Action command.</param>
        /// <param name="carId">Car unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the HTTP request.</param>
        /// <returns>200 OK response containing the car or 404 Not Found if a car with the specified unique
        /// identifier was not found.</returns>
        [HttpGet("{carId}", Name = RouteNames.GetCar)]
        [HttpHead("{carId}", Name = RouteNames.HeadCar)]
        [SwaggerResponse(StatusCodes.Status200OK, "Car with the specified unique identifier.", typeof(CarVm))]
        [SwaggerResponse(StatusCodes.Status304NotModified, "The car has not changed since the date given in the If-Modified-Since HTTP header.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Car with the specified unique identifier could not be found.")]
        [SwaggerResponse(StatusCodes.Status406NotAcceptable, "The MIME type in the Accept HTTP header is not acceptable.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Duplicate request.", typeof(ProblemDetails))]
        public Task<IActionResult> GetAsync(
            [FromServices] IGetCarCommand command,
            int carId,
            CancellationToken cancellationToken)
        {
            return command.ExecuteAsync(carId, cancellationToken);
        }

        /// <summary>
        /// Get a collection of cars using the specified paging options.
        /// </summary>
        /// <param name="command">Action command.</param>
        /// <param name="pageOptions">Page options.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the HTTP request.</param>
        /// <returns>200 OK response containing a collection of cars, 400 Bad Request if the page request
        /// parameters are invalid or 404 Not Found if a page with the specified page number was not found.
        /// </returns>
        [HttpGet("", Name = RouteNames.GetCarPage)]
        [HttpHead("", Name = RouteNames.HeadCarPage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Collection of cars for the specified page.", typeof(PaginatedResult<CarVm>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Page request parameters are invalid.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Page with the specified page number was not found.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status406NotAcceptable, "The MIME type in the Accept HTTP header is not acceptable.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Duplicate request.", typeof(ProblemDetails))]
        public Task<IActionResult> GetPageAsync(
            [FromServices] IGetCarPageCommand command,
            [FromQuery] PageOptions pageOptions,
            CancellationToken cancellationToken)
        {
            return command.ExecuteAsync(pageOptions, cancellationToken);
        }

        /// <summary>
        /// Patch car with the specified unique identifier.
        /// </summary>
        /// <param name="command">Action command.</param>
        /// <param name="carId">Cars unique identifier.</param>
        /// <param name="patch">Patch document. See http://jsonpatch.com.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the HTTP request.</param>
        /// <returns>200 OK if the car was patched, 400 Bad Request if the patch was invalid or 404 Not Found
        /// if a car with the specified unique identifier was not found.</returns>
        [HttpPatch("{carId}", Name = RouteNames.PatchCar)]
        [SwaggerResponse(StatusCodes.Status200OK, "Patched car with the specified unique identifier.", typeof(CarVm))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Patch document is invalid.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Car with the specified unique identifier could not be found.")]
        [SwaggerResponse(StatusCodes.Status406NotAcceptable, "The MIME type in the Accept HTTP header is not acceptable.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Duplicate request.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status415UnsupportedMediaType, "The MIME type in the Content-Type HTTP header is unsupported.", typeof(ProblemDetails))]
        public Task<IActionResult> PatchAsync(
            [FromServices] IPatchCarCommand command,
            int carId,
            [FromBody] JsonPatchDocument<CarSm> patch,
            CancellationToken cancellationToken)
        {
            return command.ExecuteAsync(carId, patch, cancellationToken);
        }

        /// <summary>
        /// Create a new car.
        /// </summary>
        /// <param name="command">Action command.</param>
        /// <param name="car">Car to create.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the HTTP request.</param>
        /// <returns>201 Created response containing newly created car or 400 Bad Request if the car is
        /// invalid.</returns>
        [HttpPost("", Name = RouteNames.PostCar)]
        [SwaggerResponse(StatusCodes.Status201Created, "Car was created.", typeof(CarVm))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Request is invalid.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status406NotAcceptable, "The MIME type in the Accept HTTP header is not acceptable.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Duplicate request.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status415UnsupportedMediaType, "The MIME type in the Content-Type HTTP header is unsupported.", typeof(ProblemDetails))]
        public Task<IActionResult> PostAsync(
            [FromServices] IPostCarCommand command,
            [FromBody] CarSm car,
            CancellationToken cancellationToken)
        {
            return command.ExecuteAsync(car, cancellationToken);
        }

        /// <summary>
        /// Update existing car with the specified unique identifier.
        /// </summary>
        /// <param name="command">Action command.</param>
        /// <param name="carId">Car identifier.</param>
        /// <param name="car">Car to update.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the HTTP request.</param>
        /// <returns>200 OK response containing updated car, 400 Bad Request if the car is invalid or
        /// 404 Not Found if a car with the specified unique identifier was not found.</returns>
        [HttpPut("{carId}", Name = RouteNames.PutCar)]
        [SwaggerResponse(StatusCodes.Status200OK, "Car was updated.", typeof(CarVm))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Request is invalid.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "A car with the specified unique identifier could not be found.")]
        [SwaggerResponse(StatusCodes.Status406NotAcceptable, "The MIME type in the Accept HTTP header is not acceptable.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Duplicate request.", typeof(ProblemDetails))]
        [SwaggerResponse(StatusCodes.Status415UnsupportedMediaType, "The MIME type in the Content-Type HTTP header is unsupported.", typeof(ProblemDetails))]
        public Task<IActionResult> PutAsync(
            [FromServices] IPutCarCommand command,
            int carId,
            [FromBody] CarSm car,
            CancellationToken cancellationToken)
        {
            return command.ExecuteAsync(carId, car, cancellationToken);
        }
    }
}
