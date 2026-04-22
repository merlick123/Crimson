#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class ICamera : public virtual IDevice
{
public:
    virtual ~ICamera() = default;

    virtual ::Crimson::Cpp::String CaptureSnapshot() = 0;

};
}
