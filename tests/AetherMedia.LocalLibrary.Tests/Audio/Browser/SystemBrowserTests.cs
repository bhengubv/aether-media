// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Browser;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Browser;

public class SystemBrowserTests
{
    [Fact]
    public void Open_NullUri_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SystemBrowser().Open(null!));
    }

    [Fact]
    public void Open_RelativeUri_ReturnsFalse()
    {
        var relative = new Uri("not-a-real-path", UriKind.Relative);
        Assert.False(new SystemBrowser().Open(relative));
    }
}
