using System.Collections;
using System.Collections.Generic;

namespace GenericCast
{
    public interface IEnumerableFunc<R>
    {
        R Invoke<T>(IEnumerable<T> v);
    }

    interface IEnumerableWrap
    {
        R Invoke<R>(IEnumerableFunc<R> r);
    }

    public static class EnumerableCast {
        public static R Invoke<R>(this IEnumerable v, IEnumerableFunc<R> r) =>
            CreateWrap(v).Invoke(r);

        sealed class Wrap<T> : IEnumerableWrap
        {
            public IEnumerable<T> value;
            public R Invoke<R>(IEnumerableFunc<R> r) => r.Invoke(value);
        }

        static IEnumerableWrap CreateWrap(IEnumerable v) =>
            CreateGenericWrap((dynamic)v);

        static IEnumerableWrap CreateGenericWrap<T>(IEnumerable<T> value) 
            => new Wrap<T> { value = value };
    }
}
