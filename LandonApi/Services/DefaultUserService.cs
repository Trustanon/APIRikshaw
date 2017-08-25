using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LandonApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using System.Security.Claims;
using AutoMapper;

namespace LandonApi.Services
{
    public class DefaultUserService : IUserService
    {
        private readonly UserManager<UserEntity> _userManager;

        public DefaultUserService(UserManager<UserEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<(bool Succeeded, string Error)> CreateUserAsync(RegisterForm form)
        {
            var entity = new UserEntity
            {
                Email = form.Email,
                UserName = form.Email,
                FirstName = form.FirstName,
                LastName = form.LastName,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await _userManager.CreateAsync(entity, form.Password);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return (false, firstError);
            }

            return(true,null);
        }


        public async Task<(bool Succeeded, string Error)> EditUserAsync(ClaimsPrincipal user, EditForm form)
        {
            var entity = await _userManager.GetUserAsync(user);
            entity.FirstName = form.FirstName;
            entity.LastName = form.LastName;
            //entity = changes;
            var result = await _userManager.UpdateAsync(entity);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return (false, firstError);
            }
            return (true, null);
        }
        public async Task<(bool Succeeded, string Error)> EditUserEmailAsync(ClaimsPrincipal user, EditEmailForm form)
        {
            if (form.Email != form.EmailConfirm) return (false, "Emails dont match");
            var entity = await _userManager.GetUserAsync(user);
            entity.Email = form.Email;
            var result = await _userManager.UpdateAsync(entity);

            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return (false, firstError);
            }
            return (true, null);
        }

        public async Task<(bool Succeeded,string Error)> DeleteUserAsync(ClaimsPrincipal user)
        {
            var entity = await _userManager.GetUserAsync(user);
            var result = await _userManager.DeleteAsync(entity);

                if (!result.Succeeded)
                {
                    var firstError = result.Errors.FirstOrDefault()?.Description;
                    return (false, firstError);
                }
            return (true, null);

        }
   
    
        public async Task<PagedResults<User>> GetUsersAsync(
            PagingOptions pagingOptions,
            SortOptions<User, UserEntity> sortOptions,
            SearchOptions<User, UserEntity> searchOptions,
            CancellationToken ct)
        {
            IQueryable<UserEntity> query = _userManager.Users;
            query = searchOptions.Apply(query);
            query = sortOptions.Apply(query);

            var size = await query.CountAsync(ct);

            var items = await query
                .Skip(pagingOptions.Offset.Value)
                .Take(pagingOptions.Limit.Value)
                .ProjectTo<User>()
                .ToArrayAsync(ct);

            return new PagedResults<User>
            {
                Items = items,
                TotalSize = size
            };
        }

        public async Task<User> GetUsersAsync(ClaimsPrincipal user)
        {
            var entity = await _userManager.GetUserAsync(user);
            return Mapper.Map<User>(entity);
        }

        public async Task<(bool Succeeded, string Error)> EditUserPasswordAsync(ClaimsPrincipal user, EditPasswordForm form)
        {
            if (form.Password != form.PasswordConfirm) return (false, "Passwords dont match");
            var entity = await _userManager.GetUserAsync(user);
            var change = _userManager.ChangePasswordAsync(entity, form.PasswordCurrent, form.Password);

            var result = await _userManager.UpdateAsync(entity);

            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return (false, firstError);
            }
            return (true, null);
        }
    }
}
