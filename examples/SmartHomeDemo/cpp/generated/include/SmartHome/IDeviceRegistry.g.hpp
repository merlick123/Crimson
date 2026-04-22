#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class IDeviceRegistry
{
public:
    virtual ~IDeviceRegistry() = default;

    virtual void RegisterDevice(::Crimson::Cpp::InterfaceHandle<IDevice> device) = 0;

    virtual ::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> ListDevices() = 0;

    virtual ::Crimson::Cpp::InterfaceHandle<IDevice> GetDevice(::Crimson::Cpp::String deviceId) = 0;

    virtual ::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> FindDevices(DeviceFeature feature) = 0;

};
}
