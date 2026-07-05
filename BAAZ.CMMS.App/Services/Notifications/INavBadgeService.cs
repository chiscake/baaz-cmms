using System;

using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Services.Notifications;

public interface INavBadgeService
{
    event EventHandler? BadgesChanged;

    void Attach(NavigationView navigationView);

    int GetCount(string navItemId);

    void SetCount(string navItemId, int count);

    void Increment(string navItemId, int delta = 1);

    void Clear(string navItemId);

    void ApplyToNavigationView();
}
