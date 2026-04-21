using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class RingDoorbell
{
    private int _ringCount;
    private string _lastSnapshot = "Garage is calm.";

    public RingDoorbell(string deviceId, string displayName)
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
        MotionDetected = false;
        _lastSnapshot = "Driveway is clear and the garage is shut.";
        return _lastSnapshot;
    }

    public void Ring()
    {
        _ringCount++;
        MotionDetected = true;
        _lastSnapshot = "Motion spotted near the garage side entrance.";
    }
}
