using SmartHome;

var runtime = new DemoHomeRuntime
{
    HomeName = "Willow Lane",
};

var command = new SceneCommand
{
    SceneName = "  Evening Arrival  ",
    TargetDeviceIds = new List<string>
    {
        "porch.eufy",
        "upstairs.nest",
    },
    BrightnessPercent = 42,
    Announcement = "Welcome home. Evening mode is active.",
    AwayModeEnabled = false,
};

RunScenario(runtime, command);

static void RunScenario(IDemoHomeRuntime runtime, ISceneCommand command)
{
    Console.WriteLine($"Home: {runtime.HomeName}");
    Console.WriteLine($"Scene before apply: {runtime.ActiveScene}");
    Console.WriteLine($"Away mode before apply: {runtime.AwayMode}");
    Console.WriteLine();

    var devices = runtime.ListDevices();
    Console.WriteLine("Registered devices:");
    foreach (var device in devices)
    {
        Console.WriteLine($"- {device.DeviceId} :: {device.DisplayName} :: {string.Join(", ", device.GetSupportedFeatures().OrderBy(feature => feature.ToString(), StringComparer.Ordinal))}");
    }

    Console.WriteLine();
    Console.WriteLine("Doorbell-capable devices discovered through the shared runtime contract:");
    foreach (var device in runtime.FindDevices(DeviceFeature.Doorbell))
    {
        if (device is IDoorbell doorbell)
        {
            doorbell.Ring();
            Console.WriteLine($"- {device.DisplayName} rang without the caller knowing the concrete vendor type.");
        }
    }

    Console.WriteLine();
    runtime.ConnectDevices("porch.eufy", "hall.hue");
    runtime.ConnectDevices("hall.hue", "living.sonos");
    runtime.ConnectDevices("garage.ring", "living.sonos");
    Console.WriteLine("Automation chain from porch.eufy:");
    Console.WriteLine($"  {string.Join(" -> ", runtime.TraceChain("porch.eufy"))}");
    Console.WriteLine();

    Console.WriteLine("Capability queries:");
    foreach (var device in devices)
    {
        if (device is ICamera camera)
        {
            Console.WriteLine($"- camera snapshot from {device.DisplayName}: {camera.CaptureSnapshot()}");
        }

        if (device is IMotionSensor motionSensor)
        {
            Console.WriteLine($"- motion state for {device.DisplayName}: {motionSensor.MotionDetected}");
        }

        if (device is IThermostat thermostat)
        {
            thermostat.TargetTemperature = 20.5;
            Console.WriteLine($"- thermostat {device.DisplayName}: target={thermostat.TargetTemperature:F1}C current={thermostat.ReadTemperature():F1}C");
        }
    }

    Console.WriteLine();
    runtime.ApplyScene(command);
    Console.WriteLine($"Scene after apply: {runtime.ActiveScene}");
    Console.WriteLine($"Away mode after apply: {runtime.AwayMode}");
    Console.WriteLine();

    Console.WriteLine("Device state after scene:");
    foreach (var device in devices)
    {
        Console.WriteLine($"- {device.DisplayName}: {device.DescribeState()}");
    }
}
