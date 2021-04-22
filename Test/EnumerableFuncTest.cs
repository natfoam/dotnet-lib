using EnumerableFunc;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class EnumerableFuncTest
    {
        class GetItemTypeName : IEnumerableFunc<string>
        {
            public string Do<T>(IEnumerable<T> v)
            {
                return typeof(T).Name;
            }
        }

        [Fact]
        public void Test1()
        {
            var list = new List<int>();
            var name = list.Invoke(new GetItemTypeName());
            Assert.Equal("Int32", name);
        }
    }
}
