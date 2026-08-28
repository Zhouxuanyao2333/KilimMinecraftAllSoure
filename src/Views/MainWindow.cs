using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Project.Launch.Views
{
    public partial class MainWindow : Window
    {
        private readonly Color _defaultStartColor = Color.Parse("#CCFF6B9D");
        private readonly Color _defaultEndColor = Color.Parse("#CCB366FF");
        private readonly string _logFilePath = "MainWindowLog.txt";
        private readonly object _logLock = new object();
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            try
            {
                if (File.Exists(_logFilePath))
                    File.Delete(_logFilePath);
                File.WriteAllText(_logFilePath, $"=== Kilim Launcher Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            }
            catch { }

            Log("程序启动");
            InitializeComponent();
            Log("InitializeComponent 完成");

            var closeButton = this.FindControl<Button>("CloseButton");
            if (closeButton != null)
                closeButton.Click += (s, e) => this.Close();

            var minimizeButton = this.FindControl<Button>("MinimizeButton");
            if (minimizeButton != null)
                minimizeButton.Click += (s, e) => this.WindowState = WindowState.Minimized;

            Log("按钮事件绑定完成");

            UpdateTitleBarGradient(_defaultStartColor, _defaultEndColor);
            Log("初始化完成");

            if (!App.LoginCheck)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ShowLoginDialog();
                });
            }
            else
            {
                UpdateLoginStatus();
            }
        }

        private void UpdateLoginStatus()
        {
            var statusText = this.FindControl<TextBlock>("SkinStatusText");
            if (statusText != null)
            {
                if (App.IsLoggedIn && !string.IsNullOrEmpty(App.PlayerName))
                {
                    statusText.Text = App.PlayerName;
                }
                else
                {
                    statusText.Text = "未登录";
                }
            }
        }

        private void Log(string message)
        {
            try
            {
                lock (_logLock)
                {
                    File.AppendAllText(_logFilePath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n");
                }
            }
            catch { }
        }

        private async Task ShowLoginDialog()
        {
            var overlay = this.FindControl<Border>("Overlay");
            if (overlay != null) overlay.IsVisible = true;

            try
            {
                var loginWindow = new LoginWindow();
                await loginWindow.ShowDialog(this);

                UpdateLoginStatus();

                if (App.IsLoggedIn && loginWindow.FaceBitmap != null && loginWindow.HatBitmap != null)
                {
                    var faceImg = this.FindControl<Image>("FaceImage");
                    var hatImg = this.FindControl<Image>("HatImage");
                    if (faceImg != null)
                    {
                        faceImg.Source = loginWindow.FaceBitmap;
                        RenderOptions.SetBitmapInterpolationMode(faceImg, BitmapInterpolationMode.None);
                    }
                    if (hatImg != null)
                    {
                        hatImg.Source = loginWindow.HatBitmap;
                        RenderOptions.SetBitmapInterpolationMode(hatImg, BitmapInterpolationMode.None);
                    }
                }
            }
            finally
            {
                if (overlay != null) overlay.IsVisible = false;
            }
        }

        private async Task<(Bitmap? face, Bitmap? hat)> CropSkinAsync(Stream stream)
        {
            try
            {
                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                return await Task.Run<(Bitmap? face, Bitmap? hat)>(() =>
                {
                    try
                    {
                        using var bitmap = new Bitmap(new MemoryStream(imageData));
                        int width = bitmap.PixelSize.Width;
                        int height = bitmap.PixelSize.Height;

                        bool isValidSize = (width == 64 && height == 32) ||
                                           (width == 64 && height == 64) ||
                                           (width == 128 && height == 128);
                        if (!isValidSize)
                            return (null, null);

                        int faceX, faceY, faceSize, hatX, hatY;

                        if (width >= 128 && height >= 128)
                        {
                            faceX = 16; faceY = 16; faceSize = 16;
                            hatX = 80; hatY = 16;
                        }
                        else
                        {
                            faceX = 8; faceY = 8; faceSize = 8;
                            hatX = 40; hatY = 8;
                        }

                        var faceCropped = new CroppedBitmap(bitmap, new PixelRect(faceX, faceY, faceSize, faceSize));
                        var hatCropped = new CroppedBitmap(bitmap, new PixelRect(hatX, hatY, faceSize, faceSize));

                        var faceBmp = new RenderTargetBitmap(new PixelSize(faceSize, faceSize), new Vector(96, 96));
                        using (var ctx = faceBmp.CreateDrawingContext())
                            ctx.DrawImage(faceCropped, new Rect(0, 0, faceSize, faceSize));

                        var hatBmp = new RenderTargetBitmap(new PixelSize(faceSize, faceSize), new Vector(96, 96));
                        using (var ctx = hatBmp.CreateDrawingContext())
                            ctx.DrawImage(hatCropped, new Rect(0, 0, faceSize, faceSize));

                        return (faceBmp, hatBmp);
                    }
                    catch
                    {
                        return (null, null);
                    }
                });
            }
            catch
            {
                return (null, null);
            }
        }

        private void UpdateTitleBarGradient(Color startColor, Color endColor)
        {
            var titleBar = this.FindControl<Border>("TitleBarBorder");
            if (titleBar == null) return;

            var gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(startColor, 0.0),
                    new GradientStop(endColor, 1.0)
                }
            };

            titleBar.Background = gradient;
            UpdateTitleBarForeground(startColor, endColor);
        }

        private void UpdateTitleBarForeground(Color startColor, Color endColor)
        {
            double avgLuminance = (GetLuminance(startColor) + GetLuminance(endColor)) / 2.0;
            bool useDark = avgLuminance > 0.5;

            var titleText = this.FindControl<TextBlock>("TitleText");
            if (titleText != null)
                titleText.Foreground = useDark ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White);

            var minImg = this.FindControl<Image>("MinimizeImage");
            if (minImg != null)
            {
                var path = useDark ? "avares://Project.Launch/src/Views/imgs/black-.png"
                                   : "avares://Project.Launch/src/Views/imgs/white-.png";
                try
                {
                    var uri = new Uri(path);
                    var stream = AssetLoader.Open(uri);
                    minImg.Source = new Bitmap(stream);
                }
                catch { }
            }

            var closeImg = this.FindControl<Image>("CloseImage");
            if (closeImg != null)
            {
                var path = useDark ? "avares://Project.Launch/src/Views/imgs/blackx.png"
                                   : "avares://Project.Launch/src/Views/imgs/whitex.png";
                try
                {
                    var uri = new Uri(path);
                    var stream = AssetLoader.Open(uri);
                    closeImg.Source = new Bitmap(stream);
                }
                catch { }
            }
        }

        private static double GetLuminance(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        public void SetTitleBarGradient(Color start, Color end)
        {
            UpdateTitleBarGradient(start, end);
        }
    }
}