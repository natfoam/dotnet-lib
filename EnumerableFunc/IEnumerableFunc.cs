using System.Collections;
using System.Collections.Generic;

namespace EnumerableFunc
{
    /// <summary>
    /// A function which can be applied to any IEnumerable<...> collection.
    /// </summary>
    /// <typeparam name="R">A type of the result of the function.</typeparam>
    public interface IEnumerableFunc<R>
    {
        R Do<T>(IEnumerable<T> v);
    }

    public static class EnumerableFuncEx {
        public static R Invoke<R>(this IEnumerable value, IEnumerableFunc<R> func) 
            => CreateWrap((dynamic)value).Invoke(func);

        interface IWrap
        {
            R Invoke<R>(IEnumerableFunc<R> r);
        }

        sealed class Wrap<T> : IWrap
        {
            public IEnumerable<T> value;
            public R Invoke<R>(IEnumerableFunc<R> func) => func.Do(value);
        }

        static IWrap CreateWrap<T>(IEnumerable<T> value) 
            => new Wrap<T> { value = value };
    }
}
