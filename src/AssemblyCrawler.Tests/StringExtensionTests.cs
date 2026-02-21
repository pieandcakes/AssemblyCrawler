using AssemblyCrawler;

namespace AssemblyCrawler.Tests;

public class StringExtensionTests
{
    [Fact]
    public void GetStableHashCode_SameInput_ReturnsSameValue()
    {
        var hash1 = "hello".GetStableHashCode();
        var hash2 = "hello".GetStableHashCode();
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetStableHashCode_DifferentInputs_ReturnsDifferentValues()
    {
        var hash1 = "hello".GetStableHashCode();
        var hash2 = "world".GetStableHashCode();
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetStableHashCode_EmptyString_ReturnsConsistentValue()
    {
        var hash1 = "".GetStableHashCode();
        var hash2 = "".GetStableHashCode();
        Assert.Equal(hash1, hash2);
    }
}
