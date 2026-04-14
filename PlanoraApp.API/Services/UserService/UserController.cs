using Microsoft.AspNetCore.Mvc;
using PlanoraApp.API.Services.UserService.Models;

namespace PlanoraApp.API.Services.UserService
{
    [ApiController]
    [Route("api/v1/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertUser([FromBody] User request)
        {
            var user = await _userService.UpsertUserAsync(request);
            return Ok(user);
        }
    }
}
