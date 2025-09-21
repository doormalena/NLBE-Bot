namespace WorldOfTanksBlitzApi.Tests.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class WotInspectorBattleTests
{
	private static WotInspectorBattle CreateModel(int battleType = -1, int roomType = -1, int battleResult = -1)
	{
		WotInspectorBattle model = Substitute.For<WotInspectorBattle>();
		model.BattleType = battleType;
		model.RoomType = roomType;
		model.BattleResult = battleResult;
		return model;
	}

	[DataTestMethod]
	[DataRow(0, "Encounter")]
	[DataRow(1, "Supremacy")]
	[DataRow(-1, "")]
	[DataRow(99, "")]
	public void BattleTypeAsString_ShouldReturnExpected(int input, string expected)
	{
		WotInspectorBattle model = CreateModel(battleType: input);
		Assert.AreEqual(expected, model.BattleTypeAsString);
	}

	[DataTestMethod]
	[DataRow(1, "Normal")]
	[DataRow(2, "Training")]
	[DataRow(4, "Tournament")]
	[DataRow(5, "Tournament")]
	[DataRow(7, "Rating")]
	[DataRow(8, "Mad Games")]
	[DataRow(22, "Realistic")]
	[DataRow(23, "Uprising")]
	[DataRow(24, "Gravity Force")]
	[DataRow(25, "Skirmish")]
	[DataRow(26, "Burning")]
	[DataRow(27, "Boss Fight")]
	[DataRow(0, "")]
	[DataRow(3, "")]
	[DataRow(6, "")]
	[DataRow(99, "")]
	public void RoomTypeAsString_ShouldReturnExpected(int input, string expected)
	{
		WotInspectorBattle model = CreateModel(roomType: input);
		Assert.AreEqual(expected, model.RoomTypeAsString);
	}

	[DataTestMethod]
	[DataRow(1, "Victory")]
	[DataRow(2, "Defeat")]
	[DataRow(0, "Draw")]
	[DataRow(3, "Draw")]
	[DataRow(99, "Draw")]
	public void BattleResultAsString_ShouldReturnExpected(int input, string expected)
	{
		WotInspectorBattle model = CreateModel(battleResult: input);
		Assert.AreEqual(expected, model.BattleResultAsString);
	}
}
