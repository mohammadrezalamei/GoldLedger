namespace GoldLedger.Domain.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string? Message { get; }
        public ErrorType ErrorType { get; }

        protected Result(bool isSuccess, string? message, ErrorType errorType)
        {
            IsSuccess = isSuccess;
            Message = message;
            ErrorType = errorType;
        }

        public static Result Success(string? message = null) => new(true, message, ErrorType.None);

        public static Result Failure(string errorMessage, ErrorType errorType)
            => new(false, errorMessage, errorType);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(T? value, bool isSuccess, string? message, ErrorType errorType)
            : base(isSuccess, message, errorType)
        {
            Value = value;
        }

        public static Result<T> Success(T value, string? message = null) => new(value, true, message, ErrorType.None);

        public new static Result<T> Failure(string errorMessage, ErrorType errorType)
            => new(default, false, errorMessage, errorType);
    }
}

