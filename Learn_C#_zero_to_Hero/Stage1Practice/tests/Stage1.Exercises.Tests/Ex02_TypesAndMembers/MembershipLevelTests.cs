using Stage1.Exercises.Ex02_TypesAndMembers;

namespace Stage1.Exercises.Tests.Ex02_TypesAndMembers;

public class MembershipLevelTests
{
    [Theory]
    [InlineData("Bronze")]
    [InlineData("Silver")]
    [InlineData("Gold")]
    [InlineData("Platinum")]
    public void DefinesExpectedLevelNames(string levelName)
    {
        Assert.True(Enum.TryParse<MembershipLevel>(levelName, out _));
    }

    [Fact]
    public void LevelsAreOrderedAscendingByRank()
    {
        Assert.True((int)MembershipLevel.Bronze < (int)MembershipLevel.Silver);
        Assert.True((int)MembershipLevel.Silver < (int)MembershipLevel.Gold);
        Assert.True((int)MembershipLevel.Gold < (int)MembershipLevel.Platinum);
    }
}
