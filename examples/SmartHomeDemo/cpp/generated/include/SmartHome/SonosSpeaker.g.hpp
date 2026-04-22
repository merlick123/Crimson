#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/ISonosSpeaker.g.hpp"

namespace SmartHome
{

class SonosSpeakerGenerated : public ISonosSpeaker
{
public:
    SonosSpeakerGenerated() = default;
    ~SonosSpeakerGenerated() override = default;

    ::Crimson::Cpp::String GetDeviceId() const override;

    ::Crimson::Cpp::String GetDisplayName() const override;
    void SetDisplayName(::Crimson::Cpp::String value) override;

protected:
    void SetDeviceId(::Crimson::Cpp::String value);

private:
    ::Crimson::Cpp::String deviceId_ = ::Crimson::Cpp::String{};
    ::Crimson::Cpp::String displayName_ = ::Crimson::Cpp::String{};
};
}
