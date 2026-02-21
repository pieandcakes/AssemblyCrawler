using AssemblyCrawler;
using System.IO;
using System.Reflection;

namespace AssemblyCrawler.Tests;

public class AssemblyInfoTests : IDisposable
{
    private readonly string _tempDir;

    public AssemblyInfoTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AssemblyInfoTests_" + Guid.NewGuid().ToString("N"));
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
        string location = typeof(AssemblyInfoTests).Assembly.Location;
        string lowercaseName = Path.GetFileName(location).ToLowerInvariant();
        string dest = Path.Combine(_tempDir, lowercaseName);
        File.Copy(location, dest, overwrite: true);
        return new FileInfo(dest);
    }

    [Fact]
    public void Constructor_ManagedDll_PopulatesFName()
    {
        var fileInfo = GetManagedDllFileInfo();
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.Equal(fileInfo.Name.ToLowerInvariant(), assemblyInfo.FName.Value);
    }

    [Fact]
    public void Constructor_ManagedDll_IsManaged_ReturnsTrue()
    {
        var fileInfo = GetManagedDllFileInfo();
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.True(assemblyInfo.IsManaged.Value);
    }

    [Fact]
    public void Constructor_ManagedDll_AName_IsNotEmpty()
    {
        var fileInfo = GetManagedDllFileInfo();
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.NotEmpty(assemblyInfo.AName.Value);
    }

    [Fact]
    public void Constructor_ManagedDll_AssemblyVersion_IsNotNull()
    {
        var fileInfo = GetManagedDllFileInfo();
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.NotNull(assemblyInfo.AssemblyVersion.Value);
    }

    [Fact]
    public void Constructor_NonManagedBinary_IsManaged_ReturnsFalse()
    {
        var tempFile = Path.Combine(_tempDir, "notadll.dll");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });
        var fileInfo = new FileInfo(tempFile);
        var assemblyInfo = new AssemblyInfo(fileInfo);

        Assert.False(assemblyInfo.IsManaged.Value);
    }

    [Fact]
    public void GetHashCode_SameAssembly_ReturnsConsistentValue()
    {
        var fileInfo = GetManagedDllFileInfo();
        var assemblyInfo1 = new AssemblyInfo(fileInfo);
        var assemblyInfo2 = new AssemblyInfo(fileInfo);

        Assert.Equal(assemblyInfo1.GetHashCode(), assemblyInfo2.GetHashCode());
    }
}
