#include <algorithm>
#include <iostream>
#include <memory>
#include <sstream>
#include <string>
#include <vector>

#include "SmartHome/DemoHomeRuntime.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/ICamera.g.hpp"
#include "SmartHome/IDemoHomeRuntime.g.hpp"
#include "SmartHome/IDoorbell.g.hpp"
#include "SmartHome/IDevice.g.hpp"
#include "SmartHome/IMotionSensor.g.hpp"
#include "SmartHome/ISceneCommand.g.hpp"
#include "SmartHome/ISpeaker.g.hpp"
#include "SmartHome/IThermostat.g.hpp"
#include "SmartHome/SceneCommand.hpp"

namespace
{
std::string FeatureName(SmartHome::DeviceFeature feature)
{
    switch (feature)
    {
        case SmartHome::DeviceFeature::Camera:
            return "Camera";
        case SmartHome::DeviceFeature::Motion:
            return "Motion";
        case SmartHome::DeviceFeature::Doorbell:
            return "Doorbell";
        case SmartHome::DeviceFeature::Lighting:
            return "Lighting";
        case SmartHome::DeviceFeature::Climate:
            return "Climate";
        case SmartHome::DeviceFeature::Speaker:
            return "Speaker";
        case SmartHome::DeviceFeature::Automation:
            return "Automation";
    }

    return "Unknown";
}

std::string Join(const std::vector<std::string>& values, const std::string& separator)
{
    std::ostringstream builder;
    for (std::size_t index = 0; index < values.size(); ++index)
    {
        if (index > 0)
        {
            builder << separator;
        }

        builder << values[index];
    }

    return builder.str();
}

void RunScenario(SmartHome::IDemoHomeRuntime& runtime, const ::Crimson::Cpp::InterfaceHandle<SmartHome::ISceneCommand>& command)
{
    std::cout << "Home: " << runtime.GetHomeName() << '\n';
    std::cout << "Scene before apply: " << runtime.GetActiveScene() << '\n';
    std::cout << "Away mode before apply: " << std::boolalpha << runtime.GetAwayMode() << "\n\n";

    const auto devices = runtime.ListDevices();
    std::cout << "Registered devices:\n";
    for (const auto& device : devices)
    {
        std::vector<std::string> features;
        for (const auto feature : device->GetSupportedFeatures())
        {
            features.push_back(FeatureName(feature));
        }

        std::sort(features.begin(), features.end());
        std::cout << "- " << device->GetDeviceId() << " :: " << device->GetDisplayName() << " :: " << Join(features, ", ") << '\n';
    }

    std::cout << "\nDoorbell-capable devices discovered through the shared runtime contract:\n";
    for (const auto& device : runtime.FindDevices(SmartHome::DeviceFeature::Doorbell))
    {
        if (const auto doorbell = std::dynamic_pointer_cast<SmartHome::IDoorbell>(device))
        {
            doorbell->Ring();
            std::cout << "- " << device->GetDisplayName() << " rang without the caller knowing the concrete vendor type.\n";
        }
    }

    std::cout << '\n';
    runtime.ConnectDevices("porch.eufy", "hall.hue");
    runtime.ConnectDevices("hall.hue", "living.sonos");
    runtime.ConnectDevices("garage.ring", "living.sonos");
    std::cout << "Automation chain from porch.eufy:\n";

    std::vector<std::string> traced;
    for (const auto& deviceId : runtime.TraceChain("porch.eufy"))
    {
        traced.push_back(deviceId);
    }

    std::cout << "  " << Join(traced, " -> ") << "\n\n";
    std::cout << "Capability queries:\n";
    for (const auto& device : devices)
    {
        if (const auto camera = std::dynamic_pointer_cast<SmartHome::ICamera>(device))
        {
            std::cout << "- camera snapshot from " << device->GetDisplayName() << ": " << camera->CaptureSnapshot() << '\n';
        }

        if (const auto motionSensor = std::dynamic_pointer_cast<SmartHome::IMotionSensor>(device))
        {
            std::cout << "- motion state for " << device->GetDisplayName() << ": " << std::boolalpha << motionSensor->GetMotionDetected() << '\n';
        }

        if (const auto thermostat = std::dynamic_pointer_cast<SmartHome::IThermostat>(device))
        {
            thermostat->SetTargetTemperature(20.5);
            std::ostringstream builder;
            builder.setf(std::ios::fixed);
            builder.precision(1);
            builder << "- thermostat " << device->GetDisplayName()
                << ": target=" << thermostat->GetTargetTemperature()
                << "C current=" << thermostat->ReadTemperature() << "C";
            std::cout << builder.str() << '\n';
        }
    }

    std::cout << '\n';
    runtime.ApplyScene(command);
    std::cout << "Scene after apply: " << runtime.GetActiveScene() << '\n';
    std::cout << "Away mode after apply: " << std::boolalpha << runtime.GetAwayMode() << "\n\n";

    std::cout << "Device state after scene:\n";
    for (const auto& device : devices)
    {
        std::cout << "- " << device->GetDisplayName() << ": " << device->DescribeState() << '\n';
    }
}
}

int main()
{
    SmartHome::DemoHomeRuntime runtime;
    runtime.SetHomeName("Willow Lane");

    auto command = std::make_shared<SmartHome::SceneCommand>();
    command->SetSceneName("  Evening Arrival  ");
    command->SetTargetDeviceIds({"porch.eufy", "upstairs.nest"});
    command->SetBrightnessPercent(42);
    command->SetAnnouncement("Welcome home. Evening mode is active.");
    command->SetAwayModeEnabled(false);

    RunScenario(runtime, command);
    return 0;
}
