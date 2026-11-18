namespace CDMS.Core.Models;

/// <summary>
/// A discriminated result type for connector operations, encapsulating either a
/// successful value or a list of structured errors. Inspired by the Railway-Oriented
/// Programming pattern to avoid exception-based control flow for expected failure modes.
/// </summary>
/// <typeparam name="T">The type of the value on the success path.</typeparam>
public sealed class ConnectorResult<T>
{
    private ConnectorResult() { }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Gets the value returned on the success path.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException(
                    "Cannot access Value on a failed ConnectorResult. Check IsSuccess first.");
            return _value!;
        }
    }

    private T? _value;

    /// <summary>Gets the list of errors returned on the failure path (empty when successful).</summary>
    public IReadOnlyList<ConnectorError> Errors { get; private init; } = Array.Empty<ConnectorError>();

    /// <summary>Gets the first error message, or <c>null</c> when the result is successful.</summary>
    public string? FirstError => Errors.Count > 0 ? Errors[0].Message : null;

    // ── Factory methods ──────────────────────────────────────────────────────

    /// <summary>Creates a successful result wrapping the given value.</summary>
    /// <param name="value">The operation's return value.</param>
    public static ConnectorResult<T> Ok(T value) =>
        new() { IsSuccess = true, _value = value, Errors = Array.Empty<ConnectorError>() };

    // Note: _value backing field not set (stays null) on failure — this is intentional.
    // The Value property throws if accessed.

    /// <summary>Creates a failed result with the specified errors.</summary>
    /// <param name="errors">One or more errors describing why the operation failed.</param>
    public static ConnectorResult<T> Fail(IEnumerable<ConnectorError> errors) =>
        new() { IsSuccess = false, Errors = errors.ToList().AsReadOnly() };

    /// <summary>Creates a failed result with a single error.</summary>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <param name="message">Human-readable error description.</param>
    /// <param name="isTransient">
    /// <c>true</c> when the error is transient and the operation may be retried.
    /// </param>
    public static ConnectorResult<T> Fail(string errorCode, string message, bool isTransient = false) =>
        Fail(new[] { new ConnectorError(errorCode, message, isTransient) });

    /// <summary>
    /// Projects the value of a successful result through a transformation function.
    /// If the result is a failure the errors are propagated without calling <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="mapper">The transformation function.</param>
    public ConnectorResult<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess
            ? ConnectorResult<TOut>.Ok(mapper(Value))
            : ConnectorResult<TOut>.Fail(Errors);

    /// <inheritdoc/>
    public override string ToString() =>
        IsSuccess
            ? $"Ok({Value})"
            : $"Fail([{string.Join(", ", Errors.Select(e => e.ErrorCode))}])";
}

/// <summary>
/// Describes a single error produced by a connector operation.
/// </summary>
/// <param name="ErrorCode">Machine-readable error code (e.g. <c>RATE_LIMITED</c>, <c>NOT_FOUND</c>).</param>
/// <param name="Message">Human-readable description of the error.</param>
/// <param name="IsTransient">
/// <c>true</c> when the error is transient (network timeout, rate limit) and
/// the operation can be retried after a back-off; <c>false</c> for permanent failures.
/// </param>
/// <param name="HttpStatusCode">The HTTP status code returned by the external API, if applicable.</param>
public sealed record ConnectorError(
    string ErrorCode,
    string Message,
    bool IsTransient = false,
    int? HttpStatusCode = null);
