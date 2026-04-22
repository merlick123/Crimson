#include "SmartHome/SonosSpeaker.hpp"

#include <algorithm>
#include <cctype>
#include <utility>

namespace SmartHome
{

namespace
{
::Crimson::Cpp::String Trim(::Crimson::Cpp::String value)
{
    const auto notSpace = [](unsigned char character) { return !std::isspace(character); };
    value.erase(value.begin(), std::find_if(value.begin(), value.end(), notSpace));
    value.erase(std::find_if(value.rbegin(), value.rend(), notSpace).base(), value.end());
    return value;
}
}

SonosSpeaker::SonosSpeaker(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName)
{
    SetDeviceId(std::move(deviceId));
    SetDisplayName(std::move(displayName));
}

::Crimson::Cpp::Set<DeviceFeature> SonosSpeaker::GetSupportedFeatures()
{
    return
    {
        DeviceFeature::Speaker,
        DeviceFeature::Automation,
    };
}

::Crimson::Cpp::String SonosSpeaker::DescribeState()
{
    return "last announcement=\"" + lastAnnouncement_ + "\"";
}

void SonosSpeaker::PlayAnnouncement(::Crimson::Cpp::String message)
{
    message = Trim(std::move(message));
    lastAnnouncement_ = message.empty() ? "Silent update." : std::move(message);
}

}
