#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/ICamera.g.hpp"
#include "SmartHome/IDoorbell.g.hpp"
#include "SmartHome/IMotionSensor.g.hpp"

namespace SmartHome
{

class IRingDoorbell : public virtual ICamera, public virtual IMotionSensor, public virtual IDoorbell
{
public:
    virtual ~IRingDoorbell() = default;

};
}
