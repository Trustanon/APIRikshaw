using LandonApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace LandonApi.Services
{
    public interface IUserService
    {
        Task<PagedResults<User>> GetUsersAsync(
            PagingOptions pagingOptions,
            SortOptions<User, UserEntity> sortOptions,
            SearchOptions<User, UserEntity> searchOptions,
            CancellationToken ct);

        Task<(bool Succeeded, string Error)> CreateUserAsync(RegisterForm form);
        Task<(bool Succeeded, string Error)> EditUserAsync(ClaimsPrincipal user,EditForm form);
        Task<(bool Succeeded, string Error)> EditUserEmailAsync(ClaimsPrincipal user, EditEmailForm form);
        Task<(bool Succeeded, string Error)> DeleteUserAsync(ClaimsPrincipal user);
        Task<User> GetUsersAsync(ClaimsPrincipal user);
        Task<(bool Succeeded,string Error)> EditUserPasswordAsync(ClaimsPrincipal user, EditPasswordForm form);
    }
}
