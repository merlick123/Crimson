using System.Reflection;
using SmartHome.Contracts;

var request = new SceneRequest
{
    SceneName = "  Evening Arrival  ",
    TargetDevices = new List<string>
    {
        "entry.light",
        "hallway.blinds",
        "living.speaker",
    },
    AwayModeEnabled = false,
    BrightnessPercent = 42,
};

var controllers = DiscoverControllers().ToArray();
Console.WriteLine($"Discovered {controllers.Length} interchangeable home controllers.");
Console.WriteLine();

foreach (var controller in controllers)
{
    RunScenario(controller, request);
}

static IEnumerable<IHomeController> DiscoverControllers()
{
    return Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(type => typeof(IHomeController).IsAssignableFrom(type))
        .Where(type => !type.IsAbstract && !type.IsInterface)
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .Select(type => (IHomeController)Activator.CreateInstance(type)!);
}

static void RunScenario(IHomeController controller, SceneRequest request)
{
    Console.WriteLine($"== {controller.GetType().Name} ==");
    Console.WriteLine($"Home: {controller.HomeName}");
    Console.WriteLine($"Controller id: {controller.ControllerId}");
    Console.WriteLine($"Scene before apply: {controller.ActiveScene}");
    Console.WriteLine($"Devices: {string.Join(", ", controller.ListDevices())}");

    var focusDevice = "entry.motion";
    Console.WriteLine($"Features for {focusDevice}: {string.Join(", ", controller.GetSupportedFeatures(focusDevice).OrderBy(feature => feature.ToString(), StringComparer.Ordinal))}");

    controller.ConnectDevices("entry.motion", "garden.sprinkler");
    Console.WriteLine($"Chain from {focusDevice}: {string.Join(" -> ", controller.TraceChain(focusDevice))}");

    controller.ApplyScene(request);
    Console.WriteLine($"Scene after apply: {controller.ActiveScene}");
    Console.WriteLine($"Away mode: {controller.AwayMode}");
    Console.WriteLine($"entry.light: {controller.DescribeDevice("entry.light")}");
    Console.WriteLine($"garden.sprinkler: {controller.DescribeDevice("garden.sprinkler")}");
    Console.WriteLine();
}
