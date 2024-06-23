namespace CdxEnrich.FunctionalHelpers
{
    public static class FunctionalHelper
    {
        public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> func)
        {
            return result switch
            {
                Ok<T> ok => func(ok.Data),
                Error<T> error => Error.From<U>(error),
                _ => throw new InvalidOperationException("Unknown result type")
            };
        }

        public static Result<TOutput> Map<TInput, TOutput>(this Result<TInput> input, Func<TInput, TOutput> fun)
        {
            return input switch
            {
                Ok<TInput> ok => new Ok<TOutput>(fun(ok.Data)),
                Error<TInput> error => Error.From<TOutput>(error),
                _ => throw new InvalidOperationException("Unknown result type")
            };
        }

        public static Result<T> Tee<T>(this Result<T> input, Action<T> action)
        {
            if (input is Ok<T> ok)
            {
                action(ok.Data);
            }
            return input;
        }

        public static Result<V> SelectMany<T, U, V>(this Result<T> result, Func<T, Result<U>> func, Func<T, U, V> select)
        {
            return result.Bind(t => func(t).Map(u => select(t, u)));
        }

        public static Result<T> WriteError<T>(this Result<T> result)
        {
            if (result is Failure failure)
            {
                Console.WriteLine(failure.ErrorMessage);
            }
            return result;
        }
    }
}
