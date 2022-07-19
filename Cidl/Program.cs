using Cidl;
using System.Reflection;
using Text;

var path = args[0];
Console.WriteLine(path);
var fullPath = Path.GetFullPath(path);
Console.WriteLine(fullPath);

var a = Assembly.LoadFile(fullPath);
var library = new Library(a);
library.List().Write("  ", Console.WriteLine);

Console.WriteLine();

var cppText = library.Cpp().Text("    ");

File.WriteAllLines($"{library.Name}.hpp", cppText);
