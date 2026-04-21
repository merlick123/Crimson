using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class NestThermostat
{
    private double _currentTemperature = 19.3;

    public NestThermostat(string deviceId, string displayName)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
    }

    public HashSet<DeviceFeature> GetSupportedFeatures()
    {
        return
        [
            DeviceFeature.Climate,
            DeviceFeature.Automation,
        ];
    }

    public string DescribeState()
    {
        return $"target={TargetTemperature:F1}C current={ReadTemperature():F1}C";
    }

    public double ReadTemperature()
    {
        _currentTemperature = Math.Round((_currentTemperature + TargetTemperature) / 2.0, 1);
        return _currentTemperature;
    }
}
