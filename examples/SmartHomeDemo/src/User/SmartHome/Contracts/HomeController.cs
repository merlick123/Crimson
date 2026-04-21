using SmartHome.Contracts;

namespace SmartHome.Contracts;

public partial class HomeController
{
    private readonly Dictionary<string, DeviceState> _devices = new(StringComparer.Ordinal)
    {
        ["entry.motion"] = new("Entry motion sensor", new HashSet<DeviceFeature> { DeviceFeature.Motion, DeviceFeature.Automation }, new HashSet<string> { "entry.light" }, "watching"),
        ["entry.light"] = new("Entry light strip", new HashSet<DeviceFeature> { DeviceFeature.Lighting, DeviceFeature.Automation }, new HashSet<string> { "hallway.blinds" }, "brightness=10%"),
        ["hallway.blinds"] = new("Hallway blinds", new HashSet<DeviceFeature> { DeviceFeature.Lighting, DeviceFeature.Automation }, new HashSet<string> { "living.speaker" }, "half-open"),
        ["living.speaker"] = new("Living room speaker", new HashSet<DeviceFeature> { DeviceFeature.Media, DeviceFeature.Automation }, new HashSet<string>(), "quiet"),
        ["hall.thermostat"] = new("Hall thermostat", new HashSet<DeviceFeature> { DeviceFeature.Climate, DeviceFeature.Automation }, new HashSet<string>(), "target=21C"),
        ["garden.sprinkler"] = new("Garden sprinkler", new HashSet<DeviceFeature> { DeviceFeature.Automation, DeviceFeature.Security }, new HashSet<string>(), "standby"),
    };

    public HomeController()
    {
        ControllerId = 101;
        HomeName = "  Orchard House  ";
        ActiveScene = "Daylight";
        TransportId = "mesh://orchard-house";
        AwayMode = false;
    }

    public virtual List<string> ListDevices() =>
        _devices.Keys.OrderBy(static id => id, StringComparer.Ordinal).ToList();

    public virtual HashSet<DeviceFeature> GetSupportedFeatures(string deviceId) =>
        GetDevice(deviceId).Features.ToHashSet();

    public virtual void ConnectDevices(string upstreamDeviceId, string downstreamDeviceId)
    {
        var source = GetDevice(upstreamDeviceId);
        GetDevice(downstreamDeviceId);
        source.LinkedDevices.Add(downstreamDeviceId);
    }

    public virtual List<string> TraceChain(string originDeviceId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        Traverse(originDeviceId, visited, ordered);
        return ordered;
    }

    public virtual void ApplyScene(SceneRequest request)
    {
        ActiveScene = request.SceneName;
        AwayMode = request.AwayModeEnabled;

        foreach (var deviceId in request.TargetDevices)
        {
            var device = GetDevice(deviceId);
            device.Status = device.Features.Contains(DeviceFeature.Lighting)
                ? $"brightness={request.BrightnessPercent}%"
                : device.Features.Contains(DeviceFeature.Media)
                    ? $"playlist={request.SceneName}"
                    : AwayMode
                        ? "armed"
                        : "active";
        }

        if (AwayMode)
        {
            GetDevice("garden.sprinkler").Status = "standby";
        }
    }

    public virtual string DescribeDevice(string deviceId)
    {
        var device = GetDevice(deviceId);
        var features = string.Join(", ", device.Features.OrderBy(feature => feature.ToString(), StringComparer.Ordinal));
        var links = device.LinkedDevices.Count == 0
            ? "none"
            : string.Join(" -> ", device.LinkedDevices.OrderBy(static id => id, StringComparer.Ordinal));
        return $"{device.DisplayName} [{features}] status={device.Status} links={links}";
    }

    partial void OnHomeNameSetting(ref string value)
    {
        value = value.Trim();
    }

    partial void OnActiveSceneSetting(ref string value)
    {
        value = value.Trim();
    }

    private void Traverse(string deviceId, HashSet<string> visited, List<string> ordered)
    {
        var device = GetDevice(deviceId);
        if (!visited.Add(deviceId))
        {
            return;
        }

        ordered.Add(deviceId);
        foreach (var linked in device.LinkedDevices.OrderBy(static id => id, StringComparer.Ordinal))
        {
            Traverse(linked, visited, ordered);
        }
    }

    private DeviceState GetDevice(string deviceId)
    {
        if (!_devices.TryGetValue(deviceId, out var device))
        {
            throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId, "Unknown device id.");
        }

        return device;
    }

    private sealed class DeviceState(
        string displayName,
        HashSet<DeviceFeature> features,
        HashSet<string> linkedDevices,
        string status)
    {
        public string DisplayName { get; } = displayName;
        public HashSet<DeviceFeature> Features { get; } = features;
        public HashSet<string> LinkedDevices { get; } = linkedDevices;
        public string Status { get; set; } = status;
    }
}
