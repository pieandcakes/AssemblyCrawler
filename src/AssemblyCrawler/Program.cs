
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AssemblyCrawler
{
    public class Program
    {

        private static Crawler crawler = null;
        private static Dictionary<string, List<AssemblyInfo>> sortedAssemblies_all = null;
        private static Dictionary<string, List<AssemblyInfo>> sortedAssemblies_managed = null;

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

            var assemblies = crawler.AssemblyList;

            Console.WriteLine($"Found in `{crawler.Path}`:");
            Console.WriteLine($"    Total Files:      {crawler.TotalFileCount}");
            Console.WriteLine($"    Total Assemblies: {crawler.TotalAssemblyCount}");

            Console.WriteLine($"Starting sort.");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            sortedAssemblies_all = new Dictionary<string, List<AssemblyInfo>>(StringComparer.OrdinalIgnoreCase);

            foreach (var a in assemblies)
            {
                if (!sortedAssemblies_all.ContainsKey(a.FName.Value))
                {
                    sortedAssemblies_all.Add(a.FName.Value, new List<AssemblyInfo>());
                }

                sortedAssemblies_all[a.FName.Value].Add(a);
            }

            var keyList = sortedAssemblies_all.Keys.ToList();
            foreach (var key in keyList)
            {
                if (sortedAssemblies_all[key].Count() == 1)
                {
                    sortedAssemblies_all.Remove(key);
                }
            }

            sortedAssemblies_managed = new Dictionary<string, List<AssemblyInfo>>(sortedAssemblies_all);

            keyList = sortedAssemblies_managed.Keys.ToList();
            foreach(var key in keyList)
            {
                if (sortedAssemblies_managed[key][0].IsManaged.Value == false)
                {
                    sortedAssemblies_managed.Remove(key);
                }
            }

            Console.WriteLine($"Sort complete in {sw.ElapsedMilliseconds}ms");

        }

        private static Dictionary<string, List<AssemblyInfo>> ListToUse(bool useManaged)
        {
            if (useManaged)
                return sortedAssemblies_managed;
            return sortedAssemblies_all;
        } 

        private static void List()
        {
            if (sortedAssemblies_all == null)
            {
                Console.WriteLine("Must parse a directory first.");
                return;
            }

            bool exit = false;
            bool useManaged = true;

            Console.WriteLine("Use managed only (y/n)? (default:y)");
            var useManagedString = Console.ReadLine();

            if (string.Equals(useManagedString, "n", StringComparison.OrdinalIgnoreCase))
            {
                useManaged = false;
                Console.WriteLine("Using all assemblies list.");
            }
            else
            {
                Console.WriteLine("Using managed only.");
            }

            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("c: Get counts for different number of copies.");
                Console.WriteLine("d: Get details for a specific duplicate set.");
                Console.WriteLine("a: Get assemblyName detail.");
                Console.WriteLine("r: Ryan's list.");
                Console.WriteLine("q: quit");

                string? input = Console.ReadLine();

                switch (input?.ToLower())
                {
                    case "c":
                        GetCount(useManaged);
                        break;
                    case "d":
                        GetDetail(useManaged);
                        break;
                    case "a":
                        GetAssemblyNameDetail(useManaged);
                        break;
                    case "r":
                        GenerateRyansList(useManaged);
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

        private static void GenerateRyansList(bool useManaged)
        {
            var sortedList = ListToUse(useManaged);

            Console.WriteLine("Filename to save it in (empty for output to screen):");
            var filename = Console.ReadLine();

            StreamWriter sw = null;

            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    sw = new StreamWriter(Console.OpenStandardOutput());
                }
                else
                {
                    FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
                    sw = new StreamWriter(fs);
                }

                // write header
                sw.WriteLine("FileName,Count,TotalSizeInMB,IsManaged");
                foreach (var key in sortedList.Keys)
                {
                    ulong sumInMB = 0;
                    foreach (var item in sortedList[key])
                    {
                        sumInMB += item.FileSize.Value;
                    }
                    sw.WriteLine($"{sortedList[key][0].FName},{sortedList[key].Count()},{sumInMB.ToString()},{sortedList[key][0].IsManaged.Value}");
                }
            }
            finally
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                }
            }
        }

        private static void GetCount(bool useManaged)
        {
            var sortedList = ListToUse(useManaged);

            Console.WriteLine("Enter max count:");
            var maxCount = Console.ReadLine();

            if (!Int32.TryParse(maxCount, out int max))
            {
                Console.WriteLine("Number not entered.");
                return;
            }

            for (int i = 1; i <= max; i++)
            {
                var keyItems = sortedList.Where(item => item.Value.Count() == i);
                if (keyItems.Any())
                    Console.WriteLine($"Items with {i} instance(s): {keyItems.Count()}");
            }

            var manyKeyItems = sortedList.Where(item => item.Value.Count() > max);
            Console.WriteLine($"Items with more than {max} instance(s): {manyKeyItems.Count()}");
        }

        private static void GetDetail(bool useManaged)
        {
            var sortedList = ListToUse(useManaged);

            Console.WriteLine("Enter target count for details:");
            var countString = Console.ReadLine();
            if (!Int32.TryParse(countString, out int count))
            {
                Console.WriteLine("Number not entered.");
                return;
            }

            var keyItems = sortedList.Where(item => item.Value.Count() == count).ToList();
            var keyItemCount = keyItems.Count();
            Console.WriteLine($"{keyItemCount} itemsfound for target count {count}.");
            for (int i = 0; i < keyItemCount; i++)
            {
                Console.WriteLine($"  {keyItems[i].Key}");
            }

            Console.WriteLine();
            Console.WriteLine("Complete");
        }

        private static void GetAssemblyNameDetail(bool useManaged)
        {
            var sortedList = ListToUse(useManaged);

            Console.WriteLine("Enter assembly name for details:");
            var assemblyName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(assemblyName) || !sortedList.ContainsKey(assemblyName))
            {
                Console.WriteLine("Invalid assemblyname or assemblyname not found.");
                return;
            }

            var assemblyNameDetails = sortedList[assemblyName];
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
                Console.WriteLine($"fq: {samesies[key][0].AName.Value}, av: {samesies[key][0].AssemblyVersion.Value}, fv: {samesies[key][0].FileVersion.Value}, ct: {samesies[key].Count()}, sz:{samesies[key][0].FileSize.Value.ToString("N0")}, tot: {(samesies[key][0].FileSize.Value * (ulong)samesies[key].Count()).ToString("N0")}");

                foreach (var v in samesies[key].ToList())
                {
                    Console.WriteLine($"  {v.Path}");
                }
            }
        }
    }
}