using System;
using System.IO;
using System.Threading;
using Avalonia;
using Project.Launch.Tools;

namespace Project.Launch
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                LogHelper.Write(LauncherPaths.StartupLog,
                    "程序入口执行", "Program entry executed");

                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(LauncherPaths.CrashLog,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{ex}");
                }
                catch { }

                Console.WriteLine("=== CRASH ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=============");
                Console.WriteLine("崩溃日志已写入 / Crash log written: " + LauncherPaths.CrashLog);
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}