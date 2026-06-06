// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new MainPage());
}
