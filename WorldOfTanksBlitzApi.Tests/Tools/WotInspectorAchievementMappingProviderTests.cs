namespace WorldOfTanksBlitzApi.Tests.Tools;

using WorldOfTanksBlitzApi.Tools;

[TestClass]
public class WotInspectorAchievementMappingProviderTests
{
	[TestMethod]
	public async Task GetMappingAsync_ReturnsDictionary_WhenResourceExists()
	{
		// Arrange.
		WotInspectorAchievementMappingProvider provider = new();

		// Act.
		Dictionary<string, string>? mapping = await provider.GetMappingAsync();

		// Assert.
		Assert.IsNotNull(mapping);
		Assert.IsTrue(mapping.Count > 0, "Mapping should contain at least one entry.");
		foreach (KeyValuePair<string, string> kvp in mapping)
		{
			Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Key));
			Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Value));
		}
	}

	[TestMethod]
	public async Task GetMappingAsync_ThrowsFileNotFoundException_WhenResourceMissing()
	{
		// Arrange.
		WotInspectorAchievementMappingProvider provider = new("WorldOfTanksBlitzApi.Tools.NonExistentResource.json");

		// Act & Assert.
		await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
		{
			await provider.GetMappingAsync();
		});
	}
}
