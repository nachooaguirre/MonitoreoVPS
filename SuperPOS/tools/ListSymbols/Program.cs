using System.IO;
using System.Linq;
using Wpf.Ui.Controls;
var names = Enum.GetNames(typeof(SymbolRegular));
System.IO.File.WriteAllLines(@"C:\Users\ignac\symbols.txt", names.OrderBy(x => x));
Console.WriteLine($"Total: {names.Length} símbolos guardados en C:/symbols.txt");
