using Stage1.Exercises.Ex10_NullableReferenceTypes;

namespace Stage1.Exercises.Tests.Ex10_NullableReferenceTypes;

public class UserProfileTests
{
    [Fact]
    public void Constructor_WithNullEmail_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UserProfile(null!));
    }

    [Fact]
    public void Constructor_WithoutNickname_LeavesNicknameNull()
    {
        var profile = new UserProfile("a@b.com");
        Assert.Equal("a@b.com", profile.Email);
        Assert.Null(profile.Nickname);
    }

    [Fact]
    public void Constructor_WithNickname_SetsNickname()
    {
        var profile = new UserProfile("a@b.com", "Neo");
        Assert.Equal("Neo", profile.Nickname);
    }
}

public class UserProfileHelperTests
{
    [Fact]
    public void GetDisplayName_PrefersNicknameWhenPresent()
    {
        var profile = new UserProfile("jane.doe@example.com", "Janie");
        Assert.Equal("Janie", UserProfileHelper.GetDisplayName(profile));
    }

    [Fact]
    public void GetDisplayName_FallsBackToEmailLocalPart_WhenNicknameIsNull()
    {
        var profile = new UserProfile("jane.doe@example.com");
        Assert.Equal("jane.doe", UserProfileHelper.GetDisplayName(profile));
    }

    [Fact]
    public void GetDisplayName_TreatsWhitespaceNicknameAsMissing()
    {
        var profile = new UserProfile("bob@x.com", "   ");
        Assert.Equal("bob", UserProfileHelper.GetDisplayName(profile));
    }

    [Fact]
    public void GetDisplayName_WhenEmailHasNoAtSign_ReturnsEmailUnchanged()
    {
        var profile = new UserProfile("nobody");
        Assert.Equal("nobody", UserProfileHelper.GetDisplayName(profile));
    }

    [Fact]
    public void NormalizeEmail_WithNull_ReturnsNull()
    {
        Assert.Null(UserProfileHelper.NormalizeEmail(null));
    }

    [Fact]
    public void NormalizeEmail_WithWhitespace_ReturnsNull()
    {
        Assert.Null(UserProfileHelper.NormalizeEmail("   "));
    }

    [Fact]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        Assert.Equal("jane@example.com", UserProfileHelper.NormalizeEmail("  Jane@EXAMPLE.com "));
    }
}
