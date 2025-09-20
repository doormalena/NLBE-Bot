namespace WorldOfTanksBlitzApi.Tests.Repositories;

using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Repositories;

[TestClass]
public class MapsRepositoryTests
{
	[TestMethod]
	public async Task GetAllAsync_ReturnsDictionaryWithMaps()
	{
		// Arrange.
		MapsRepository repo = new();

		// Act.
		Dictionary<string, MapInfo>? result = await repo.GetAllAsync();

		// Assert.
		Assert.IsNotNull(result);
		Assert.IsTrue(result.Count > 0, "Should return at least one map.");
		foreach (KeyValuePair<string, MapInfo> kvp in result)
		{
			Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Key));
			Assert.IsNotNull(kvp.Value);
			Assert.IsTrue(kvp.Value.Id > 0);
			Assert.IsFalse(string.IsNullOrWhiteSpace(kvp.Value.Name));
			Assert.IsNotNull(kvp.Value.Modes);
		}
	}
}
