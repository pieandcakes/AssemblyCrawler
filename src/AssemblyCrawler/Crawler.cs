using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AssemblyCrawler
{
    internal class Crawler
    {
        public string ParsePath { get; private set; }
        public int TotalAssemblyCount { get; private set; }
        public int TotalFileCount { get; private set; }
        public IReadOnlyDictionary<string, Dictionary<string, List<AssemblyInfo>>> AllAssemblies { get { return sortedAssemblies_all.ToImmutableDictionary(); } }
        public IReadOnlyDictionary<string, Dictionary<string, List<AssemblyInfo>>> AllManagedAssemblies { get { return sortedAssemblies_managed.ToImmutableDictionary(); } }


        public List<AssemblyInfo> AssemblyList { get => assemblies.ToList(); }
        private readonly List<AssemblyInfo> assemblies = new List<AssemblyInfo>();
        private Dictionary<string, Dictionary<string, List<AssemblyInfo>>> sortedAssemblies_all = new Dictionary<string, Dictionary<string, List<AssemblyInfo>>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Dictionary<string, List<AssemblyInfo>>> sortedAssemblies_managed = new Dictionary<string, Dictionary<string, List<AssemblyInfo>>>(StringComparer.OrdinalIgnoreCase);


        public Crawler(string path)
        {
            this.ParsePath = path;
            TotalAssemblyCount = 0;
            TotalFileCount = 0;
        }

        public void Crawl()
        {
            if (assemblies.Any())
            {
                Console.WriteLine($"Crawler has already crawled directory '{ParsePath}'.");
                return;
            }

            DirectoryInfo d = new DirectoryInfo(ParsePath);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            CrawlDirectory(d);
            sw.Stop();

            Console.WriteLine($"Crawled '{ParsePath}' in {sw.ElapsedMilliseconds}ms.");
        }

        public void CrawlDirectory(DirectoryInfo d)
        {
            var files = d.EnumerateFiles().ToList();
            var directories = d.EnumerateDirectories().ToList();

            var assemblyFilesList = files.Where(file => string.Equals(file.Extension, ".dll", StringComparison.OrdinalIgnoreCase)
            && !file.Name.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase) // Ignore Resource dlls
            && !file.Attributes.HasFlag(FileAttributes.ReparsePoint)).ToList(); // Ignore symlinks

            TotalFileCount += files.Count;
            TotalAssemblyCount += assemblyFilesList.Count;

            assemblies.AddRange(assemblyFilesList.Select(a => new AssemblyInfo(a)));

            if (directories.Any())
            {
                foreach (var directory in directories)
                {
                    CrawlDirectory(directory);
                }
            }
        }

        public void Sort()
        {
            if (!AssemblyList.Any())
            {
                Console.WriteLine("Must parse directory first.");
                return;
            }

            var assemblies = this.assemblies.ToList();

            Console.WriteLine($"Found in `{ParsePath}`:");
            Console.WriteLine($"    Total Files:      {TotalFileCount}");
            Console.WriteLine($"    Total Assemblies: {TotalAssemblyCount}");

            Console.WriteLine($"Starting sort.");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (var a in assemblies)
            {
                if (!sortedAssemblies_all.ContainsKey(a.FName.Value))
                {
                    sortedAssemblies_all.Add(a.FName.Value, new Dictionary<string, List<AssemblyInfo>>());
                }

                var key2 = a.AName.Value + a.FrameworkVersion.ToString();
                if (!sortedAssemblies_all[a.FName.Value].ContainsKey(key2))
                {
                    sortedAssemblies_all[a.FName.Value].Add(key2, new List<AssemblyInfo>());
                }

                sortedAssemblies_all[a.FName.Value][key2].Add(a);
            }

            var keyList = sortedAssemblies_all.Keys.ToList();
            foreach (var key in keyList)
            {
                var count = 0;
                foreach (var key2 in sortedAssemblies_all[key].Keys.ToList())
                {

                    count += sortedAssemblies_all[key][key2].Count();
                }

                if (count == 1)
                {
                    sortedAssemblies_all.Remove(key);
                }
            }

            sortedAssemblies_managed = new Dictionary<string, Dictionary<string, List<AssemblyInfo>>>(sortedAssemblies_all, StringComparer.OrdinalIgnoreCase);

            keyList = sortedAssemblies_managed.Keys.ToList();
            foreach (var key in keyList)
            {
                foreach (var key2 in sortedAssemblies_managed[key].Keys.ToList())
                {
                    if (sortedAssemblies_managed[key][key2][0].IsManaged.Value == false)
                    {
                        sortedAssemblies_managed.Remove(key);
                        break;
                    }
                }
            }

            Console.WriteLine($"Sort complete in {sw.ElapsedMilliseconds}ms");
        }
    }
}
