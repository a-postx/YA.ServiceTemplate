using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace YA.ServiceTemplate.Controllers
{
    [Route("/")]
    [ApiController]
    [AllowAnonymous]
    public class HomeController : ControllerBase
    {
        /// <summary>
        /// Get root page.
        /// </summary>
        /// <returns>200 OK response.
        /// </returns>
        [HttpGet("")]
        [SwaggerResponse(StatusCodes.Status200OK, "Empty 200 response.")]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}