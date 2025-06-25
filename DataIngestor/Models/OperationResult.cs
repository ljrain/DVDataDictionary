using System;

namespace DataIngestor.Models
{
    /// <summary>
    /// Represents the result of an operation with success/failure status and optional error information
    /// </summary>
    /// <typeparam name="T">The type of data returned on success</typeparam>
    public class OperationResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        private OperationResult() { }

        public static OperationResult<T> Success(T data)
        {
            return new OperationResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public static OperationResult<T> Failure(string errorMessage, Exception exception = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Represents the result of an operation without return data
    /// </summary>
    public class OperationResult
    {
        public bool IsSuccess { get; private set; }
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        private OperationResult() { }

        public static OperationResult Success()
        {
            return new OperationResult
            {
                IsSuccess = true
            };
        }

        public static OperationResult Failure(string errorMessage, Exception exception = null)
        {
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
}