﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Formatters;
using LandonApi.Infrastructure;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;
using LandonApi.Filters;
using LandonApi.Models;
using Microsoft.EntityFrameworkCore;
using LandonApi.Services;
using AutoMapper;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using AspNet.Security.OpenIdConnect.Primitives;

namespace LandonApi
{
    public class Startup
    {
        private readonly int? _httpsPort;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            // Get the HTTPS port (only in development)
            if (env.IsDevelopment())
            {
                var launchJsonConfig = new ConfigurationBuilder()
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile("Properties\\launchSettings.json")
                    .Build();
                _httpsPort = launchJsonConfig.GetValue<int>("iisSettings:iisExpress:sslPort");
            }
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Use an in-memory database for quick dev and testing
            // TODO: Swap out with a real database in production
            services.AddDbContext<HotelApiContext>(opt =>
            {
                opt.UseInMemoryDatabase();
                opt.UseOpenIddict();
            });

            // Map some of the default claim names to the proper OpenID Connect claim names
            services.Configure<IdentityOptions>(opt =>
            {
                opt.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                opt.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                opt.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            // Add OpenIddict services
            services.AddOpenIddict<Guid>(opt =>
            {
                opt.AddEntityFrameworkCoreStores<HotelApiContext>();
                opt.AddMvcBinders();

                opt.EnableTokenEndpoint("/token");
                opt.AllowPasswordFlow();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
                });
            });

            // Add ASP.NET Core Identity
            services.AddIdentity<UserEntity, UserRoleEntity>(
                opt =>
                {
                    opt.Password.RequireDigit = false;
                    opt.Password.RequireLowercase = false;
                    opt.Password.RequireUppercase = false;
                    opt.Password.RequireNonAlphanumeric = false;
                    opt.Password.RequiredLength = 6;
                })
                .AddEntityFrameworkStores<HotelApiContext, Guid>()
                .AddDefaultTokenProviders();

            services.AddAutoMapper();
            services.AddResponseCaching();

            // Add framework services.
            services.AddMvc(opt =>
            {
                opt.Filters.Add(typeof(JsonExceptionFilter));
                opt.Filters.Add(typeof(LinkRewritingFilter));

                // Require HTTPS for all controllers
                opt.SslPort = _httpsPort;
                opt.Filters.Add(typeof(RequireHttpsAttribute));

                var jsonFormatter = opt.OutputFormatters.OfType<JsonOutputFormatter>().Single();
                opt.OutputFormatters.Remove(jsonFormatter);
                opt.OutputFormatters.Add(new IonOutputFormatter(jsonFormatter));

                opt.CacheProfiles.Add("Static", new CacheProfile
                {
                    Duration = 86400
                });
            })
            .AddJsonOptions(opt =>
            {
                // These should be the defaults, but we can be explicit:
                opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                opt.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                opt.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
            });

            services.AddRouting(opt => opt.LowercaseUrls = true);

            services.AddApiVersioning(opt =>
            {
                opt.ApiVersionReader = new MediaTypeApiVersionReader();
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.ApiVersionSelector = new CurrentImplementationApiVersionSelector(opt);
            });

            services.Configure<HotelOptions>(Configuration);
            services.Configure<HotelInfo>(Configuration.GetSection("Info"));
            services.Configure<PagingOptions>(Configuration.GetSection("DefaultPagingOptions"));

            services.AddScoped<IRoomService, DefaultRoomService>();
            services.AddScoped<IOpeningService, DefaultOpeningService>();
            services.AddScoped<IBookingService, DefaultBookingService>();
            services.AddScoped<IDateLogicService, DefaultDateLogicService>();
            services.AddScoped<IUserService, DefaultUserService>();

            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("ViewAllUsersPolicy",
                    p => p.RequireAuthenticatedUser().RequireRole("Admin"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors("AllowAllOrigins");
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            // Add some test data in development
            if (env.IsDevelopment())
            {
                // Add test roles and users
                var roleManager = app.ApplicationServices
                    .GetRequiredService<RoleManager<UserRoleEntity>>();
                var userManager = app.ApplicationServices
                    .GetRequiredService<UserManager<UserEntity>>();
                AddTestUsers(roleManager, userManager).Wait();

                var context = app.ApplicationServices.GetRequiredService<HotelApiContext>();
                var dateLogicService = app.ApplicationServices.GetRequiredService<IDateLogicService>();
                AddTestData(context, dateLogicService);
            }

            app.UseHsts(opt =>
            {
                opt.MaxAge(days: 180);
                opt.IncludeSubdomains();
                opt.Preload();
            });

            app.UseOAuthValidation();
            app.UseOpenIddict();

            app.UseResponseCaching();
            app.UseMvc();
            app.UseApiVersioning();
        }

        private static async Task AddTestUsers(
            RoleManager<UserRoleEntity> roleManager,
            UserManager<UserEntity> userManager)
        {
            // Add a test role
            await roleManager.CreateAsync(new UserRoleEntity("Admin"));

            // Add a test user
            var user = new UserEntity
            {
                Email = "admin@landon.local",
                UserName = "admin@landon.local",
                FirstName = "Admin",
                LastName = "Testerman",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, "Supersecret123!!");

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "Admin");
            await userManager.UpdateAsync(user);
        }

        private static void AddTestData(
            HotelApiContext context,
            IDateLogicService dateLogicService)
        {
            context.Rooms.Add(new RoomEntity
            {
                Id = Guid.Parse("ee2b83be-91db-4de5-8122-35a9e9195976"),
                Name = "Driscoll Suite",
                Rate = 23959
            });

            var oxford = context.Rooms.Add(new RoomEntity
            {
                Id = Guid.Parse("301df04d-8679-4b1b-ab92-0a586ae53d08"),
                Name = "Oxford Suite",
                Rate = 10119,
            }).Entity;

            var today = DateTimeOffset.Now;
            var start = dateLogicService.AlignStartTime(today);
            var end = start.Add(dateLogicService.GetMinimumStay());

            context.Bookings.Add(new BookingEntity
            {
                Id = Guid.Parse("2eac8dea-2749-42b3-9d21-8eb2fc0fd6bd"),
                Room = oxford,
                CreatedAt = DateTimeOffset.UtcNow,
                StartAt = start,
                EndAt = end,
                Total = oxford.Rate,
            });

            context.SaveChanges();
        }
    }
}
