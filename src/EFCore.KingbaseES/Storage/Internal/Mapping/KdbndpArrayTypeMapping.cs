﻿using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Kdbndp.EntityFrameworkCore.KingbaseES.Storage.ValueConversion;

namespace Kdbndp.EntityFrameworkCore.KingbaseES.Storage.Internal.Mapping;

/// <summary>
///     Type mapping for KingbaseES arrays.
/// </summary>
/// <remarks>
///     See: https://www.KingbaseES.org/docs/current/static/arrays.html
/// </remarks>
public abstract class KdbndpArrayTypeMapping : RelationalTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected KdbndpArrayTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override RelationalTypeMapping ElementTypeMapping
    {
        get
        {
            var elementTypeMapping = base.ElementTypeMapping;
            Check.DebugAssert(
                elementTypeMapping is not null,
                "KdbndpArrayTypeMapping without an element type mapping");
            Check.DebugAssert(
                elementTypeMapping is RelationalTypeMapping,
                "KdbndpArrayTypeMapping with a non-relational element type mapping");
            return (RelationalTypeMapping)elementTypeMapping;
        }
    }
}

/// <summary>
///     Type mapping for KingbaseES arrays.
/// </summary>
/// <remarks>
///     See: https://www.KingbaseES.org/docs/current/static/arrays.html
/// </remarks>
public class KdbndpArrayTypeMapping<TCollection, TConcreteCollection, TElement> : KdbndpArrayTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static KdbndpArrayTypeMapping<TCollection, TConcreteCollection, TElement> Default { get; }
        = new();

    /// <summary>
    ///     The database type used by Kdbndp.
    /// </summary>
    public virtual KdbndpDbType? KdbndpDbType { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [UsedImplicitly]
    public KdbndpArrayTypeMapping(RelationalTypeMapping elementTypeMapping)
        : this(elementTypeMapping.StoreType + "[]", elementTypeMapping)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [UsedImplicitly]
    public KdbndpArrayTypeMapping(string storeType, RelationalTypeMapping elementTypeMapping)
        : this(CreateParameters(storeType, elementTypeMapping))
    {
        Check.DebugAssert(storeType.EndsWith("[]", StringComparison.Ordinal), "KdbndpArrayTypeMapping created for a non-array store type");
    }

    private static RelationalTypeMappingParameters CreateParameters(string storeType, RelationalTypeMapping elementMapping)
    {
        ValueConverter? converter = null;

        // We do GetElementType for multidimensional arrays - these don't implement generic IEnumerable<>
        var elementType = typeof(TCollection).TryGetElementType(typeof(IEnumerable<>)) ?? typeof(TCollection).GetElementType();

        Check.DebugAssert(elementType is not null, "modelElementType cannot be null");

        if (elementMapping.Converter is { } elementConverter)
        {
            var providerElementType = elementConverter.ProviderClrType;

            // Nullability has been unwrapped on the element converter's provider CLR type, so add it back here if needed
            if (elementType.IsNullableValueType())
            {
                providerElementType = providerElementType.MakeNullable();
            }

            converter = (ValueConverter)Activator.CreateInstance(
                typeof(KdbndpArrayConverter<,,>).MakeGenericType(
                    typeof(TCollection), typeof(TConcreteCollection), providerElementType.MakeArrayType()),
                elementConverter)!;
        }
        else if (typeof(TCollection) != typeof(TConcreteCollection))
        {
            converter = (ValueConverter)Activator.CreateInstance(
                typeof(KdbndpArrayConverter<,,>).MakeGenericType(
                    typeof(TCollection), typeof(TConcreteCollection), elementType.MakeArrayType()))!;
        }

#pragma warning disable EF1001
        var comparer = typeof(TCollection).IsArray && typeof(TCollection).GetArrayRank() > 1
            ? null // TODO: Value comparer for multidimensional arrays
            : (ValueComparer?)Activator.CreateInstance(
                elementType.IsNullableValueType()
                    ? typeof(NullableValueTypeListComparer<>).MakeGenericType(elementType.UnwrapNullableType())
                    : elementMapping.Comparer.Type.IsAssignableFrom(elementType)
                        ? typeof(ListComparer<>).MakeGenericType(elementType)
                        : typeof(ObjectListComparer<>).MakeGenericType(elementType),
                elementMapping.Comparer.ToNullableComparer(elementType)!);
#pragma warning restore EF1001

        var elementJsonReaderWriter = elementMapping.JsonValueReaderWriter;
        if (elementJsonReaderWriter is not null && elementJsonReaderWriter.ValueType != typeof(TElement).UnwrapNullableType())
        {
            throw new InvalidOperationException(
                $"When '{elementJsonReaderWriter.ValueType}', '{typeof(TElement).UnwrapNullableType()}' building an array mapping, the JsonValueReaderWriter for element mapping '{elementMapping.GetType().Name}' is incorrect ('{elementMapping.JsonValueReaderWriter?.GetType().Name ?? "<null>"}').");
        }

        // If there's no JsonValueReaderWriter on the element, we also don't set one on its array (this is for rare edge cases such as
        // KdbndpRowValueTypeMapping).
        // TODO: Also, we don't (yet) support JSON serialization of multidimensional arrays.
        var collectionJsonReaderWriter =
            elementJsonReaderWriter is null || typeof(TCollection).IsArray && typeof(TCollection).GetArrayRank() > 1
                ? null
                : (JsonValueReaderWriter?)Activator.CreateInstance(
                    (elementType.IsNullableValueType()
                        ? typeof(JsonNullableStructCollectionReaderWriter<,,>)
                        : typeof(JsonCollectionReaderWriter<,,>))
                    .MakeGenericType(typeof(TCollection), typeof(TConcreteCollection), elementType.UnwrapNullableType()),
                    elementJsonReaderWriter);

        return new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
                typeof(TCollection), converter, comparer, elementMapping: elementMapping,
                jsonValueReaderWriter: collectionJsonReaderWriter),
            storeType);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected KdbndpArrayTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
        var clrType = parameters.CoreParameters.ClrType;

        if (clrType.TryGetElementType(typeof(IEnumerable<>)) == null && clrType.GetElementType() == null)
        {
            throw new ArgumentException($"CLR type '{parameters.CoreParameters.ClrType}' isn't an IEnumerable");
        }

        // If the element mapping has an KdbndpDbType or DbType, set our own KdbndpDbType as an array of that.
        // Otherwise let the ADO.NET layer infer the KingbaseES type. We can't always let it infer, otherwise
        // when given a byte[] it will infer byte (but we want smallint[])
        KdbndpDbType = KdbndpTypes.KdbndpDbType.Array
            | (ElementTypeMapping is IKdbndpTypeMapping elementKdbndpTypeMapping
                ? elementKdbndpTypeMapping.KdbndpDbType
                : ElementTypeMapping.DbType.HasValue
                    ? new KdbndpParameter { DbType = ElementTypeMapping.DbType.Value }.KdbndpDbType
                    : default(KdbndpDbType?));
    }

    // This constructor exists only to support the static Default property above, which is necessary to allow code generation for compiled
    // models. The constructor creates a completely blank type mapping, which will get cloned with all the correct details.
    private KdbndpArrayTypeMapping()
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(typeof(TCollection), elementMapping: NullMapping),
            "int[]"))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override DbParameter CreateParameter(
        DbCommand command,
        string name,
        object? value,
        bool? nullable = null,
        ParameterDirection direction = ParameterDirection.Input)
    {
        // In queries which compose non-server-correlated LINQ operators over an array parameter (e.g. Where(b => ids.Skip(1)...) we
        // get an enumerable parameter value that isn't an array/list - but those aren't supported at the Kdbndp ADO level.
        // Detect this here and evaluate the enumerable to get a fully materialized List.
        // TODO: Make Kdbndp support IList<> instead of only arrays and List<>
        if (value is not null && !value.GetType().IsArrayOrGenericList())
        {
            switch (value)
            {
                case IEnumerable<TElement> elements:
                    value = elements.ToList();
                    break;

                case IEnumerable elements:
                    value = elements.Cast<TElement>().ToList();
                    break;
            }
        }

        return base.CreateParameter(command, name, value, nullable, direction);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
    {
        Check.DebugAssert(
            parameters.CoreParameters.ClrType == typeof(TCollection), "KdbndpArrayTypeMapping.Clone attempting to change ClrType");
        Check.DebugAssert(
            parameters.CoreParameters.ElementTypeMapping is not null, "KdbndpArrayTypeMapping.Clone without an element type mapping");
        Check.DebugAssert(
            parameters.CoreParameters.ElementTypeMapping.ClrType == typeof(TElement).UnwrapNullableType(),
            "KdbndpArrayTypeMapping.Clone attempting to change element ClrType");

        return new KdbndpArrayTypeMapping<TCollection, TConcreteCollection, TElement>(parameters);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
    {
        if (value is not IEnumerable enumerable)
        {
            throw new ArgumentException($"'{value.GetType().Name}' must be an IEnumerable", nameof(value));
        }

        if (value is Array array && array.Rank != 1)
        {
            throw new NotSupportedException("Multidimensional array literals aren't supported");
        }

        var sb = new StringBuilder();
        sb.Append("ARRAY[");

        var isFirst = true;
        foreach (var element in enumerable)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                sb.Append(",");
            }

            sb.Append(ElementTypeMapping.GenerateProviderValueSqlLiteral(element));
        }

        sb.Append("]::");
        sb.Append(ElementTypeMapping.StoreType);
        sb.Append("[]");
        return sb.ToString();
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
        if (parameter is not KdbndpParameter KdbndpParameter)
        {
            throw new ArgumentException(
                $"Kdbndp-specific type mapping {GetType()} being used with non-Kdbndp parameter type {parameter.GetType().Name}");
        }

        if (KdbndpDbType.HasValue)
        {
            KdbndpParameter.KdbndpDbType = KdbndpDbType.Value;
        }
    }
}
