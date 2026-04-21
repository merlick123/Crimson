using System;

namespace SmartHome.Contracts;

public partial class HomeController
{
    public HomeController()
    {
    }

    /// <summary>Lists every known device in the home.</summary>
    public virtual List<string> ListDevices()
    {
        throw new NotImplementedException();
    }

    /// <summary>Returns the supported features for a specific device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    public virtual HashSet<DeviceFeature> GetSupportedFeatures(string deviceId)
    {
        throw new NotImplementedException();
    }

    /// <summary>Connects one device's automation output to another device.</summary>
    /// <param name="upstreamDeviceId">The source device identifier.</param>
    /// <param name="downstreamDeviceId">The destination device identifier.</param>
    public virtual void ConnectDevices(string upstreamDeviceId, string downstreamDeviceId)
    {
        throw new NotImplementedException();
    }

    /// <summary>Traces an automation chain from a starting device.</summary>
    /// <param name="originDeviceId">The starting device identifier.</param>
    /// <returns>The ordered device chain.</returns>
    public virtual List<string> TraceChain(string originDeviceId)
    {
        throw new NotImplementedException();
    }

    /// <summary>Applies a named scene across the selected devices.</summary>
    /// <param name="request">The scene request.</param>
    public virtual void ApplyScene(SceneRequest request)
    {
        throw new NotImplementedException();
    }

    /// <summary>Describes the current state of a specific device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>A human-readable device summary.</returns>
    public virtual string DescribeDevice(string deviceId)
    {
        throw new NotImplementedException();
    }

}
