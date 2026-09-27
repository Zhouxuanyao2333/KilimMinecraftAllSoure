using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Interactivity;
using LiquidGlassAvaloniaUI;
using Project.Launch;
using Project.Launch.Tools;

namespace Project.Launch.Views
{
    public partial class LoginWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        public string PlayerName { get; private set; } = "Steve";
        public Bitmap? FaceBitmap { get; private set; }
        public Bitmap? HatBitmap { get; private set; }
        public string? OfflineUuid { get; private set; }

        private Button? _offlineBtn;
        private Button? _onlineBtn;
        private Button? _thirdPartyBtn;
        private LiquidGlassSurface? _slider;
        private TranslateTransform? _sliderTransform;
        private ScaleTransform? _sliderScale;
        private int _selectedIndex = 1;

        private Border? _titleContainer;
        private TranslateTransform? _titleTransform;
        private Border? _inputPanel;

        private TextBox? _playerNameBox;
        private Button? _fetchSkinButton;
        private Button? _selectLocalButton;
        private Button? _loginButton;
        private TextBlock? _loginStatus;
        private TextBlock? _modeLabel;
        private TextBlock? _localFileLabel;

        private DispatcherTimer? _animationTimer;
        private double _animationStartX;
        private double _animationTargetX;
        private DateTime _animationStartTime;

        private const double OFFSET_OFFLINE = 0;
        private const double OFFSET_ONLINE = -10;
        private const double OFFSET_THIRD = -60;

        public LoginWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            InitializeComponent();

            InitializeLoginModeSelector();
            InitializeInputControls();

            if (_fetchSkinButton != null)
                _fetchSkinButton.Click += OnFetchSkinClicked;

            if (_selectLocalButton != null)
                _selectLocalButton.Click += OnSelectLocalSkinClicked;

