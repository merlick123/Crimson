using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class HueBulb
{
    public HueBulb(string deviceId, string displayName)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
    }

    public HashSet<DeviceFeature> GetSupportedFeatures()
    {
        return
        [
            DeviceFeature.Lighting,
            DeviceFeature.Automation,
        ];
    }

    public string DescribeState()
    {
        return BrightnessPercent == 0
            ? "off"
            : $"on at {BrightnessPercent}% brightness";
    }

    public void SetBrightness(int value)
    {
        BrightnessPercent = Math.Clamp(value, 0, 100);
    }
}
