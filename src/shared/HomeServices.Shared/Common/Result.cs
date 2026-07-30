namespace HomeServices.Shared.Common;

/// <summary>
/// A generic operation result envelope used to transfer outcomes across service boundaries
/// without throwing exceptions. Encapsulates success/failure state, an optional message,
/// and a collection of validation/error entries.
/// </summary>
public class Result
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public Result(bool succeeded, string? message = null, List<string>? errors = null)
    {
        Succeeded = succeeded;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    public static Result Success(string? message = null) => new(true, message);
    public static Result Failure(string message) => new(false, message);
    public static Result Failure(List<string> errors) => new(false, null, errors);

    public static Result<T> Success<T>(T data, string? message = null) => new(data, true, message);
    public static Result<T> Failure<T>(string message) => new(default, false, message);
}

/// <summary>
/// Generic result that carries a payload of type <typeparamref name="T"/>.
/// </summary>
public class Result<T> : Result
{
    public T? Data { get; set; }

    public Result(T? data, bool succeeded, string? message = null, List<string>? errors = null)
        : base(succeeded, message, errors)
    {
        Data = data;
    }
}
