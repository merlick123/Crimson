#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class IThermostat : public virtual IDevice
{
public:
    virtual ~IThermostat() = default;

    virtual double GetTargetTemperature() const = 0;
    virtual void SetTargetTemperature(double value) = 0;

    virtual double ReadTemperature() = 0;

};
}
