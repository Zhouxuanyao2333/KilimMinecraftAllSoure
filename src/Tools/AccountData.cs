using System;
using System.Collections.Generic;

namespace Project.Launch.Tools
{
    public class AccountInfo
    {
        public string Type { get; set; } = "offline";
        public string Username { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }

    public class AccountsRoot
    {
        public Dictionary<string, AccountInfo> Accounts { get; set; } = new();
        public string SelectedAccount { get; set; } = string.Empty;
        // ★ 新增：是否跳过登录窗口
        public bool LoginCheck { get; set; } = false;
    }
}