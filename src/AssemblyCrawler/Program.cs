
namespace AssemblyCrawler
{
    public class Program
    {

        private static Crawler crawler = null;
        private static Dictionary<string, List<AssemblyInfo>> sortedAssemblies;

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.Error.WriteLine("Argument count not correct");
                return;
            }

            InnerLoop();
            return;
        }

        public static void InnerLoop(string? path = null)
        {
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("Options:");
                Console.WriteLine("p: parse a directory");
                Console.WriteLine("l: list");
                Console.WriteLine("q: quit");
                string? input = Console.ReadLine();


                switch (input?.ToLower())
                {
                    case "p":
                        Parse();
                        break;
                    case "l":
                        List();
                        break;
                    case "q":
                        Console.WriteLine("Quit chosen");
                        exit = true;
                        break;
                    default:
                        Console.WriteLine($"Invalid option '{input}'.");
                        Console.WriteLine();
                        break;
                }
            }
        }

        private static void Parse()
        {
            Console.WriteLine();
            Console.WriteLine("Enter path to parse:");
            string path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                Console.WriteLine($"Error: invalid path '{path}'");
                return;
            }
            Crawler c = new Crawler(path);
            c.Crawl();
            crawler = c;

            Sort();
        }

        private static void Sort()
        {
            if (crawler == null)
            {
                Console.WriteLine("Must parse directory first.");
                return;
            }

            Console.WriteLine($"Found in `{crawler.Path}`:");
            Console.WriteLine($"    Total Files:      {crawler.TotalFileCount}");
            Console.WriteLine($"    Total Assemblies: {crawler.TotalAssemblyCount}");

            var assemblies = crawler.AssemblyList;
            sortedAssemblies = new Dictionary<string, List<AssemblyInfo>>(StringComparer.OrdinalIgnoreCase);

            foreach (var a in assemblies)
            {
                if (!sortedAssemblies.ContainsKey(a.Name.Value))
                {
                    sortedAssemblies.Add(a.Name.Value, new List<AssemblyInfo>());
                }
                sortedAssemblies[a.Name.Value].Add(a);
            }

        }

        private static void List()
        {
            if (sortedAssemblies == null)
            {
                Console.WriteLine("Must parse a directory first.");
                return;
            }

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("c: Get counts for different number of copies.");
                Console.WriteLine("d: Get details for a specific duplicate set.");
                Console.WriteLine("a: Get assemblyName detail.");
                Console.WriteLine("q: quit");

                string? input = Console.ReadLine();

                switch (input?.ToLower())
                {
                    case "c":
                        GetCount();
                        break;
                    case "d":
                        GetDetail();
                        break;
                    case "a":
                        GetAssemblyNameDetail();
                        break;
                    case "q":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine($"Invalid option '{input}'.");
                        Console.WriteLine();
                        break;
                }
            }
        }

        private static void GetCount()
        {
            Console.WriteLine("Enter max count:");
            var maxCount = Console.ReadLine();

            if (!Int32.TryParse(maxCount, out int max))
            {
                Console.WriteLine("Number not entered.");
                return;
            }

            for (int i = 1; i <= max; i++)
            {
                var keyItems = sortedAssemblies.Where(item => item.Value.Count() == i);
                if (keyItems.Any())
                    Console.WriteLine($"Items with {i} instance(s): {keyItems.Count()}");
            }

            var manyKeyItems = sortedAssemblies.Where(item => item.Value.Count() > max);
            Console.WriteLine($"Items with more than {max} instance(s): {manyKeyItems.Count()}");
        }

        private static void GetDetail()
        {
            Console.WriteLine("Enter target count for details:");
            var countString = Console.ReadLine();
            if (!Int32.TryParse(countString, out int count))
            {
                Console.WriteLine("Number not entered.");
                return;
            }

            var keyItems = sortedAssemblies.Where(item => item.Value.Count() == count).ToList();
            var keyItemCount = keyItems.Count();
            Console.WriteLine($"{keyItemCount} itemsfound for target count {count}.");
            for (int i = 0; i < keyItemCount; i++)
            {
                Console.WriteLine($"  {keyItems[i].Key}");
            }

            Console.WriteLine();
            Console.WriteLine("Complete");
        }

        private static void GetAssemblyNameDetail()
        {
            Console.WriteLine("Enter assembly name for details:");
            var assemblyName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(assemblyName) || !sortedAssemblies.ContainsKey(assemblyName))
            {
                Console.WriteLine("Invalid assemblyname or assemblyname not found.");
                return;
            }

            var assemblyNameDetails = sortedAssemblies[assemblyName];
            Console.WriteLine();
            Console.WriteLine($"'{assemblyName}' has {assemblyNameDetails.Count()} instances.");

            Dictionary<int, List<AssemblyInfo>> samesies = new Dictionary<int, List<AssemblyInfo>>();

            foreach (var item in assemblyNameDetails.OrderByDescending(a => string.Concat(a.AssemblyVersion.Value.ToString(), a.FileVersion.Value.ToString())))
            {
                //Console.WriteLine($"{item.Path}");
                //Console.WriteLine($"  av:'{item.AssemblyVersion.Value}'");
                //Console.WriteLine($"  fv:'{item.FileVersion.Value}'");
                //Console.WriteLine($"  sz:'{item.FileSize.Value.ToString("N0")}'");

                int hc = item.GetHashCode();
                if (!samesies.ContainsKey(hc)) samesies[hc] = new List<AssemblyInfo>();
                samesies[hc].Add(item);
            }

            Console.WriteLine();
            Console.WriteLine("Matching assembly order:");
            foreach (var key in samesies.Keys)
            {
                Console.WriteLine($"av: {samesies[key][0].AssemblyVersion.Value}, fv: {samesies[key][0].FileVersion.Value}, sz:{samesies[key][0].FileSize.Value.ToString("N0")}");
                foreach (var v in samesies[key].ToList())
                {
                    Console.WriteLine($"  {v.Path}");
                }
            }
        }
    }
}