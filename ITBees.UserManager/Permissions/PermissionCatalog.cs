using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ITBees.UserManager.Permissions;

/// <summary>
/// Collects the permission keys of all registered contributors and refuses an invalid catalog, so a typo or a
/// duplicated key stops the application instead of silently granting or denying access.
/// </summary>
public class PermissionCatalog : IPermissionCatalog
{
    private static readonly Regex KeyFormat = new(@"^[a-z][a-zA-Z0-9]*(\.[a-z][a-zA-Z0-9]*)+$");

    private readonly List<string> _keys;
    private readonly HashSet<string> _keySet;

    public PermissionCatalog(IEnumerable<IPermissionCatalogContributor> contributors)
    {
        _keys = contributors.SelectMany(x => x.GetPermissionKeys()).ToList();
        Validate(_keys);
        _keySet = new HashSet<string>(_keys, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> AllKeys => _keys;

    public bool Contains(string permissionKey) => permissionKey != null && _keySet.Contains(permissionKey);

    private static void Validate(List<string> keys)
    {
        var errors = keys
            .Where(x => x == null || KeyFormat.IsMatch(x) == false)
            .Select(x => $"Invalid permission key '{x}' (expected e.g. 'app.area.action').")
            .Concat(keys
                .Where(x => x != null)
                .GroupBy(x => x, StringComparer.Ordinal)
                .Where(x => x.Count() > 1)
                .Select(x => $"Permission key '{x.Key}' is declared {x.Count()} times."))
            .ToList();

        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid permission catalog: " + string.Join(" ", errors));
    }
}
