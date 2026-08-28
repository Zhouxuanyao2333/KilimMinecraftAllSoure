using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Project.Launch.Views;
using System;
using Project.Launch.Tools;

namespace Project.Launch
{
    public partial class App : Application
    {
        public static bool LoginCheck { get; set; }
        public static bool IsLoggedIn { get; set; }
        public static string? PlayerName { get; set; }
        public static string? OfflineUuid { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var account = AccountService.GetSelectedAccount();
            if (account != null)
            {
                PlayerName = account.Username;
                OfflineUuid = account.Uuid;
                IsLoggedIn = true;
                LoginCheck = AccountService.GetLoginCheck();
            }
            else
            {
                PlayerName = null;
                OfflineUuid = null;
                IsLoggedIn = false;
                LoginCheck = false;
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                base.OnFrameworkInitializationCompleted();
            }
        }
    }
}