#include "SmartHome/HueBulb.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String HueBulbGenerated::GetDeviceId() const
{
    return deviceId_;
}

void HueBulbGenerated::SetDeviceId(::Crimson::Cpp::String value)
{
    deviceId_ = value;
}

::Crimson::Cpp::String HueBulbGenerated::GetDisplayName() const
{
    return displayName_;
}

void HueBulbGenerated::SetDisplayName(::Crimson::Cpp::String value)
{
    displayName_ = value;
}

std::int32_t HueBulbGenerated::GetBrightnessPercent() const
{
    return brightnessPercent_;
}

void HueBulbGenerated::SetBrightnessPercent(std::int32_t value)
{
    brightnessPercent_ = value;
}

}
