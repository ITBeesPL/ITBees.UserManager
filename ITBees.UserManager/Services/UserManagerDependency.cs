using System;
using System.Threading.Tasks;
using ITBees.UserManager.Helpers;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ITBees.UserManager.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections.Generic;
using System.Reflection;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;
using Nelibur.ObjectMapper;

namespace ITBees.UserManager.Services
{
    public class UserManagerDependency
    {
        public static void RegisterDefaultFASImplementation<TContext, TIdentityUser>(IServiceCollection services, IConfigurationRoot configurationRoot) where TContext : DbContext where TIdentityUser : IdentityUser, new()
        {
            TinyMapperSetup.ConfigureMappings();
            services
                .AddMvc()
                .AddMvcOptions(o => o.Conventions.Add(new GenericRestControllerNameConvention()))
                .ConfigureApplicationPartManager(c =>
                {
                    c.FeatureProviders.Add(new GenericRestControllerFeatureProvider<TIdentityUser>());
                });

            services.AddScoped(typeof(INewUserRegistrationService), typeof(NewUserRegistrationService<TIdentityUser>));
            services.AddScoped(typeof(ILoginService<>), typeof(LoginService<>));
            services.AddScoped(typeof(IUserManager), typeof(FASUserManager<TIdentityUser>));
            services.AddScoped(typeof(UserManager<TIdentityUser>));
            services.AddIdentity<TIdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
                .AddEntityFrameworkStores<TContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = configurationRoot.GetSection("TokenIssuer").Value,
                        ValidAudience = configurationRoot.GetSection("TokenAudience").Value,
                        IssuerSigningKey = JwtSecurityKey.Create(configurationRoot.GetSection("TokenSecretKey").Value)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("OnAuthenticationFailed: " + context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("OnTokenValidated: " + context.SecurityToken);
                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }

    public class TinyMapperSetup
    {
        public static void ConfigureMappings()
        {
            TinyMapper.Bind<MyAccount, MyAccountVm>();
        }
    }

    public class GenericRestControllerFeatureProvider<T> : IApplicationFeatureProvider<ControllerFeature> where T: IdentityUser
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            
            var controller_type = typeof(LoginController<>).MakeGenericType(typeof(T)).GetTypeInfo();
            feature.Controllers.Add(controller_type);
            return;
        }
    }
}