using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class EufyDoorbell
{
    private int _ringCount;
    private string _lastSnapshot = "Porch is quiet.";

    public EufyDoorbell(string deviceId, string displayName)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
    }

    public HashSet<DeviceFeature> GetSupportedFeatures()
    {
        return
        [
            DeviceFeature.Camera,
            DeviceFeature.Motion,
            DeviceFeature.Doorbell,
            DeviceFeature.Automation,
        ];
    }

    public string DescribeState()
    {
        return $"motion={(MotionDetected ? "detected" : "clear")}, rings={_ringCount}, snapshot=\"{_lastSnapshot}\"";
    }

    public string CaptureSnapshot()
    {
        MotionDetected = true;
        _lastSnapshot = "Package courier visible at the porch.";
        return _lastSnapshot;
    }

    public void Ring()
    {
        _ringCount++;
        MotionDetected = true;
        _lastSnapshot = "Visitor pressed the porch doorbell.";
    }
}
