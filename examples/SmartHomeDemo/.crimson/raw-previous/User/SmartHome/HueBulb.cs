using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class HueBulb
{
    public HueBulb()
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

    /// <summary>Sets the light brightness level.</summary>
    /// <param name="value">The requested brightness level.</param>
    public virtual void SetBrightness(int value)
    {
        throw new NotImplementedException();
    }

}
