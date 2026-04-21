using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHome;

public partial class DemoHomeRuntime
{
    private readonly Dictionary<string, IDevice> _devices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _connections = new(StringComparer.Ordinal);

    public DemoHomeRuntime()
    {
        RegisterDevice(new EufyDoorbell("porch.eufy", "Front Porch Eufy Doorbell"));
        RegisterDevice(new RingDoorbell("garage.ring", "Garage Ring Doorbell"));
        RegisterDevice(new HueBulb("hall.hue", "Hallway Hue Bulb"));
        RegisterDevice(new NestThermostat("upstairs.nest", "Upstairs Nest Thermostat"));
        RegisterDevice(new SonosSpeaker("living.sonos", "Living Room Sonos"));
    }

    public void RegisterDevice(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (string.IsNullOrWhiteSpace(device.DeviceId))
        {
            throw new InvalidOperationException("Registered devices must have a stable device id.");
        }

        _devices[device.DeviceId] = device;
    }

    public List<IDevice> ListDevices()
    {
        return _devices.Values
            .OrderBy(static device => device.DeviceId, StringComparer.Ordinal)
            .ToList();
    }

    public IDevice GetDevice(string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            return device;
        }

        throw new KeyNotFoundException($"Device '{deviceId}' is not registered.");
    }

    public List<IDevice> FindDevices(DeviceFeature feature)
    {
        return ListDevices()
            .Where(device => device.GetSupportedFeatures().Contains(feature))
            .ToList();
    }

    public void ConnectDevices(string upstreamDeviceId, string downstreamDeviceId)
    {
        _ = GetDevice(upstreamDeviceId);
        _ = GetDevice(downstreamDeviceId);

        if (!_connections.TryGetValue(upstreamDeviceId, out var downstream))
        {
            downstream = [];
            _connections[upstreamDeviceId] = downstream;
        }

        if (!downstream.Contains(downstreamDeviceId, StringComparer.Ordinal))
        {
            downstream.Add(downstreamDeviceId);
        }
    }

    public List<string> TraceChain(string originDeviceId)
    {
        _ = GetDevice(originDeviceId);

        var chain = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        TraceFrom(originDeviceId, chain, visited);
        return chain;
    }

    public void ApplyScene(ISceneCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        ActiveScene = command.SceneName;
        AwayMode = command.AwayModeEnabled;

        var affectedDeviceIds = command.TargetDeviceIds
            .SelectMany(TraceChain)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var deviceId in affectedDeviceIds)
        {
            var device = GetDevice(deviceId);

            if (device is ILight light)
            {
                light.SetBrightness(command.BrightnessPercent);
            }

            if (device is ISpeaker speaker)
            {
                speaker.PlayAnnouncement(command.Announcement);
            }

            if (device is IThermostat thermostat)
            {
                thermostat.TargetTemperature = command.AwayModeEnabled ? 17.5 : 20.5;
            }
        }
    }

    private void TraceFrom(string originDeviceId, ICollection<string> chain, ISet<string> visited)
    {
        if (!visited.Add(originDeviceId))
        {
            return;
        }

        chain.Add(originDeviceId);

        if (!_connections.TryGetValue(originDeviceId, out var downstream))
        {
            return;
        }

        foreach (var next in downstream.OrderBy(static value => value, StringComparer.Ordinal))
        {
            TraceFrom(next, chain, visited);
        }
    }
}
