#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/ILightDevice.g.hpp"

namespace SmartHome
{

class LightDeviceGenerated : public ILightDevice
{
public:
    LightDeviceGenerated() = default;
    ~LightDeviceGenerated() override = default;

    ::Crimson::Cpp::String GetDisplayName() const override;
    void SetDisplayName(::Crimson::Cpp::String value) override;

    std::int32_t GetBrightnessPercent() const override;
    void SetBrightnessPercent(std::int32_t value) override;

private:
    ::Crimson::Cpp::String displayName_ = ::Crimson::Cpp::String{};
    std::int32_t brightnessPercent_ = 35;
};
}
