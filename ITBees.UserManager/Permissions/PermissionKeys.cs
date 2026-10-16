using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ITBees.UserManager.Permissions;

public static class PermissionKeys
{
    /// <summary>
    /// Every public string constant of the type and of its nested types - so the class of constants used in
    /// [HasPermission] is also the catalog, with no second list to keep in sync. Reflection does not promise any
    /// order, so members are ordered by their metadata token, which keeps the declaration order of a source file.
    /// </summary>
    public static IEnumerable<string> FromConstants(Type constantsType)
    {
        var keys = constantsType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral && x.FieldType == typeof(string))
            .OrderBy(x => x.MetadataToken)
            .Select(x => (string)x.GetRawConstantValue());

        var nestedKeys = constantsType.GetNestedTypes(BindingFlags.Public)
            .OrderBy(x => x.MetadataToken)
            .SelectMany(FromConstants);

        return keys.Concat(nestedKeys).ToList();
    }
}
