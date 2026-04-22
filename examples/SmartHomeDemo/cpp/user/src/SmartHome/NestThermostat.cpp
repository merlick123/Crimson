#include "SmartHome/NestThermostat.hpp"

#include <cmath>
#include <sstream>
#include <utility>

namespace SmartHome
{

NestThermostat::NestThermostat(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName)
{
    SetDeviceId(std::move(deviceId));
    SetDisplayName(std::move(displayName));
}

::Crimson::Cpp::Set<DeviceFeature> NestThermostat::GetSupportedFeatures()
{
    return
    {
        DeviceFeature::Climate,
        DeviceFeature::Automation,
    };
}

::Crimson::Cpp::String NestThermostat::DescribeState()
{
    std::ostringstream builder;
    builder.setf(std::ios::fixed);
    builder.precision(1);
    builder << "target=" << GetTargetTemperature() << "C current=" << ReadTemperature() << "C";
    return builder.str();
}

double NestThermostat::ReadTemperature()
{
    currentTemperature_ = std::round((currentTemperature_ + GetTargetTemperature()) / 2.0 * 10.0) / 10.0;
    return currentTemperature_;
}

}
