using Wpf.Ui;
using Wpf.Ui.Controls;

namespace YFToolbox.App.Tests.Fakes;

internal sealed class FakeNavigationService : INavigationService
{
    public Type? NavigatedType { get; private set; }

    public int NavigateCallCount { get; private set; }

    public bool Navigate(Type pageType)
    {
        NavigatedType = pageType;
        NavigateCallCount++;
        return true;
    }

    public bool Navigate(Type pageType, object? dataContext)
    {
        NavigatedType = pageType;
        NavigateCallCount++;
        return true;
    }

    public bool Navigate(string pageIdOrTargetTag) => true;

    public bool Navigate(string pageIdOrTargetTag, object? dataContext) => true;

    public bool NavigateWithHierarchy(Type pageType)
    {
        NavigatedType = pageType;
        NavigateCallCount++;
        return true;
    }

    public bool NavigateWithHierarchy(Type pageType, object? dataContext)
    {
        NavigatedType = pageType;
        NavigateCallCount++;
        return true;
    }

    public bool GoBack() => true;

    public INavigationView GetNavigationControl() => null!;

    public void SetNavigationControl(INavigationView navigationView)
    {
    }
}
