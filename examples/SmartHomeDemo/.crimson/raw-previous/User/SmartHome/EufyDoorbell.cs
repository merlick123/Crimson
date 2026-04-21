using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class EufyDoorbell
{
    public EufyDoorbell()
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

    /// <summary>Captures a short human-readable snapshot.</summary>
    public virtual string CaptureSnapshot()
    {
        throw new NotImplementedException();
    }

    /// <summary>Rings or chimes the doorbell.</summary>
    public virtual void Ring()
    {
        throw new NotImplementedException();
    }

}
