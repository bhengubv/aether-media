// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace AetherMedia.UI.Shared;

/// <summary>
/// Base class for Blazor components that are backed by an <see cref="INotifyPropertyChanged"/>
/// ViewModel.  Subscribes to <c>PropertyChanged</c> and marshals any notification back to
/// the Blazor render thread via <see cref="ComponentBase.InvokeAsync"/>.
/// </summary>
/// <typeparam name="TViewModel">ViewModel type — must implement <see cref="INotifyPropertyChanged"/>.</typeparam>
public abstract class ViewModelComponentBase<TViewModel> : ComponentBase, IDisposable
    where TViewModel : class, INotifyPropertyChanged
{
    [Inject] protected TViewModel ViewModel { get; set; } = null!;

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnVmPropertyChanged;
        base.OnInitialized();
    }

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        ViewModel.PropertyChanged -= OnVmPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
