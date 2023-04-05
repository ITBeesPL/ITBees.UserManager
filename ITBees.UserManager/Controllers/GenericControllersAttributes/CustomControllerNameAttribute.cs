using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ITBees.UserManager.Controllers.GenericControllersAttributes
{
    public class CustomControllerNameAttribute : Attribute, IControllerModelConvention
    {
        private readonly string _controllerName;

        public CustomControllerNameAttribute(string controllerName)
        {
            _controllerName = controllerName;
        }

        public void Apply(ControllerModel controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            controller.ControllerName = _controllerName;
        }
    }
}