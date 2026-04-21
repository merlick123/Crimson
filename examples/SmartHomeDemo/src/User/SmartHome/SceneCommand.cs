using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHome;

public partial class SceneCommand
{
    partial void OnSceneNameSetting(ref string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Unnamed Scene" : value.Trim();
    }

    partial void OnTargetDeviceIdsSetting(ref List<string> value)
    {
        value = (value ?? [])
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    partial void OnBrightnessPercentSetting(ref int value)
    {
        value = Math.Clamp(value, 0, 100);
    }

    partial void OnAnnouncementSetting(ref string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Welcome home." : value.Trim();
    }
}
