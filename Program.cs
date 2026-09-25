using System;
using System.IO;
using Avalonia;
using Fluid.Avalonia.Acrylic;

namespace Project.Launch
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // 记录启动时间，确认程序真的执行到这里
            File.WriteAllText("program_start.log", $"程序入口执行时间: {DateTime.Now}\n");

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);

                File.AppendAllText("program_start.log", $"正常退出时间: {DateTime.Now}\n");
            }
            catch (Exception ex)
            {
                // ★ 把异常写入文件
                File.WriteAllText("crash.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{ex}");
                Console.WriteLine("=== CRASH ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=============");
                Console.ReadKey();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseAcrylicPerformanceDefaults()   // ★ 新增：启用性能优化
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}