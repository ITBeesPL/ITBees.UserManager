using System;
using System.Threading.Tasks;
using ITBees.UserManager.Helpers;
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
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.PlatformOperator;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.AppleLogins;
using ITBees.UserManager.Services.FacebookLogins;
using ITBees.UserManager.Services.GoogleLogins;
using ITBees.UserManager.Services.Invitations;
using ITBees.UserManager.Services.Mailing;
using ITBees.UserManager.Services.Passwords;
using ITBees.UserManager.Services.PlatformOperator;
using ITBees.UserManager.Services.Registration;

namespace ITBees.UserManager.Services
{
    public class FasDependencyRegistration
    {
        public static void Register<TContext, TIdentityUser, TCompany>(
            IServiceCollection services, 
            IConfigurationRoot configurationRoot, 
            bool enableLoginWithoutPasswordChecking = false) where TContext : DbContext where TIdentityUser : IdentityUser<Guid>, new() where TCompany : Company, new()
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
            services.AddScoped(typeof(IUserRolesService), typeof(UserRolesService));
            services.AddScoped(typeof(INewUserRegistrationService), typeof(NewUserRegistrationService<TIdentityUser, TCompany>));
            services.AddScoped(typeof(INewUserRegistrationFromGoogle), typeof(NewUserRegistrationFromGoogle<TIdentityUser>));
            services.AddScoped(typeof(IInvitationResendService), typeof(InvitationResendService));
            services.AddScoped(typeof(IUserInvitationService), typeof(UserInvitationService));
            services.AddScoped(typeof(ITokenAsService<>), typeof(TokenAsService<>));

            if (enableLoginWithoutPasswordChecking)
            {
                services.AddScoped(typeof(ILoginService<>), typeof(PlatformDebugLoginService<>));
            }
            else
            {
                services.AddScoped(typeof(ILoginService<>), typeof(LoginService<>));
            }
            
            services.AddScoped(typeof(IPlatformUsersService), typeof(PlatformUsersService));
            services.AddScoped(typeof(IGoogleLoginService<>), typeof(GoogleLoginService<>));
            services.AddScoped(typeof(IConfirmRegistrationService<>), typeof(ConfirmRegistrationService<>));
            services.AddScoped(typeof(IUserManager<TIdentityUser>), typeof(FASUserManager<TIdentityUser>));
            services.AddScoped<IEmailAvailabilityAndConfirmationStatusCheckingService, EmailAvailabilityAndConfirmationStatusCheckingService<TIdentityUser>>();
            services.AddScoped<IRegistrationEmailComposer, RegistrationEmailComposer>();
            services.AddScoped<IAccessControlService, AccessControlService>();
            services.AddScoped<IRoleAddingService, RoleAddingService<TIdentityUser>>();
            services.AddScoped<IPasswordResettingService, PasswordResettingService<TIdentityUser>>();
            services.AddScoped<IResetPasswordEmailConstructorService, ResetPasswordEmailConstructorService>();
            services.AddScoped<IChangePasswordService, ChangePasswordService<TIdentityUser>>();
            services.AddScoped<IAcceptInvitationService, AcceptInvitationService>();
            services.AddScoped<IAwaitingInvitationsService, AwaitingInvitationsService>();
            services.AddScoped<IMyCompaniesService, MyCompaniesService>();
            services.AddScoped<INewUserRegistrationFromApple, NewUserRegistrationFromApple<TIdentityUser>>();
            services.AddScoped<IAppleLoginService<TIdentityUser>, AppleLoginService<TIdentityUser>>();
            services.AddScoped<IFacebookLoginService<TIdentityUser>, FacebookLoginService<TIdentityUser>>();
            services.AddScoped<IMyAccountUpdateService, MyAccountUpdateService>();
            services.AddScoped<IPlatformStatusService, PlatformStatusService>();
            services.AddScoped<HttpClient>();
            services.AddScoped(typeof(UserManager<TIdentityUser>));
            CheckForUsereDeleteAccountServiceImplementation(services);
            if (services.Any(descriptor =>
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

            services.AddIdentity<TIdentityUser, FasIdentityRole>(options =>
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
                .AddRoles<FasIdentityRole>();

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

        private static void CheckForUsereDeleteAccountServiceImplementation(IServiceCollection services)
        {
            if (services.Any(descriptor =>
                    descriptor.ServiceType == typeof(IAccountDeleteService<>)) == false)
            {
                var message = $"You have to provide Your own implementation for IAccountDeleteService service to user FasManager";
                Console.WriteLine(message);
                throw new ArgumentException(message);
            };
        }
    }

    public class GenericRestControllerFeatureProvider<T> : IApplicationFeatureProvider<ControllerFeature> where T : IdentityUser<Guid>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {

            var controller_type = typeof(LoginController<>).MakeGenericType(typeof(T)).GetTypeInfo();
            feature.Controllers.Add(controller_type);
            feature.Controllers.Add(typeof(ConfirmRegistrationController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(GoogleLoginController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(AppleLoginController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(FacebookLoginController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(MyAccountController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(AccountDeleteController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(TokenAsController<>).MakeGenericType(typeof(T)).GetTypeInfo());
            feature.Controllers.Add(typeof(LoginAsParamController<>).MakeGenericType(typeof(T)).GetTypeInfo());
        }
    }
}