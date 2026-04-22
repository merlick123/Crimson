#include "SmartHome/NestThermostat.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String NestThermostatGenerated::GetDeviceId() const
{
    return deviceId_;
}

void NestThermostatGenerated::SetDeviceId(::Crimson::Cpp::String value)
{
    deviceId_ = value;
}

::Crimson::Cpp::String NestThermostatGenerated::GetDisplayName() const
{
    return displayName_;
}

void NestThermostatGenerated::SetDisplayName(::Crimson::Cpp::String value)
{
    displayName_ = value;
}

double NestThermostatGenerated::GetTargetTemperature() const
{
    return targetTemperature_;
}

void NestThermostatGenerated::SetTargetTemperature(double value)
{
    targetTemperature_ = value;
}

}
