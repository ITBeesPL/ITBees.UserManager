using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ITBees.UserManager.Controllers
{
    [ApiController]
    [GenericRestControllerNameConvention]
    [Route("/Login")]
    public class LoginController<T> : RestfulControllerBase<LoginController<T>> where T: IdentityUser
    {
        private readonly ILoginService<T> _loginService;

        public LoginController(ILoginService<T> loginService, ILogger<LoginController<T>> logger) : base(logger)
        {
            _loginService = loginService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            Console.WriteLine("test");
            return Ok();
        }
        /// <summary>
        /// Allows the user to log in, by returning valid token with expiration date
        /// </summary>
        /// <param name="loginIm">Login input model</param>
        /// <returns>Returns jwt token and token expiration date</returns>
        [Produces(typeof(TokenVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginIm loginIm)
        {
            try
            {
                if (!TryValidateModel(loginIm))
                {
                    return base.CreateBaseErrorResponse(ModelState,string.Empty);
                }

                var token = await _loginService.Login(loginIm.Username, loginIm.Password);
                return Ok(token);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericRestControllerNameConvention : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (!controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() != typeof(ILoginService<>))
            {
                return;
            }
            var entityType = controller.ControllerType.GenericTypeArguments[0];
            controller.ControllerName = entityType.Name;
            controller.RouteValues["Controller"] = entityType.Name;
        }
    }
}