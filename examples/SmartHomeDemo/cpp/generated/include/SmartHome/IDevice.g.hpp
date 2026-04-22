#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"

namespace SmartHome
{

class IDevice
{
public:
    virtual ~IDevice() = default;

    virtual ::Crimson::Cpp::String GetDeviceId() const = 0;

    virtual ::Crimson::Cpp::String GetDisplayName() const = 0;
    virtual void SetDisplayName(::Crimson::Cpp::String value) = 0;

    virtual ::Crimson::Cpp::Set<DeviceFeature> GetSupportedFeatures() = 0;

    virtual ::Crimson::Cpp::String DescribeState() = 0;

};
}
