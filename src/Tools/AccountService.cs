using System;
using System.IO;
using System.Text.Json;

namespace Project.Launch.Tools
{
    public static class AccountService
    {
        private static readonly string LauncherDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "KilimLauncher"
        );
        private static readonly string AccountsFile = Path.Combine(LauncherDir, "accounts.json");
        private static readonly object _lock = new object();

        static AccountService()
        {
            if (!Directory.Exists(LauncherDir))
                Directory.CreateDirectory(LauncherDir);
        }

        public static AccountsRoot LoadAccounts()
        {
            lock (_lock)
            {
                if (!File.Exists(AccountsFile))
                    return new AccountsRoot();
                try
                {
                    string json = File.ReadAllText(AccountsFile);
                    return JsonSerializer.Deserialize<AccountsRoot>(json) ?? new AccountsRoot();
                }
                catch
                {
                    return new AccountsRoot();
                }
            }
        }

        public static void SaveAccounts(AccountsRoot root)
        {
            lock (_lock)
            {
                string json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AccountsFile, json);
            }
        }

        public static AccountInfo? GetSelectedAccount()
        {
            var root = LoadAccounts();
            if (string.IsNullOrEmpty(root.SelectedAccount) || !root.Accounts.ContainsKey(root.SelectedAccount))
                return null;
            return root.Accounts[root.SelectedAccount];
        }

        public static void SetSelectedAccount(string accountId, AccountInfo account)
        {
            var root = LoadAccounts();
            root.Accounts[accountId] = account;
            root.SelectedAccount = accountId;
            SaveAccounts(root);
        }

        public static void RemoveAccount(string accountId)
        {
            var root = LoadAccounts();
            if (root.Accounts.Remove(accountId) && root.SelectedAccount == accountId)
                root.SelectedAccount = string.Empty;
            SaveAccounts(root);
        }

        // ★ 新增：读取 LoginCheck
        public static bool GetLoginCheck()
        {
            var root = LoadAccounts();
            return root.LoginCheck;
        }

        // ★ 新增：写入 LoginCheck
        public static void SetLoginCheck(bool value)
        {
            var root = LoadAccounts();
            root.LoginCheck = value;
            SaveAccounts(root);
        }
    }
}