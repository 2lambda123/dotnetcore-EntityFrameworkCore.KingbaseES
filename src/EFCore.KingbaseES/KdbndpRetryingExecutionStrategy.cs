﻿using Kdbndp.EntityFrameworkCore.KingbaseES.Storage.Internal;

namespace Kdbndp.EntityFrameworkCore.KingbaseES;

/// <summary>
///     An <see cref="IExecutionStrategy" /> implementation for retrying failed executions on KingbaseES.
/// </summary>
/// <remarks>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each <see cref="DbContext" /> instance will use
///         its own instance of this service. The implementation may depend on other services registered with any lifetime. The
///         implementation does not need to be thread-safe.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see> for more
///         information and examples.
///     </para>
/// </remarks>
public class KdbndpRetryingExecutionStrategy : ExecutionStrategy
{
    private readonly ICollection<string>? _additionalErrorCodes;

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="context"> The context on which the operations will be invoked. </param>
    /// <remarks>
    ///     The default retry limit is 6, which means that the total amount of time spent before failing is about a minute.
    /// </remarks>
    public KdbndpRetryingExecutionStrategy(
        DbContext context)
        : this(context, DefaultMaxRetryCount)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    public KdbndpRetryingExecutionStrategy(
        ExecutionStrategyDependencies dependencies)
        : this(dependencies, DefaultMaxRetryCount)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="context"> The context on which the operations will be invoked. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    public KdbndpRetryingExecutionStrategy(
        DbContext context,
        int maxRetryCount)
        : this(context, maxRetryCount, DefaultMaxDelay, errorCodesToAdd: null)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    public KdbndpRetryingExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount)
        : this(dependencies, maxRetryCount, DefaultMaxDelay, errorCodesToAdd: null)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    /// <param name="errorCodesToAdd"> Additional error codes that should be considered transient. </param>
    public KdbndpRetryingExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        ICollection<string>? errorCodesToAdd)
    : this(dependencies, DefaultMaxRetryCount, DefaultMaxDelay, errorCodesToAdd)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="context"> The context on which the operations will be invoked. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    /// <param name="maxRetryDelay"> The maximum delay between retries. </param>
    /// <param name="errorCodesToAdd"> Additional error codes that should be considered transient. </param>
    public KdbndpRetryingExecutionStrategy(
        DbContext context,
        int maxRetryCount,
        TimeSpan maxRetryDelay,
        ICollection<string>? errorCodesToAdd)
    : base(
        context,
        maxRetryCount,
        maxRetryDelay)
    {
        _additionalErrorCodes = errorCodesToAdd;
    }

    /// <summary>
    ///     Creates a new instance of <see cref="KdbndpRetryingExecutionStrategy" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing service dependencies. </param>
    /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
    /// <param name="maxRetryDelay"> The maximum delay between retries. </param>
    /// <param name="errorCodesToAdd"> Additional SQL error numbers that should be considered transient. </param>
    public KdbndpRetryingExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount,
        TimeSpan maxRetryDelay,
        ICollection<string>? errorCodesToAdd)
    : base(dependencies, maxRetryCount, maxRetryDelay)
    {
        _additionalErrorCodes = errorCodesToAdd;
    }

    // TODO: Unlike SqlException, which seems to also wrap various transport/IO errors
    // and expose them via error codes, we have KdbndpException with an inner exception.
    // Would be good to provide a way to add these into the additional list.
    /// <inheritdoc />
    protected override bool ShouldRetryOn(Exception? exception)
    => exception is KdbndpException KdbndpException && _additionalErrorCodes?.Contains(KdbndpException.SqlState!) == true
    || KdbndpTransientExceptionDetector.ShouldRetryOn(exception);
}
