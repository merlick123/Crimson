#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/INestThermostat.g.hpp"

namespace SmartHome
{

class NestThermostatGenerated : public INestThermostat
{
public:
    NestThermostatGenerated() = default;
    ~NestThermostatGenerated() override = default;

    ::Crimson::Cpp::String GetDeviceId() const override;

    ::Crimson::Cpp::String GetDisplayName() const override;
    void SetDisplayName(::Crimson::Cpp::String value) override;

    double GetTargetTemperature() const override;
    void SetTargetTemperature(double value) override;

protected:
    void SetDeviceId(::Crimson::Cpp::String value);

private:
    ::Crimson::Cpp::String deviceId_ = ::Crimson::Cpp::String{};
    ::Crimson::Cpp::String displayName_ = ::Crimson::Cpp::String{};
    double targetTemperature_ = 21;
};
}
