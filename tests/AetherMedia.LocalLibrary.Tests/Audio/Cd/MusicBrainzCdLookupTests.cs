// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Cd;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Cd;

public class MusicBrainzCdLookupTests
{
    [Fact]
    public void ComputeDiscId_IsStable_AndBase64Url_Encoded()
    {
        // Construct a TOC with 3 audio tracks at known offsets.
        var toc = new CdToc(new[]
        {
            new CdTrack(Number: 1, StartLba: 0,    SectorCount: 13500, IsAudio: true),  // 3:00
            new CdTrack(Number: 2, StartLba: 13500, SectorCount: 22500, IsAudio: true), // 5:00
            new CdTrack(Number: 3, StartLba: 36000, SectorCount: 18000, IsAudio: true), // 4:00
        });

        var id = MusicBrainzCdLookup.ComputeDiscId(toc);
        Assert.False(string.IsNullOrEmpty(id));
        // Must be base64-url (no +, /, =).
        Assert.DoesNotContain('+', id);
        Assert.DoesNotContain('/', id);
        Assert.DoesNotContain('=', id);
        // Same TOC must produce the same ID.
        var id2 = MusicBrainzCdLookup.ComputeDiscId(toc);
        Assert.Equal(id, id2);
    }
}
