namespace Kdbndp.EntityFrameworkCore.KingbaseES.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core
///     infrastructure and not subject to the same compatibility standards as
///     public APIs. It may be changed or removed without notice in any release.
///     You should only use it directly in your code with extreme caution and
///     knowing that doing so can result in application failures when updating
///     to a new Entity Framework Core release.
/// </summary>
public class KdbndpQueryCompilationContext : RelationalQueryCompilationContext {
  /// <summary>
  ///     This is an internal API that supports the Entity Framework Core
  ///     infrastructure and not subject to the same compatibility standards as
  ///     public APIs. It may be changed or removed without notice in any
  ///     release. You should only use it directly in your code with extreme
  ///     caution and knowing that doing so can result in application failures
  ///     when updating to a new Entity Framework Core release.
  /// </summary>
  public KdbndpQueryCompilationContext(
      QueryCompilationContextDependencies dependencies,
      RelationalQueryCompilationContextDependencies relationalDependencies,
      bool async)
      : base(dependencies, relationalDependencies, async) {}

  /// <summary>
  ///     This is an internal API that supports the Entity Framework Core
  ///     infrastructure and not subject to the same compatibility standards as
  ///     public APIs. It may be changed or removed without notice in any
  ///     release. You should only use it directly in your code with extreme
  ///     caution and knowing that doing so can result in application failures
  ///     when updating to a new Entity Framework Core release.
  /// </summary>
  public override bool IsBuffering =>
      base.IsBuffering ||
      QuerySplittingBehavior ==
          Microsoft.EntityFrameworkCore.QuerySplittingBehavior.SplitQuery;
}
