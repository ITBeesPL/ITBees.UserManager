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
using System.Linq;
using System.Net.Http;
using System.Reflection;
using ITBees.Interfaces.Platforms;
using ITBees.Mailing;
using ITBees.Mailing.Interfaces;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.AppleLogins;
using ITBees.UserManager.Services.FacebookLogins;
using ITBees.UserManager.Services.Mailing;
using ITBees.UserManager.Services.Passwords;
using ITBees.UserManager.Services.Registration;

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
            services.AddScoped(typeof(IMyAccountServie), typeof(MyAccountService));
            services.AddScoped(typeof(INewUserRegistrationService), typeof(NewUserRegistrationService<TIdentityUser>));
            services.AddScoped(typeof(INewUserRegistrationFromGoogle), typeof(NewUserRegistrationFromGoogle<TIdentityUser>));
            services.AddScoped(typeof(ILoginService<>), typeof(LoginService<>));
            services.AddScoped(typeof(IGoogleLoginService<>), typeof(GoogleLoginService<>));
            services.AddScoped(typeof(IConfirmRegistrationService<>), typeof(ConfirmRegistrationService<>));
            services.AddScoped(typeof(IUserManager), typeof(FASUserManager<TIdentityUser>));
            services.AddScoped<IEmailAvailabilityAndConfirmationStatusCheckingService, EmailAvailabilityAndConfirmationStatusCheckingService>();
            services.AddScoped<IRegistrationEmailComposer, RegistrationEmailComposer>();
            services.AddScoped<IAccessControlService, AccessControlService>();
            services.AddScoped<IRoleAddingService, RoleAddingService>();
            services.AddScoped<IPasswordResettingService, PasswordResettingService>();
            services.AddScoped<IResetPasswordEmailConstructorService, ResetPasswordEmailConstructorService>();
            services.AddScoped<IChangePasswordService, ChangePasswordService>();
            services.AddScoped<IAcceptInvitationService, AcceptInvitationService>();
            services.AddScoped<IAwaitingInvitationsService, AwaitingInvitationsService>();
            services.AddScoped<IMyCompaniesService, MyCompaniesService>();
            services.AddScoped<INewUserRegistrationFromApple, NewUserRegistrationFromApple<TIdentityUser>>();
            services.AddScoped<IAppleLoginService<TIdentityUser>, AppleLoginService<TIdentityUser>>();
            services.AddScoped<IFacebookLoginService<TIdentityUser>, FacebookLoginService<TIdentityUser>>();
            services.AddScoped<HttpClient>();
            services.AddScoped(typeof(UserManager<TIdentityUser>));
            if(services.Any(descriptor =>
                   descriptor.ServiceType == typeof(IEmailSendingService)) == false)
            {
                Console.WriteLine($"Using default implementation for EmailSendingService in UserManager.");
                services.AddScoped<IEmailSendingService, EmailSendingService>();
            };
            if (services.Any(descriptor =>
                    descriptor.ServiceType == typeof(IUserManagerSettings)) == false)
            {
                var message = $"Please create class with IUserManagerSettings for proper configuration in UserManager module";
                Console.WriteLine(message);
                throw new Exception(message);
            };
            if (services.Any(descriptor =>
                    descriptor.ServiceType == typeof(IPlatformSettingsService)) == false)
            {
                var message = $"Please register IPlatformSettingsService in DI container";
                throw new Exception(message);
            };

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
                .AddDefaultTokenProviders()
                .AddRoles<IdentityRole>();

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

    public class GenericRestControllerFeatureProvider<T> : IApplicationFeatureProvider<ControllerFeature> where T : IdentityUser
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {

            var controller_type = typeof(LoginController<>).MakeGenericType(typeof(T)).GetTypeInfo();
            feature.Controllers.Add(controller_type);
            feature.Controllers.Add(typeof(ConfirmRegistrationController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(GoogleLoginController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(AppleLoginController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(FacebookLoginController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            return;
        }
    }
}