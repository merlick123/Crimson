#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/SonosSpeaker.g.hpp"

namespace SmartHome
{

class SonosSpeaker : public SonosSpeakerGenerated
{
public:
    SonosSpeaker(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName);
    ~SonosSpeaker() override = default;

    ::Crimson::Cpp::Set<DeviceFeature> GetSupportedFeatures() override;
    ::Crimson::Cpp::String DescribeState() override;
    void PlayAnnouncement(::Crimson::Cpp::String message) override;

private:
    ::Crimson::Cpp::String lastAnnouncement_ = "Silence.";
};
}
