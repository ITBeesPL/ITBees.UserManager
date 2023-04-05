using System;
using ITBees.UserManager.Interfaces.Services;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ITBees.UserManager.Controllers.GenericControllersAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericRestControllerNameConvention : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (!controller.ControllerType.IsGenericType || 
                (controller.ControllerType.GetGenericTypeDefinition() != typeof(ILoginService<>) &&
                 controller.ControllerType.GetGenericTypeDefinition() != typeof(IConfirmRegistrationService<>)))
            {
                return;
            }
            var entityType = controller.ControllerType.GenericTypeArguments[0];
            controller.ControllerName = entityType.Name;
            controller.RouteValues["Controller"] = entityType.Name;
        }
    }
}