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
        public static void Write(this IEnumerable<Item> list, string indent)
        {
            foreach (var item in list)
            {
                item.Write(indent);
            }
        }

        public static void Write(this Item item, string indent)
        {
            foreach (var line in item.Text(indent))
            {
                Console.WriteLine(line);
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
