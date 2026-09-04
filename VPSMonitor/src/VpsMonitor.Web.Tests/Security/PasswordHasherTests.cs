using VpsMonitor.Web.Security;
using Xunit;

namespace VpsMonitor.Web.Tests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_returns_true_for_matching_password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("correct horse battery staple");

        Assert.False(hasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Hash_does_not_store_plain_password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("correct horse battery staple");

        Assert.NotEqual("correct horse battery staple", hash);
        Assert.StartsWith("$2", hash, StringComparison.Ordinal);
    }
}
