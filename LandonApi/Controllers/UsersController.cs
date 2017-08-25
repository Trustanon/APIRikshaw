using LandonApi.Models;
using LandonApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace LandonApi.Controllers
{
    [Route("/[controller]")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthorizationService _authzService;
        private readonly PagingOptions _defaultPagingOptions;

        public UsersController(
            IUserService userService,
            IAuthorizationService authzService,
            IOptions<PagingOptions> defaultPagingOptions)
        {
            _userService = userService;
            _authzService = authzService;
            _defaultPagingOptions = defaultPagingOptions.Value;
        }

        [HttpGet(Name = nameof(GetVisibleUsersAsync))]
        public async Task<IActionResult> GetVisibleUsersAsync(
            [FromQuery] PagingOptions pagingOptions,
            [FromQuery] SortOptions<User, UserEntity> sortOptions,
            [FromQuery] SearchOptions<User, UserEntity> searchOptions,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));

            pagingOptions.Offset = pagingOptions.Offset ?? _defaultPagingOptions.Offset;
            pagingOptions.Limit = pagingOptions.Limit ?? _defaultPagingOptions.Limit;

            var users = new PagedResults<User>();
            if(User.Identity.IsAuthenticated)
            {
                var canSeeEveryone = await _authzService.AuthorizeAsync(User, "ViewAllUsersPolicy");
                if(canSeeEveryone)
                {
                    users = await _userService.GetUsersAsync(
                        pagingOptions, sortOptions, searchOptions, ct);
                }
                else
                {
                    var myself = await _userService.GetUsersAsync(User);
                    users.Items = new[] { myself };
                    users.TotalSize = 1;
                }
            }


            // TODO: Return a collection of visible users
           

            var collection = PagedCollection<User>.Create(
                Link.To(nameof(GetVisibleUsersAsync)),
                users.Items.ToArray(),
                users.TotalSize,
                pagingOptions);

            return Ok(collection);
        }


        [Authorize]
        [HttpGet("me", Name = nameof(GetMeAsync))]
        public async Task<IActionResult> GetMeAsync(CancellationToken ct)
        {
            if (User == null) return BadRequest();

            var user = await _userService.GetUsersAsync(User);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [Authorize]
        [HttpPatch("edit/name", Name = nameof(EditMeAsync))]
        public async Task<IActionResult> EditMeAsync(
            [FromBody] EditForm form,
            CancellationToken ct,
            ClaimsPrincipal user)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));
            if (user == null) return BadRequest();

            var (succeeded, error) = await _userService.EditUserAsync(User,form);

            var users = await _userService.GetUsersAsync(User);
            if (succeeded) return Ok(users);

            return BadRequest(new ApiError
            {
                Message = "Edit failed.",
                Detail = error
            });

            
        }
        
        [Authorize]
        [HttpPatch("edit/email", Name = nameof(EditMeEmailAsync))]
        public async Task<IActionResult> EditMeEmailAsync(
        [FromBody] EditEmailForm form,
        CancellationToken ct,
        ClaimsPrincipal user)
        {

            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));
            if (user == null) return BadRequest();

            var (succeeded, error) = await _userService.EditUserEmailAsync(User, form);

            var users = await _userService.GetUsersAsync(User);
            if (succeeded) return Ok(users);

            return BadRequest(new ApiError
            {
                Message = "Email edit failed.",
                Detail = error
            });

        
        }


        [Authorize]
        [HttpPatch("edit/password", Name = nameof(EditMePasswordAsync))]
        public async Task<IActionResult> EditMePasswordAsync(
        [FromBody] EditPasswordForm form,
        CancellationToken ct,
        ClaimsPrincipal user)
        {

            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));
            if (user == null) return BadRequest();

            var (succeeded, error) = await _userService.EditUserPasswordAsync(User, form);

            var users = await _userService.GetUsersAsync(User);
            if (succeeded) return Ok();

            return BadRequest(new ApiError
            {
                Message = "Email edit failed.",
                Detail = error
            });


        }

        [Authorize]
        [HttpDelete("delete",Name = nameof(DeleteMeAsync))]
        public async Task<IActionResult> DeleteMeAsync(ClaimsPrincipal user)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));
            if (user == null) return BadRequest();

            var (succeeded, error) = await _userService.DeleteUserAsync(User);

            if (succeeded) return Ok();

            return BadRequest(new ApiError
            {
                Message = "Delete account failed.",
                Detail = error
            });

        }

        [HttpPost(Name = nameof(RegisterUserAsync))]
        public async Task<IActionResult> RegisterUserAsync(
            [FromBody] RegisterForm form,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));

            var (succeeded, error) = await _userService.CreateUserAsync(form);
            if (succeeded) return Created(Url.Link(nameof(GetMeAsync), null), null);

            return BadRequest(new ApiError
            {
                Message = "Registration failed.",
                Detail = error
            });
        }

        [Authorize]
        [HttpGet("{userId}", Name = nameof(GetUserByIdAsync))]
        public Task<IActionResult> GetUserByIdAsync(Guid userId, CancellationToken ct)
        {
            // TODO is userId the current user's ID?
            // If so, return myself.
            // If not, only Admin roles should be able to view arbitrary users.
            throw new NotImplementedException();
        }
    }
}
