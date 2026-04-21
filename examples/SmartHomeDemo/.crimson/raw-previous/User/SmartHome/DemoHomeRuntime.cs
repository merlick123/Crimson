using System;
using System.Collections.Generic;

namespace SmartHome;

public partial class DemoHomeRuntime
{
    public DemoHomeRuntime()
    {
    }

    /// <summary>Registers a device with the runtime.</summary>
    /// <param name="device">The device instance to register.</param>
    public virtual void RegisterDevice(IDevice device)
    {
        throw new NotImplementedException();
    }

    /// <summary>Lists every registered device.</summary>
    public virtual List<IDevice> ListDevices()
    {
        throw new NotImplementedException();
    }

    /// <summary>Fetches a device by identifier.</summary>
    /// <param name="deviceId">The device identifier.</param>
    public virtual IDevice GetDevice(string deviceId)
    {
        throw new NotImplementedException();
    }

    /// <summary>Finds all devices that advertise a specific capability.</summary>
    /// <param name="feature">The feature to match.</param>
    /// <returns>Matching devices.</returns>
    public virtual List<IDevice> FindDevices(DeviceFeature feature)
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

    /// <summary>Applies a scene across the selected devices.</summary>
    /// <param name="command">The scene command.</param>
    public virtual void ApplyScene(ISceneCommand command)
    {
        throw new NotImplementedException();
    }

}
