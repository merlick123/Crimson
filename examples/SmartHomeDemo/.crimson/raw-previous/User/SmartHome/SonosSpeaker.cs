using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class SonosSpeaker
{
    public SonosSpeaker()
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

    /// <summary>Plays or queues an announcement.</summary>
    /// <param name="message">The message to speak.</param>
    public virtual void PlayAnnouncement(string message)
    {
        throw new NotImplementedException();
    }

}
