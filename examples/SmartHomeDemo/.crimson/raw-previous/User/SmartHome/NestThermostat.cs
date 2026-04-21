using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class NestThermostat
{
    public NestThermostat()
    {
    }

    /// <summary>Reports the static capabilities supported by this device.</summary>
    public virtual HashSet<DeviceFeature> GetSupportedFeatures()
    {
        throw new NotImplementedException();
    }

    /// <summary>Describes the current device state.</summary>
    public virtual string DescribeState()
    {
        throw new NotImplementedException();
    }

    /// <summary>Reads the current measured temperature.</summary>
    public virtual double ReadTemperature()
    {
        throw new NotImplementedException();
    }

}
