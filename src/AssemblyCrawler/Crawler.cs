using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyCrawler
{
    internal class Crawler
    {
        public string Path { get; private set; }
        public int TotalAssemblyCount { get; private set; }
        public int TotalFileCount { get; private set; }

        public List<AssemblyInfo> AssemblyList { get => assemblies.ToList(); }
        private readonly List<AssemblyInfo> assemblies = new List<AssemblyInfo>();

        public Crawler(string path)
        {
            this.Path = path;
            TotalAssemblyCount = 0;
            TotalFileCount = 0;
        }

        public void Crawl()
        {
            DirectoryInfo d = new DirectoryInfo(Path);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            CrawlDirectory(d);
            sw.Stop();

            Console.WriteLine($"Crawled '{Path}' in {sw.ElapsedMilliseconds}ms.");
        }

        public void CrawlDirectory(DirectoryInfo d)
        {
            var files = d.EnumerateFiles();
            var directories = d.EnumerateDirectories();

            var assemblyFiles = files.Where(file => string.Equals(file.Extension, ".dll", StringComparison.OrdinalIgnoreCase) && !file.Name.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase));

            TotalFileCount += files.Count();
            TotalAssemblyCount += assemblyFiles.Count();

            assemblies.AddRange(assemblyFiles.Select(a => new AssemblyInfo(a)));

            if (directories.Any())
            {
                foreach (var directory in directories)
                {
                    CrawlDirectory(directory);
                }
            }
        }
    }
}
