﻿namespace Kdbndp.EntityFrameworkCore.KingbaseES.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core
///     infrastructure and not subject to the same compatibility standards as
///     public APIs. It may be changed or removed without notice in any release.
///     You should only use it directly in your code with extreme caution and
///     knowing that doing so can result in application failures when updating
///     to a new Entity Framework Core release.
/// </summary>
public class KdbndpSqlTranslatingExpressionVisitorFactory
    : IRelationalSqlTranslatingExpressionVisitorFactory {
  private readonly RelationalSqlTranslatingExpressionVisitorDependencies
      _dependencies;

  /// <summary>
  ///     This is an internal API that supports the Entity Framework Core
  ///     infrastructure and not subject to the same compatibility standards as
  ///     public APIs. It may be changed or removed without notice in any
  ///     release. You should only use it directly in your code with extreme
  ///     caution and knowing that doing so can result in application failures
  ///     when updating to a new Entity Framework Core release.
  /// </summary>
  public KdbndpSqlTranslatingExpressionVisitorFactory(
      RelationalSqlTranslatingExpressionVisitorDependencies dependencies) {
    _dependencies = dependencies;
  }

  /// <summary>
  ///     This is an internal API that supports the Entity Framework Core
  ///     infrastructure and not subject to the same compatibility standards as
  ///     public APIs. It may be changed or removed without notice in any
  ///     release. You should only use it directly in your code with extreme
  ///     caution and knowing that doing so can result in application failures
  ///     when updating to a new Entity Framework Core release.
  /// </summary>
  public virtual RelationalSqlTranslatingExpressionVisitor Create(
      QueryCompilationContext queryCompilationContext,
      QueryableMethodTranslatingExpressionVisitor
          queryableMethodTranslatingExpressionVisitor) => new KdbndpSqlTranslatingExpressionVisitor(_dependencies,
                                                                                                    queryCompilationContext,
                                                                                                    queryableMethodTranslatingExpressionVisitor);
}
