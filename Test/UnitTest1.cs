using GenericCast;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class UnitTest1
    {
        class GetItemTypeName : IEnumerableFunc<string>
        {
            public string Invoke<T>(IEnumerable<T> v)
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
