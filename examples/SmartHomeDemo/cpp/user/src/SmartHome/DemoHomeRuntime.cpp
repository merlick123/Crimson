#include "SmartHome/DemoHomeRuntime.hpp"

#include <algorithm>
#include <memory>
#include <stdexcept>
#include <utility>

#include "SmartHome/EufyDoorbell.hpp"
#include "SmartHome/HueBulb.hpp"
#include "SmartHome/ILight.g.hpp"
#include "SmartHome/ISpeaker.g.hpp"
#include "SmartHome/IThermostat.g.hpp"
#include "SmartHome/NestThermostat.hpp"
#include "SmartHome/RingDoorbell.hpp"
#include "SmartHome/SonosSpeaker.hpp"

namespace SmartHome
{

namespace
{
template <typename TItem>
void PushUnique(::Crimson::Cpp::List<TItem>& items, TItem value)
{
    if (std::find(items.begin(), items.end(), value) == items.end())
    {
        items.push_back(std::move(value));
    }
}
}

DemoHomeRuntime::DemoHomeRuntime()
{
    RegisterDevice(std::static_pointer_cast<IDevice>(std::make_shared<EufyDoorbell>("porch.eufy", "Front Porch Eufy Doorbell")));
    RegisterDevice(std::static_pointer_cast<IDevice>(std::make_shared<RingDoorbell>("garage.ring", "Garage Ring Doorbell")));
    RegisterDevice(std::static_pointer_cast<IDevice>(std::make_shared<HueBulb>("hall.hue", "Hallway Hue Bulb")));
    RegisterDevice(std::static_pointer_cast<IDevice>(std::make_shared<NestThermostat>("upstairs.nest", "Upstairs Nest Thermostat")));
    RegisterDevice(std::static_pointer_cast<IDevice>(std::make_shared<SonosSpeaker>("living.sonos", "Living Room Sonos")));
}

void DemoHomeRuntime::RegisterDevice(::Crimson::Cpp::InterfaceHandle<IDevice> device)
{
    if (device == nullptr)
    {
        throw std::invalid_argument("Registered devices must not be null.");
    }

    auto deviceId = device->GetDeviceId();
    if (deviceId.empty())
    {
        throw std::runtime_error("Registered devices must have a stable device id.");
    }

    devices_[deviceId] = std::move(device);
}

::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> DemoHomeRuntime::ListDevices()
{
    ::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> devices;
    for (const auto& [_, device] : devices_)
    {
        devices.push_back(device);
    }

    return devices;
}

::Crimson::Cpp::InterfaceHandle<IDevice> DemoHomeRuntime::GetDevice(::Crimson::Cpp::String deviceId)
{
    if (const auto iterator = devices_.find(deviceId); iterator != devices_.end())
    {
        return iterator->second;
    }

    throw std::out_of_range("Device '" + deviceId + "' is not registered.");
}

::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> DemoHomeRuntime::FindDevices(DeviceFeature feature)
{
    auto devices = ListDevices();
    devices.erase(
        std::remove_if(
            devices.begin(),
            devices.end(),
            [feature](const auto& device)
            {
                return device == nullptr || !device->GetSupportedFeatures().contains(feature);
            }),
        devices.end());
    return devices;
}

void DemoHomeRuntime::ConnectDevices(::Crimson::Cpp::String upstreamDeviceId, ::Crimson::Cpp::String downstreamDeviceId)
{
    (void)GetDevice(upstreamDeviceId);
    (void)GetDevice(downstreamDeviceId);

    auto& downstream = connections_[upstreamDeviceId];
    if (std::find(downstream.begin(), downstream.end(), downstreamDeviceId) == downstream.end())
    {
        downstream.push_back(std::move(downstreamDeviceId));
    }
}

::Crimson::Cpp::List<::Crimson::Cpp::String> DemoHomeRuntime::TraceChain(::Crimson::Cpp::String originDeviceId)
{
    (void)GetDevice(originDeviceId);

    ::Crimson::Cpp::List<::Crimson::Cpp::String> chain;
    std::set<::Crimson::Cpp::String> visited;
    TraceFrom(originDeviceId, chain, visited);
    return chain;
}

void DemoHomeRuntime::ApplyScene(::Crimson::Cpp::InterfaceHandle<ISceneCommand> command)
{
    if (command == nullptr)
    {
        throw std::invalid_argument("Scene command must not be null.");
    }

    SetActiveScene(command->GetSceneName());
    SetAwayMode(command->GetAwayModeEnabled());

    ::Crimson::Cpp::List<::Crimson::Cpp::String> affectedDeviceIds;
    for (const auto& deviceId : command->GetTargetDeviceIds())
    {
        for (const auto& tracedDeviceId : TraceChain(deviceId))
        {
            PushUnique(affectedDeviceIds, tracedDeviceId);
        }
    }

    for (const auto& deviceId : affectedDeviceIds)
    {
        auto device = GetDevice(deviceId);
        if (const auto light = std::dynamic_pointer_cast<ILight>(device))
        {
            light->SetBrightness(command->GetBrightnessPercent());
        }

        if (const auto speaker = std::dynamic_pointer_cast<ISpeaker>(device))
        {
            speaker->PlayAnnouncement(command->GetAnnouncement());
        }

        if (const auto thermostat = std::dynamic_pointer_cast<IThermostat>(device))
        {
            thermostat->SetTargetTemperature(command->GetAwayModeEnabled() ? 17.5 : 20.5);
        }
    }
}

void DemoHomeRuntime::TraceFrom(const ::Crimson::Cpp::String& originDeviceId, ::Crimson::Cpp::List<::Crimson::Cpp::String>& chain, std::set<::Crimson::Cpp::String>& visited)
{
    if (!visited.insert(originDeviceId).second)
    {
        return;
    }

    chain.push_back(originDeviceId);

    if (const auto iterator = connections_.find(originDeviceId); iterator != connections_.end())
    {
        auto downstream = iterator->second;
        std::sort(downstream.begin(), downstream.end());
        for (const auto& next : downstream)
        {
            TraceFrom(next, chain, visited);
        }
    }
}

}
