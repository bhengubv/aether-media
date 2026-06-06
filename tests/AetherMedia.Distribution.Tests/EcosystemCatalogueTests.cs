// SPDX-License-Identifier: MIT

using AetherMedia.Distribution;

namespace AetherMedia.Distribution.Tests;

public sealed class EcosystemCatalogueTests
{
    [Fact]
    public void Catalogue_ContainsAetherNetMedia()
    {
        Assert.Contains(EcosystemCatalogue.Apps, a => a.AppId == "aether-media");
    }

    [Fact]
    public void Catalogue_ContainsSleptOn()
    {
        Assert.Contains(EcosystemCatalogue.Apps, a => a.AppId == "slepton");
    }

    [Fact]
    public void Catalogue_AllAppsHaveRequiredFields()
    {
        foreach (var app in EcosystemCatalogue.Apps)
        {
            Assert.False(string.IsNullOrWhiteSpace(app.AppId),
                "An app in the catalogue has no AppId");
            Assert.False(string.IsNullOrWhiteSpace(app.Name),
                $"{app.AppId} has no Name");
            Assert.False(string.IsNullOrWhiteSpace(app.CloudflareUrl),
                $"{app.AppId} has no CloudflareUrl");
            Assert.Contains("cdn.aethermedia.app", app.CloudflareUrl);
        }
    }

    [Fact]
    public void Catalogue_AllAppsHaveAtLeastOneTag()
    {
        foreach (var app in EcosystemCatalogue.Apps)
            Assert.NotEmpty(app.Tags);
    }

    [Fact]
    public void Catalogue_AppIdsAreUnique()
    {
        var ids = EcosystemCatalogue.Apps.Select(a => a.AppId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Catalogue_TagsDisplay_JoinsWithDot()
    {
        var app = EcosystemCatalogue.Apps.First(a => a.Tags.Length > 1);
        Assert.Contains(" · ", app.TagsDisplay);
    }

    [Fact]
    public void Catalogue_AllContentTypes_AreApk()
    {
        foreach (var app in EcosystemCatalogue.Apps)
            Assert.Equal("application/vnd.android.package-archive", app.ContentType);
    }
}
