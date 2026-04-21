using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class SonosSpeaker
{
    private string _lastAnnouncement = "Silence.";

    public SonosSpeaker(string deviceId, string displayName)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
    }

    public HashSet<DeviceFeature> GetSupportedFeatures()
    {
        return
        [
            DeviceFeature.Speaker,
            DeviceFeature.Automation,
        ];
    }

    public string DescribeState()
    {
        return $"last announcement=\"{_lastAnnouncement}\"";
    }

    public void PlayAnnouncement(string message)
    {
        _lastAnnouncement = string.IsNullOrWhiteSpace(message) ? "Silent update." : message.Trim();
    }
}
