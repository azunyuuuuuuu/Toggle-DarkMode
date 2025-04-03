// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
using DotNetWindowsRegistry;
using Microsoft.Win32;
using PInvoke;

const string PersonalizeRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
const string SystemThemeKey = "SystemUsesLightTheme";
const string AppsRegistryKey = "AppsUseLightTheme";

var registry = new WindowsRegistry();

var currentTheme = GetCurrentTheme(SystemThemeKey);
var newTheme = currentTheme == Theme.Light ? Theme.Dark : Theme.Light;

SetCurrentTheme(SystemThemeKey, newTheme);
SetCurrentTheme(AppsRegistryKey, newTheme);
BroadcastSettingSetChanged("ImmersiveColorSet");

await Task.Delay(1000);
SetCurrentTheme(SystemThemeKey, currentTheme);
await Task.Delay(100);
SetCurrentTheme(SystemThemeKey, newTheme);

Theme GetCurrentTheme(string themeValueName)
{
    using var subKey = OpenPersonalizeSubKey(false);
    var rawValue = subKey.GetValue(themeValueName, Theme.Dark)
        ?? throw new InvalidOperationException(@$"Registry value not found: HKCU:\{PersonalizeRegistryPath}\{themeValueName}");
    return Enum.Parse<Theme>(rawValue.ToString()!);
}

void SetCurrentTheme(string themeValueName, Theme theme)
{
    using var subKey = OpenPersonalizeSubKey(true);
    subKey.SetValue(themeValueName, Convert.ToInt32(theme));
}

static nint BroadcastSettingSetChanged(string settingSetName)
{
    var result = nint.Zero;
    var lParam = Marshal.StringToHGlobalUni(settingSetName);

    try
    {
        User32.SendMessageTimeout(
            hWnd: User32.HWND_BROADCAST,
            msg: User32.WindowMessage.WM_SETTINGCHANGE,
            wParam: nint.Zero,
            lParam: lParam,
            flags: User32.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
            timeout: 100,
            pdwResult: out result);
    }
    finally
    {
        Marshal.FreeHGlobal(lParam);
    }

    return result;
}

IRegistryKey OpenPersonalizeSubKey(bool writable)
{
    var currentUserKey = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
        ?? throw new InvalidOperationException(@$"Registry key not found: HKCU");
    return currentUserKey.OpenSubKey(PersonalizeRegistryPath, writable)
        ?? throw new InvalidOperationException(@$"Registry key not found: HKCU:\{PersonalizeRegistryPath}");
}

enum Theme { Dark = 0, Light = 1 }