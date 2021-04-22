using System.Collections;
using System.Collections.Generic;

namespace GenericCast
{
    public interface IEnumerableFunc<R>
    {
        R Do<T>(IEnumerable<T> v);
    }

    public static class EnumerableCast {
        public static R Invoke<R>(this IEnumerable value, IEnumerableFunc<R> func)
        {
            IWrap wrap = CreateWrap((dynamic)value);
            return wrap.Invoke(func);
        }

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
