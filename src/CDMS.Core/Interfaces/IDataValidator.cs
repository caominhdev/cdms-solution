namespace CDMS.Core.Interfaces;

/// <summary>
/// Validates an incoming data object before it is mapped and persisted.
/// Implementations apply business rules, schema constraints, and tenant-specific
/// validation logic.
/// </summary>
/// <typeparam name="T">The type of the object to validate.</typeparam>
public interface IDataValidator<T>
{
    /// <summary>
    /// Validates the specified item and returns a result containing any validation errors.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> that is <see cref="ValidationResult.IsValid"/> when
    /// the item passes all checks, or contains a list of <see cref="ValidationError"/> objects
    /// describing the failures.
    /// </returns>
    Task<ValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default);
}

/// <summary>
/// The outcome of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    private ValidationResult() { }

    /// <summary>Gets a value indicating whether validation passed with no errors.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Gets the list of validation errors, if any.</summary>
    public IReadOnlyList<ValidationError> Errors { get; private init; } = Array.Empty<ValidationError>();

    /// <summary>Creates a successful validation result.</summary>
    public static ValidationResult Success() => new() { Errors = Array.Empty<ValidationError>() };

    /// <summary>Creates a failed validation result with the specified errors.</summary>
    /// <param name="errors">One or more validation errors.</param>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) =>
        new() { Errors = errors.ToList().AsReadOnly() };

    /// <summary>Creates a failed validation result with a single error.</summary>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="fieldName">Optional field name that caused the failure.</param>
    public static ValidationResult Failure(string errorCode, string message, string? fieldName = null) =>
        Failure(new[] { new ValidationError(errorCode, message, fieldName) });
}

/// <summary>
/// Describes a single validation failure.
/// </summary>
/// <param name="ErrorCode">Machine-readable error code (e.g. <c>MISSING_SKU</c>).</param>
/// <param name="Message">Human-readable description of the failure.</param>
/// <param name="FieldName">The name of the field that failed, or <c>null</c> for object-level failures.</param>
public sealed record ValidationError(string ErrorCode, string Message, string? FieldName = null);
