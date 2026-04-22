#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class IMotionSensor : public virtual IDevice
{
public:
    virtual ~IMotionSensor() = default;

    virtual bool GetMotionDetected() const = 0;

};
}
