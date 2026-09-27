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
            LogHelper.Write(LauncherPaths.AppLog,
                "应用初始化开始", "Application initialization started");

            var account = AccountService.GetSelectedAccount();
            if (account != null)
            {
                PlayerName = account.Username;
                OfflineUuid = account.Uuid;
                IsLoggedIn = true;
                LoginCheck = AccountService.GetLoginCheck();

                LogHelper.Write(LauncherPaths.AppLog,
                    $"已加载账户: {PlayerName}，跳过登录: {LoginCheck}",
                    $"Account loaded: {PlayerName}, LoginCheck: {LoginCheck}");
            }
            else
            {
                PlayerName = null;
                OfflineUuid = null;
                IsLoggedIn = false;
                LoginCheck = false;

                AccountService.SetLoginCheck(false);

                LogHelper.Write(LauncherPaths.AppLog,
                    "未找到账户，LoginCheck 设为 false",
                    "No account found, LoginCheck set to false");
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                base.OnFrameworkInitializationCompleted();
            }

            LogHelper.Write(LauncherPaths.AppLog,
                "应用初始化完成", "Application initialization complete");
        }
    }
}