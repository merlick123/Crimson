#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/NestThermostat.g.hpp"

namespace SmartHome
{

class NestThermostat : public NestThermostatGenerated
{
public:
    NestThermostat(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName);
    ~NestThermostat() override = default;

    ::Crimson::Cpp::Set<DeviceFeature> GetSupportedFeatures() override;
    ::Crimson::Cpp::String DescribeState() override;
    double ReadTemperature() override;

private:
    double currentTemperature_ = 19.3;
};
}