            if (_playerNameBox != null)
                _playerNameBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) OnLoginClicked(s, e); };

            var gitHubButton = this.FindControl<Button>("GitHubButton");
            if (gitHubButton != null)
                gitHubButton.Click += OnGitHubClicked;

            _titleContainer = this.FindControl<Border>("TitleContainer");
            if (_titleContainer != null)
            {
                _titleTransform = _titleContainer.RenderTransform as TranslateTransform;
                if (_titleTransform == null)
                {
                    _titleTransform = new TranslateTransform();
                    _titleContainer.RenderTransform = _titleTransform;
                }
                _titleTransform.Y = OFFSET_ONLINE;
            }

            _inputPanel = this.FindControl<Border>("InputPanel");

            UpdateModeUI(1);
            SetDefaultSteveSkin();
        }

        private void InitializeInputControls()
        {
            _playerNameBox = this.FindControl<TextBox>("PlayerNameBox");
            _fetchSkinButton = this.FindControl<Button>("FetchSkinButton");
            _selectLocalButton = this.FindControl<Button>("SelectLocalSkinButton");
            _loginButton = this.FindControl<Button>("LoginButton");
            _loginStatus = this.FindControl<TextBlock>("LoginStatus");
            _modeLabel = this.FindControl<TextBlock>("ModeLabel");
            _localFileLabel = this.FindControl<TextBlock>("LocalFileLabel");
        }

        private void InitializeLoginModeSelector()
        {
            _offlineBtn = this.FindControl<Button>("OfflineLoginButton");
            _onlineBtn = this.FindControl<Button>("OnlineLoginButton");
            _thirdPartyBtn = this.FindControl<Button>("ThirdPartyLoginButton");

            _slider = this.FindControl<LiquidGlassSurface>("LoginModeSlider");
            if (_slider != null)
            {
                var group = _slider.RenderTransform as TransformGroup;
                if (group != null)
                {
                    _sliderTransform = group.Children.OfType<TranslateTransform>().FirstOrDefault();
                    _sliderScale = group.Children.OfType<ScaleTransform>().FirstOrDefault();
                }

                if (_sliderTransform != null)
                {
                    _sliderTransform.X = 120;
                    _sliderTransform.Y = 0;
                }

                if (_sliderScale != null)
                {
                    _sliderScale.ScaleX = 1;
                    _sliderScale.ScaleY = 1;
                }
            }

            if (_offlineBtn != null) _offlineBtn.Click += (s, e) => { _selectedIndex = 0; AnimateSliderTo(0); UpdateModeUI(0); };
            if (_onlineBtn != null) _onlineBtn.Click += (s, e) => { _selectedIndex = 1; AnimateSliderTo(1); UpdateModeUI(1); };
            if (_thirdPartyBtn != null) _thirdPartyBtn.Click += (s, e) => { _selectedIndex = 2; AnimateSliderTo(2);  /* UpdateModeUI(2); */};

            UpdateButtonColors(1);
        }

        private void UpdateModeUI(int index)
        {
            if (_playerNameBox == null || _loginButton == null) return;

            if (_inputPanel != null)
                _inputPanel.Opacity = 0;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(AnimationConfig.InputPanelFadeDuration);

                switch (index)
                {
                    case 0:
                        _modeLabel!.IsVisible = false;
                        _localFileLabel!.IsVisible = false;
                        _fetchSkinButton!.IsVisible = false;
                        _selectLocalButton!.IsVisible = false;
                        _playerNameBox.IsVisible = true;
                        _playerNameBox.PlaceholderText = "输入用户名";
                        _playerNameBox.Width = 200;
                        _loginButton.IsVisible = true;
                        _loginButton.Content = "确认登录";
                        _loginStatus!.IsVisible = true;
                        _loginStatus.Text = "输入用户名，点击确认登录";
                        break;
                    case 1:
                        _modeLabel!.IsVisible = true;
                        _localFileLabel!.IsVisible = true;
                        _fetchSkinButton!.IsVisible = true;
                        _selectLocalButton!.IsVisible = true;
                        _playerNameBox.IsVisible = true;
                        _playerNameBox.PlaceholderText = "输入正版 ID";
                        _playerNameBox.Width = 170;
                        _loginButton.IsVisible = true;
                        _loginButton.Content = "确认登录";
                        _loginStatus!.IsVisible = true;
                        _loginStatus.Text = "输入正版ID，获取皮肤后确认登录";
                        break;
                    case 2:
                        _modeLabel!.IsVisible = false;
                        _localFileLabel!.IsVisible = false;
                        _fetchSkinButton!.IsVisible = false;
                        _selectLocalButton!.IsVisible = false;
                        _playerNameBox.IsVisible = false;
                        _loginButton.IsVisible = false;
                        _loginStatus!.IsVisible = true;
                        _loginStatus.Text = "第三方登录暂未支持";
                        break;
                }

                if (_inputPanel != null)
                    _inputPanel.Opacity = 1;
            });
        }

        // ★ 滑块动画：位置（对称EaseInOut） + 缩放（高斯钟形） + 中心补偿 + 模糊（三角形曲线）
        private void AnimateSliderTo(int index)
        {
            if (_sliderTransform == null || _slider == null) return;

            double targetX = index * 120;
            double startX = _sliderTransform.X;

            _animationTimer?.Stop();

            _animationStartX = startX;
            _animationTargetX = targetX;
            _animationStartTime = DateTime.Now;

            TimeSpan duration = AnimationConfig.SliderMoveDuration;
            double peakScale = AnimationConfig.SliderPeakScale;
            double sigma = AnimationConfig.SliderScaleSigma;
            double blurPeak = AnimationConfig.SliderBlurPeak;
            double halfW = AnimationConfig.SliderWidth / 2.0;
            double halfH = AnimationConfig.SliderHeight / 2.0;

            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _animationStartTime;
                double progress = elapsed.TotalMilliseconds / duration.TotalMilliseconds;
                if (progress >= 1.0)
                {
                    progress = 1.0;
                    _animationTimer.Stop();
                }

                // ★ 位置：EaseInOut（对称，两端慢中间快）
                double eased = progress < 0.5
                    ? 4 * progress * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 3) / 2;

                double baseX = _animationStartX + (_animationTargetX - _animationStartX) * eased;

                // ★ 缩放：高斯钟形（对称，中间峰值）
                double scaleCurve = Math.Exp(-Math.Pow((progress - 0.5) / sigma, 2));
                double currentScale = 1.0 + (peakScale - 1.0) * scaleCurve;

                // ★ 模糊：三角形曲线（0 → 峰值 → 0，中间线性变化）
                double blurCurve = 1.0 - Math.Abs(progress * 2.0 - 1.0);
                double currentBlur = blurPeak * blurCurve;
                _slider.BlurRadius = currentBlur;

                // 中心补偿（抵消左上角缩放造成的视觉偏移）
                double xCompensation = -(currentScale - 1.0) * halfW;
                double yCompensation = -(currentScale - 1.0) * halfH;

                _sliderTransform.X = baseX + xCompensation;
                _sliderTransform.Y = yCompensation;

                if (_sliderScale != null)
                {
                    _sliderScale.ScaleX = currentScale;
                    _sliderScale.ScaleY = currentScale;
                }

                if (progress >= 1.0)
                {
                    _sliderTransform.X = _animationTargetX;
                    _sliderTransform.Y = 0;
                    if (_sliderScale != null)
                    {
                        _sliderScale.ScaleX = 1.0;
                        _sliderScale.ScaleY = 1.0;
                    }
                    _slider.BlurRadius = 0;
                }
            };

            _animationTimer.Start();

            UpdateButtonColors(index);
        }

        private void UpdateButtonColors(int selectedIndex)
        {
            var buttons = new[] { _offlineBtn, _onlineBtn, _thirdPartyBtn };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i]!.Foreground = (i == selectedIndex)
                        ? new SolidColorBrush(Colors.Black)
                        : new SolidColorBrush(Color.Parse("#666666"));
                }
            }
        }

        private async void OnGitHubClicked(object? sender, EventArgs e)
        {
            try
            {
                var uri = new Uri("https://github.com/Zhouxuanyao2333/KilimMinecraftLauncher");
                var launcher = TopLevel.GetTopLevel(this)?.Launcher;
                if (launcher != null)
                    await launcher.LaunchUriAsync(uri);
                else
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = uri.ToString(),
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开链接失败: {ex.Message}");
                if (_loginStatus != null)
                    _loginStatus.Text = "❌ 打开链接失败，请手动访问仓库";
            }
        }

        private async void OnFetchSkinClicked(object? sender, EventArgs e)
        {
            if (_playerNameBox == null || _loginStatus == null) return;
            if (string.IsNullOrWhiteSpace(_playerNameBox.Text))
            {
                _loginStatus.Text = "❌ 请输入玩家ID";
                return;
            }

            string playerName = _playerNameBox.Text.Trim();
            _loginStatus.Text = $"⏳ 正在获取 {playerName} 的皮肤...";

            try
            {
                var (face, hat) = await FetchSkinFromMojangAsync(playerName);
                if (face != null && hat != null)
                {
                    PlayerName = playerName;
                    FaceBitmap = face;
                    HatBitmap = hat;
                    _loginStatus.Text = $"✅ 已加载 {playerName} 的皮肤";
                }
                else
                {
                    SetDefaultSteveSkin();
                    _loginStatus.Text = $"❌ 未找到玩家 {playerName}，使用默认皮肤";
                }
            }
            catch (Exception ex)
            {
                _loginStatus.Text = $"❌ 错误: {ex.Message}";
            }
        }

        private async void OnSelectLocalSkinClicked(object? sender, EventArgs e)
        {
            if (_loginStatus == null) return;
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择皮肤文件",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("PNG 图片")
                        {
                            Patterns = new[] { "*.png" },
                            MimeTypes = new[] { "image/png" }
                        }
                    }
                });

                if (files.Count >= 1)
                {
                    var file = files[0];
                    await using var stream = await file.OpenReadAsync();
                    var (face, hat) = await CropSkinAsync(stream);
                    if (face != null && hat != null)
                    {
                        PlayerName = Path.GetFileNameWithoutExtension(file.Name);
                        FaceBitmap = face;
                        HatBitmap = hat;
                        _loginStatus.Text = $"✅ 已加载本地皮肤: {file.Name}";
                    }
                    else
                    {
                        _loginStatus.Text = "❌ 无效的皮肤文件";
                    }
                }
            }
            catch (Exception ex)
            {
                _loginStatus.Text = $"❌ 选择文件失败: {ex.Message}";
            }
        }

        public void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            if (_selectedIndex == 0)
            {
                if (_playerNameBox == null || string.IsNullOrWhiteSpace(_playerNameBox.Text))
                {
                    if (_loginStatus != null) _loginStatus.Text = "❌ 请输入用户名";
                    return;
                }

                string username = _playerNameBox.Text.Trim();
                try
                {
                    string uuidStr = OfflineUuidGenerator.GenerateUuidString(username).Replace("-", "");

                    App.PlayerName = username;
                    App.OfflineUuid = uuidStr;
                    App.IsLoggedIn = true;
                    App.LoginCheck = true;

                    var account = new AccountInfo
                    {
                        Type = "offline",
                        Username = username,
                        Uuid = uuidStr,
                        LastUsed = DateTime.UtcNow
                    };
                    string accountId = $"offline_{username}";
                    AccountService.SetSelectedAccount(accountId, account);
                    AccountService.SetLoginCheck(true);

                    Close(true);
                }
                catch (Exception ex)
                {
                    if (_loginStatus != null)
                        _loginStatus.Text = $"❌ 保存账户失败: {ex.Message}";
                }
            }
            else if (_selectedIndex == 1)
            {
                if (FaceBitmap == null || HatBitmap == null)
                    SetDefaultSteveSkin();
                App.LoginCheck = true;
                App.IsLoggedIn = true;
                AccountService.SetLoginCheck(true);
                if (!string.IsNullOrEmpty(PlayerName) && PlayerName != "Steve")
                    App.PlayerName = PlayerName;
                Close(true);
            }
            else
            {
                if (_loginStatus != null) _loginStatus.Text = "❌ 第三方登录暂未实现";
            }
        }

        private void SetDefaultSteveSkin()
        {
            try
            {
                var stevePath = "avares://Project.Launch/src/Views/imgs/skin/Steve.png";
                var uri = new Uri(stevePath);
                var stream = AssetLoader.Open(uri);
                using var bitmap = new Bitmap(stream);
                var result = CropSkinAsync(stream).GetAwaiter().GetResult();
                if (result.face != null && result.hat != null)
                {
                    FaceBitmap = result.face;
                    HatBitmap = result.hat;
                    PlayerName = "Steve";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载默认皮肤失败: {ex.Message}");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        private async Task<(Bitmap? face, Bitmap? hat)> FetchSkinFromMojangAsync(string username)
        {
            try
            {
                var uuidUrl = $"https://api.mojang.com/users/profiles/minecraft/{username}";
                var uuidResponse = await _httpClient.GetStringAsync(uuidUrl);
                var uuidDoc = JsonDocument.Parse(uuidResponse);
                var uuid = uuidDoc.RootElement.GetProperty("id").GetString();

                var profileUrl = $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}";
                var profileResponse = await _httpClient.GetStringAsync(profileUrl);
                var profileDoc = JsonDocument.Parse(profileResponse);
                var properties = profileDoc.RootElement.GetProperty("properties");
                string? skinUrl = null;

                foreach (var prop in properties.EnumerateArray())
                {
                    if (prop.GetProperty("name").GetString() == "textures")
                    {
                        var base64 = prop.GetProperty("value").GetString();
                        if (!string.IsNullOrEmpty(base64))
                        {
                            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                            var texturesDoc = JsonDocument.Parse(json);
                            if (texturesDoc.RootElement.TryGetProperty("textures", out var textures))
                            {
                                if (textures.TryGetProperty("SKIN", out var skin))
                                {
                                    skinUrl = skin.GetProperty("url").GetString();
                                    break;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(skinUrl))
                    return (null, null);

                var response = await _httpClient.GetAsync(skinUrl);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                return await CropSkinAsync(memoryStream);
            }
            catch
            {
                return (null, null);
            }
        }

        private void Close(bool isLogin) => Close();
    }
}