using System.Diagnostics.CodeAnalysis;

#pragma warning disable S101
#pragma warning disable S1694

namespace CdxEnrich.FunctionalHelpers
{
    public abstract class Result<TData>
    {
        public abstract TData Data { get; }
    }



    public interface Success { }

    public interface Failure
    {
        public IErrorType ErrorType { get; }

        public string ErrorMessage { get; }
    }
    public interface IErrorType
    {
        public string ErrorMessage { get; }
    }

    public class Ok<TData>(TData data) : Result<TData>, Success
    {
        public override TData Data => data;
    }
    public class Error<TData> : Result<TData>, Failure
    {
        public IErrorType ErrorType { get; }
        public string ErrorMessage => ErrorType.ErrorMessage;


        public override TData Data => throw new InvalidOperationException("This Result is not a success. Always make sure you first check its type!");

        public Error(IErrorType error)
        {
            ErrorType = error;
        }

        public Error(Failure result)
        {
            ErrorType = result.ErrorType;
        }

        public static Error<T> From<T>(Failure input)
        {
            return new Error<T>(input.ErrorType);
        }
    }
    public static class Error
    {
        public static Error<T> From<T>(Failure input)
        {
            return new Error<T>(input.ErrorType);
        }
    }

}
#pragma warning restore S101
#pragma warning restore S1694