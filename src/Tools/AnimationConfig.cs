using System;

namespace Project.Launch.Tools
{
    /// <summary>
    /// 全局动画参数配置
    /// 后期"设置"页面只需修改这里的属性值即可统一调整动画手感
    /// </summary>
    public static class AnimationConfig
    {
        // ============================================================
        // 登录滑块动画
        // ============================================================

        /// <summary>
        /// 滑块移动总时长（毫秒）
        /// </summary>
        public static TimeSpan SliderMoveDuration { get; set; } = TimeSpan.FromMilliseconds(700);

        /// <summary>
        /// 滑块移动过程中放大的峰值倍数（1.0 = 不放大）
        /// </summary>
        public static double SliderPeakScale { get; set; } = 1.6;

        /// <summary>
        /// 缩放曲线的高斯 σ（控制峰值宽度）
        /// 越小峰越尖，越大峰越宽
        /// </summary>
        public static double SliderScaleSigma { get; set; } = 0.18;

        /// <summary>
        /// 滑块移动过程中的最大模糊值（0 = 不模糊，0.6 = 中等模糊）
        /// </summary>
        public static double SliderBlurPeak { get; set; } = 0.6;

        /// <summary>
        /// 滑块宽度（用于缩放中心补偿，必须和 XAML 里的 Width 一致）
        /// </summary>
        public static double SliderWidth { get; set; } = 114;

        /// <summary>
        /// 滑块高度（用于缩放中心补偿，必须和 XAML 里的 Height 一致）
        /// </summary>
        public static double SliderHeight { get; set; } = 36;

        // ============================================================
        // 输入面板淡出淡入
        // ============================================================

        /// <summary>
        /// 输入面板淡出时长（毫秒）
        /// </summary>
        public static TimeSpan InputPanelFadeDuration { get; set; } = TimeSpan.FromMilliseconds(250);
    }
}