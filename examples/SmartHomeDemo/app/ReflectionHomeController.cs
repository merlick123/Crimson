using System.Reflection;
using SmartHome.Contracts;

public sealed class ReflectionHomeController : IHomeController
{
    private readonly Dictionary<string, ReflectedDeviceModel> _devices;
    private readonly Dictionary<string, HashSet<string>> _links;

    public ReflectionHomeController()
    {
        ControllerId = 9002;
        HomeName = "Reflection House";
        ActiveScene = "idle";
        AwayMode = false;

        _devices = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Select(type => (Type: type, Device: type.GetCustomAttribute<SmartDeviceAttribute>()))
            .Where(entry => entry.Device is not null)
            .OrderBy(entry => entry.Device!.DeviceId, StringComparer.Ordinal)
            .ToDictionary(
                entry => entry.Device!.DeviceId,
                entry => new ReflectedDeviceModel(
                    entry.Device!.DisplayName,
                    new HashSet<DeviceFeature>(entry.Device!.Features),
                    entry.Type.GetCustomAttributes<DeviceLinkAttribute>().Select(attribute => attribute.TargetDeviceId).ToHashSet(StringComparer.Ordinal),
                    "standby"),
                StringComparer.Ordinal);

        _links = _devices.ToDictionary(
            entry => entry.Key,
            entry => new HashSet<string>(entry.Value.LinkedDevices, StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    public long ControllerId { get; }

    public string HomeName { get; set; }

    public string ActiveScene { get; set; }

    public bool AwayMode { get; set; }

    public List<string> ListDevices() =>
        _devices.Keys.OrderBy(static id => id, StringComparer.Ordinal).ToList();

    public HashSet<DeviceFeature> GetSupportedFeatures(string deviceId) =>
        GetDevice(deviceId).Features.ToHashSet();

    public void ConnectDevices(string upstreamDeviceId, string downstreamDeviceId)
    {
        EnsureDeviceExists(upstreamDeviceId);
        EnsureDeviceExists(downstreamDeviceId);
        _links[upstreamDeviceId].Add(downstreamDeviceId);
    }

    public List<string> TraceChain(string originDeviceId)
    {
        EnsureDeviceExists(originDeviceId);

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        Traverse(originDeviceId, visited, ordered);
        return ordered;
    }

    public void ApplyScene(SceneRequest request)
    {
        ActiveScene = request.SceneName;
        AwayMode = request.AwayModeEnabled;

        foreach (var deviceId in request.TargetDevices)
        {
            EnsureDeviceExists(deviceId);
            var device = GetDevice(deviceId);
            var nextStatus = device.Features.Contains(DeviceFeature.Lighting)
                ? $"brightness={request.BrightnessPercent}%"
                : device.Features.Contains(DeviceFeature.Media)
                    ? $"playlist={request.SceneName}"
                    : AwayMode
                        ? "armed"
                        : "active";

            _devices[deviceId] = device with { Status = nextStatus };
        }
    }

    public string DescribeDevice(string deviceId)
    {
        var device = GetDevice(deviceId);
        var features = string.Join(", ", device.Features.OrderBy(feature => feature.ToString(), StringComparer.Ordinal));
        var links = _links[deviceId].Count == 0
            ? "none"
            : string.Join(" -> ", _links[deviceId].OrderBy(static id => id, StringComparer.Ordinal));
        return $"{device.DisplayName} [{features}] status={device.Status} links={links}";
    }

    private void Traverse(string deviceId, HashSet<string> visited, List<string> ordered)
    {
        if (!visited.Add(deviceId))
        {
            return;
        }

        ordered.Add(deviceId);
        foreach (var linked in _links[deviceId].OrderBy(static id => id, StringComparer.Ordinal))
        {
            Traverse(linked, visited, ordered);
        }
    }

    private ReflectedDeviceModel GetDevice(string deviceId)
    {
        EnsureDeviceExists(deviceId);
        return _devices[deviceId];
    }

    private void EnsureDeviceExists(string deviceId)
    {
        if (!_devices.ContainsKey(deviceId))
        {
            throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId, "Unknown device id.");
        }
    }

    private sealed record ReflectedDeviceModel(
        string DisplayName,
        HashSet<DeviceFeature> Features,
        HashSet<string> LinkedDevices,
        string Status);
}

[AttributeUsage(AttributeTargets.Class)]
file sealed class SmartDeviceAttribute(string deviceId, string displayName, params DeviceFeature[] features) : Attribute
{
    public string DeviceId { get; } = deviceId;
    public string DisplayName { get; } = displayName;
    public IReadOnlyList<DeviceFeature> Features { get; } = features;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
file sealed class DeviceLinkAttribute(string targetDeviceId) : Attribute
{
    public string TargetDeviceId { get; } = targetDeviceId;
}

[SmartDevice("entry.motion", "Entry motion sensor", DeviceFeature.Motion, DeviceFeature.Automation)]
[DeviceLink("entry.light")]
file sealed class EntryMotionSensor;

[SmartDevice("entry.light", "Entry light strip", DeviceFeature.Lighting, DeviceFeature.Automation)]
[DeviceLink("hallway.blinds")]
file sealed class EntryLight;

[SmartDevice("hallway.blinds", "Hallway blinds", DeviceFeature.Lighting, DeviceFeature.Automation)]
[DeviceLink("living.speaker")]
file sealed class HallwayBlinds;

[SmartDevice("living.speaker", "Living room speaker", DeviceFeature.Media, DeviceFeature.Automation)]
file sealed class LivingSpeaker;

[SmartDevice("hall.thermostat", "Hall thermostat", DeviceFeature.Climate, DeviceFeature.Automation)]
file sealed class HallThermostat;

[SmartDevice("garden.sprinkler", "Garden sprinkler", DeviceFeature.Automation, DeviceFeature.Security)]
file sealed class GardenSprinkler;
