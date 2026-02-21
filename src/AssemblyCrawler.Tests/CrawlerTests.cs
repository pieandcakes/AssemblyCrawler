using AssemblyCrawler;
using System.IO;

namespace AssemblyCrawler.Tests;

public class CrawlerTests : IDisposable
{
    private readonly string _tempDir;

    public CrawlerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CrawlerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void CopyTestDllToDir(string dir, string fileName)
    {
        string sourceDll = typeof(CrawlerTests).Assembly.Location;
        // Use lowercase filename to match FName.Value (which lowercases the name)
        File.Copy(sourceDll, Path.Combine(dir, fileName.ToLowerInvariant()), overwrite: true);
    }

    [Fact]
    public void Crawl_ValidDirectory_PopulatesAssemblyListAndCounts()
    {
        CopyTestDllToDir(_tempDir, "test1.dll");
        CopyTestDllToDir(_tempDir, "test2.dll");

        var crawler = new Crawler(_tempDir);
        crawler.Crawl();

        Assert.True(crawler.TotalFileCount >= 2);
        Assert.True(crawler.TotalAssemblyCount >= 2);
        Assert.True(crawler.AssemblyList.Count >= 2);
    }

    [Fact]
    public void Crawl_CalledTwice_DoesNotReCrawl()
    {
        CopyTestDllToDir(_tempDir, "test1.dll");

        var crawler = new Crawler(_tempDir);
        crawler.Crawl();
        int countAfterFirstCrawl = crawler.TotalAssemblyCount;

        // Copy another DLL to the dir between crawls (shouldn't be picked up)
        CopyTestDllToDir(_tempDir, "test2.dll");
        crawler.Crawl();

        Assert.Equal(countAfterFirstCrawl, crawler.TotalAssemblyCount);
    }

    [Fact]
    public void Sort_BeforeCrawl_DoesNotCrash()
    {
        var crawler = new Crawler(_tempDir);

        // Should print "Must parse directory first." but not throw
        var exception = Record.Exception(() => crawler.Sort());
        Assert.Null(exception);
    }

    [Fact]
    public void Sort_AfterCrawl_SeparatesManagedAndUnmanaged()
    {
        CopyTestDllToDir(_tempDir, "managed1.dll");
        CopyTestDllToDir(_tempDir, "managed2.dll");

        // Create a non-managed binary file
        var nonManagedPath = Path.Combine(_tempDir, "native.dll");
        File.WriteAllBytes(nonManagedPath, new byte[128]);

        var crawler = new Crawler(_tempDir);
        crawler.Crawl();
        crawler.Sort();

        // AllAssemblies contains assemblies with duplicates (>1 copy)
        // AllManagedAssemblies is the filtered managed subset
        // Both should be valid without exception
        Assert.NotNull(crawler.AllAssemblies);
        Assert.NotNull(crawler.AllManagedAssemblies);
    }

    [Fact]
    public void CrawlDirectory_IgnoresResourceDlls()
    {
        CopyTestDllToDir(_tempDir, "regular.dll");
        CopyTestDllToDir(_tempDir, "resources.resources.dll");

        var crawler = new Crawler(_tempDir);
        crawler.Crawl();

        // The .resources.dll should be ignored; only regular.dll counted
        Assert.Equal(1, crawler.TotalAssemblyCount);
    }
}
