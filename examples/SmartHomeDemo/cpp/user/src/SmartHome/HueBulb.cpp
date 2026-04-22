#include "SmartHome/HueBulb.hpp"

#include <algorithm>
#include <utility>

namespace SmartHome
{

HueBulb::HueBulb(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName)
{
    SetDeviceId(std::move(deviceId));
    SetDisplayName(std::move(displayName));
}

::Crimson::Cpp::Set<DeviceFeature> HueBulb::GetSupportedFeatures()
{
    return
    {
        DeviceFeature::Lighting,
        DeviceFeature::Automation,
    };
}

::Crimson::Cpp::String HueBulb::DescribeState()
{
    return GetBrightnessPercent() == 0
        ? "off"
        : "on at " + std::to_string(GetBrightnessPercent()) + "% brightness";
}

void HueBulb::SetBrightness(std::int32_t value)
{
    SetBrightnessPercent(std::clamp<std::int32_t>(value, 0, 100));
}

}
