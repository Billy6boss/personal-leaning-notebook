using Stage1.Exercises.Ex02_TypesAndMembers;

namespace Stage1.Exercises.Tests.Ex02_TypesAndMembers;

public class LoyaltyAccountTests
{
    [Fact]
    public void NewAccount_StartsAtZeroPointsAndBronze()
    {
        var account = new LoyaltyAccount("Alice");

        Assert.Equal("Alice", account.OwnerName);
        Assert.Equal(0, account.PointsBalance);
        Assert.Equal(MembershipLevel.Bronze, account.CurrentLevel);
    }

    [Theory]
    [InlineData(500, MembershipLevel.Bronze)]
    [InlineData(999, MembershipLevel.Bronze)]
    [InlineData(1_000, MembershipLevel.Silver)]
    [InlineData(4_999, MembershipLevel.Silver)]
    [InlineData(5_000, MembershipLevel.Gold)]
    [InlineData(19_999, MembershipLevel.Gold)]
    [InlineData(20_000, MembershipLevel.Platinum)]
    [InlineData(50_000, MembershipLevel.Platinum)]
    public void AddPoints_UpdatesLevelAccordingToThresholds(int pointsToAdd, MembershipLevel expectedLevel)
    {
        var account = new LoyaltyAccount("Bob");

        account.AddPoints(pointsToAdd);

        Assert.Equal(pointsToAdd, account.PointsBalance);
        Assert.Equal(expectedLevel, account.CurrentLevel);
    }

    [Fact]
    public void AddPoints_Accumulates_AndCrossesThresholds()
    {
        var account = new LoyaltyAccount("Cara");

        account.AddPoints(900);
        Assert.Equal(MembershipLevel.Bronze, account.CurrentLevel);

        account.AddPoints(200); // 總計 1100
        Assert.Equal(1_100, account.PointsBalance);
        Assert.Equal(MembershipLevel.Silver, account.CurrentLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddPoints_WithNonPositiveAmount_Throws(int invalidPoints)
    {
        var account = new LoyaltyAccount("Dave");

        Assert.Throws<ArgumentOutOfRangeException>(() => account.AddPoints(invalidPoints));
    }
}
