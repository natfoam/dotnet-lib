namespace Text
{
    abstract class Item
    {
        public abstract IEnumerable<string> Text(string indent, string offset = "");
    }

    sealed class Line : Item
    {
        public readonly string Value;

        public Line(string value)
        {
            Value = value;
        }

        public override IEnumerable<string> Text(string indent, string offset)
        {
            yield return offset + Value;
        }
    }

    sealed class Block : Item
    {
        public readonly IEnumerable<Item> ItemList;

        public Block(IEnumerable<Item> itemList)
        {
            ItemList = itemList;
        }

        public override IEnumerable<string> Text(string indent, string offset)
            => ItemList.SelectMany(item => item.Text(indent, offset + indent));
    }

    static class TextEx
    {
        public static IEnumerable<string> Text(this IEnumerable<Item> list, string indent)
            => list.SelectMany(i => i.Text(indent));

        public static void Write(this IEnumerable<Item> list, string indent, Action<string> write)
        {
            foreach (var item in list.Text(indent))
            {
                write(item);
            }
        }

        static readonly Line CurlyOpen = new Line("{");
        static readonly Line CurlyClose = new Line("}");

        public static IEnumerable<Item> Curly(this Block block, string header)
        {
            yield return new Line(header);
            yield return CurlyOpen;
            yield return block;
            yield return CurlyClose;
        }
    }
}
