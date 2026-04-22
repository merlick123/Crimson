#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IThermostat.g.hpp"

namespace SmartHome
{

class INestThermostat : public virtual IThermostat
{
public:
    virtual ~INestThermostat() = default;

};
}
