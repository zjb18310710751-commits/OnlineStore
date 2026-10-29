using System.Collections;
using System.Globalization;
using System.Resources;

namespace OnlineStore.Tests.Models;

public class ResourceConsistencyTests
{
    [Fact]
    public void ChineseAndEnglishResourcesHaveMatchingNonEmptyKeys()
    {
        var resourceManager = new ResourceManager(
            "OnlineStore.Resources.SharedResource",
            typeof(SharedResource).Assembly);
        var chinese = ReadResources(resourceManager, new CultureInfo("zh-CN"));
        var english = ReadResources(resourceManager, new CultureInfo("en"));

        Assert.Equal(chinese.Keys.Order(), english.Keys.Order());
        Assert.All(chinese, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value)));
        Assert.All(english, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value)));
    }

    private static Dictionary<string, string> ReadResources(
        ResourceManager resourceManager,
        CultureInfo culture)
    {
        using var resources = resourceManager.GetResourceSet(culture, true, true)
            ?? throw new InvalidOperationException($"Missing resources for {culture.Name}.");

        return resources.Cast<DictionaryEntry>()
            .ToDictionary(
                entry => (string)entry.Key,
                entry => entry.Value?.ToString() ?? string.Empty);
    }
}
