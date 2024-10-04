
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

            c.Sort();
        }

        private static IReadOnlyDictionary<string, Dictionary<string, List<AssemblyInfo>>> ListToUse(bool useManaged)
        {
            if (useManaged)
                return crawler.AllManagedAssemblies;
            return crawler.AllAssemblies;
        } 

        private static void List()
        {
            if (crawler == null)
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
                    case "s":
                        CreateSymLinkForAssembly();
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

        private static void CreateSymLinkForAssembly()
        {
            //// only symlink managed assemblies
            //var list = ListToUse(true);
            //Console.WriteLine("Enter assembly name for SymLinking:");
            //var assemblyName = Console.ReadLine();
            //if (string.IsNullOrWhiteSpace(assemblyName) || !list.Keys.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
            //{
            //    Console.WriteLine("Invalid assemblyname or assemblyname not found. Only Managed Assembly names supported.");
            //    return;
            //}

            //crawler.CreateSymlinks(assemblyName);

        }

        private static void GenerateRyansList(bool useManaged)
        {
            var sortedList = ListToUse(useManaged);

            Console.WriteLine("Filename to save it in (empty for output to screen):");
            var filename = Console.ReadLine();

            StreamWriter? sw = null;

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
                sw.WriteLine("FileName,AssemblyName,Count,TotalSizeInMB,IsManaged,FilePath,Size");
                foreach (var key in sortedList.Keys)
                {
                    ulong sumInMB = 0;
                    foreach (var key2 in sortedList[key].Keys.ToList())
                    {
                        foreach (var item in sortedList[key][key2])
                        {
                            sumInMB += item.FileSize.Value;
                        }
                        var prefix = $"{sortedList[key][key2][0].FName},{key2.Replace(',', ' ')},{sortedList[key][key2].Count()},{sumInMB.ToString()},{sortedList[key][key2][0].IsManaged.Value}";
                        foreach (var a in sortedList[key][key2])
                        {
                            sw.WriteLine($"{prefix},{a.Path},{a.FileSize}");
                        }
                    }
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

            foreach (var key2 in assemblyNameDetails.Keys.ToList())
            {
                var assemblyByHashCode = assemblyNameDetails[key2].SortByHashCode();
                Console.WriteLine();
                Console.WriteLine("Matching assembly order:");
                foreach (var key in assemblyByHashCode.Keys)
                {
                    Console.WriteLine($"fq: {assemblyByHashCode[key][0].AName.Value}, av: {assemblyByHashCode[key][0].AssemblyVersion.Value}, fv: {assemblyByHashCode[key][0].FileVersion.Value}, ct: {assemblyByHashCode[key].Count()}, sz:{assemblyByHashCode[key][0].FileSize.Value.ToString("N0")}, tot: {(assemblyByHashCode[key][0].FileSize.Value * (ulong)assemblyByHashCode[key].Count()).ToString("N0")}");

                    foreach (var v in assemblyByHashCode[key].ToList())
                    {
                        Console.WriteLine($"  {v.Path}");
                    }
                }
            }
        }
    }
}