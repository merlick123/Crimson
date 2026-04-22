#include "SmartHome/LightDevice.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String LightDeviceGenerated::GetDisplayName() const
{
    return displayName_;
}

void LightDeviceGenerated::SetDisplayName(::Crimson::Cpp::String value)
{
    displayName_ = value;
}

std::int32_t LightDeviceGenerated::GetBrightnessPercent() const
{
    return brightnessPercent_;
}

void LightDeviceGenerated::SetBrightnessPercent(std::int32_t value)
{
    brightnessPercent_ = value;
}

}
