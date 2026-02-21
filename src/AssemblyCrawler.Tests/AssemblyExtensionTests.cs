using AssemblyCrawler;
using System.IO;

namespace AssemblyCrawler.Tests;

public class AssemblyExtensionTests : IDisposable
{
    private readonly string _tempDir;

    public AssemblyExtensionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AssemblyExtensionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // IsManagedAssembly uses FName (lowercased), so copy the DLL with a lowercase filename
    private FileInfo GetManagedDllFileInfo()
    {
        string location = typeof(AssemblyExtensionTests).Assembly.Location;
        string lowercaseName = Path.GetFileName(location).ToLowerInvariant();
        string dest = Path.Combine(_tempDir, lowercaseName);
        File.Copy(location, dest, overwrite: true);
        return new FileInfo(dest);
    }

    [Fact]
    public void IsManagedAssembly_ManagedDll_ReturnsTrue()
    {
        var fileInfo = GetManagedDllFileInfo();
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.True(assemblyInfo.IsManagedAssembly());
    }

    [Fact]
    public void IsManagedAssembly_NonManagedFile_ReturnsFalse()
    {
        var tempFile = Path.Combine(_tempDir, "notmanaged.dll");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });
        var fileInfo = new FileInfo(tempFile);
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.False(assemblyInfo.IsManagedAssembly());
    }

    [Fact]
    public void SortByHashCode_GroupsIdenticalAssembliesTogether()
    {
        var fileInfo = GetManagedDllFileInfo();
        var a1 = new AssemblyInfo(fileInfo);
        var a2 = new AssemblyInfo(fileInfo);
        var a3 = new AssemblyInfo(fileInfo);

        var list = new List<AssemblyInfo> { a1, a2, a3 };
        var sorted = list.SortByHashCode();

        // All three should have the same hash code and be in the same group
        Assert.Single(sorted);
        Assert.Equal(3, sorted.Values.First().Count);
    }
}
