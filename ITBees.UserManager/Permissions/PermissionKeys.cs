using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ITBees.UserManager.Permissions;

public static class PermissionKeys
{
    /// <summary>
    /// Every public string constant of the type and of its nested types, in declaration order - so the class of
    /// constants used in [HasPermission] is also the catalog, with no second list to keep in sync.
    /// </summary>
    public static IEnumerable<string> FromConstants(Type constantsType)
    {
        var keys = constantsType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue());

        return keys.Concat(constantsType.GetNestedTypes(BindingFlags.Public).SelectMany(FromConstants)).ToList();
    }
}
