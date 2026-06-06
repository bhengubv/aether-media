// SPDX-License-Identifier: MIT
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace AetherMedia.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        // Wire up the Blazor root component in code-behind so that XamlC does not
        // try to resolve the Razor-generated Routes type during the XAML compile phase
        // (which runs before the Blazor source generator produces that type).
        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector      = "#app",
            ComponentType = typeof(Routes),
        });
    }
}
