using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using Speech2TextAssistant.Models;

namespace Speech2TextAssistant;

public class ConfigManager
{
    private static readonly string ConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speech2TextAssistant",
            "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                // 解密API密钥
                if (settings != null)
                {
                    if (!string.IsNullOrEmpty(settings.OpenAIApiKey))
                        settings.OpenAIApiKey = DecryptString(settings.OpenAIApiKey);
                    if (!string.IsNullOrEmpty(settings.AzureSpeechKey))
                        settings.AzureSpeechKey = DecryptString(settings.AzureSpeechKey);

                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载配置失败: {ex.Message}", "错误");
        }

        return new AppSettings();
    }

    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            var configDir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(configDir))
                Directory.CreateDirectory(configDir);

            // 加密API密钥
            var settingsToSave = new AppSettings
            {
                HotKey = settings.HotKey,
                OpenAIApiKey = string.IsNullOrEmpty(settings.OpenAIApiKey) ? "" : EncryptString(settings.OpenAIApiKey),
                DefaultProcessingMode = settings.DefaultProcessingMode,
                ModelName = settings.ModelName,
                AzureSpeechKey = string.IsNullOrEmpty(settings.AzureSpeechKey)
                    ? ""
                    : EncryptString(settings.AzureSpeechKey),
                AzureSpeechRegion = settings.AzureSpeechRegion,
                PlaySounds = settings.PlaySounds,
                RecordingTimeoutSeconds = settings.RecordingTimeoutSeconds,
                RecordingMode = settings.RecordingMode,
                StartWithWindows = settings.StartWithWindows,
                MinimizeToTray = settings.MinimizeToTray,
                ShowTrayNotifications = settings.ShowTrayNotifications
            };

            var json = JsonSerializer.Serialize(settingsToSave, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误");
        }
    }

    private static string EncryptString(string plainText)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return plainText; // 如果加密失败，返回原文
        }
    }

    private static string DecryptString(string encryptedText)
    {
        try
        {
            var data = Convert.FromBase64String(encryptedText);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return encryptedText; // 如果解密失败，返回原文
        }
    }
}