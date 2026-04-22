#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/IEufyDoorbell.g.hpp"

namespace SmartHome
{

class EufyDoorbellGenerated : public IEufyDoorbell
{
public:
    EufyDoorbellGenerated() = default;
    ~EufyDoorbellGenerated() override = default;

    ::Crimson::Cpp::String GetDeviceId() const override;

    ::Crimson::Cpp::String GetDisplayName() const override;
    void SetDisplayName(::Crimson::Cpp::String value) override;

    bool GetMotionDetected() const override;

protected:
    void SetDeviceId(::Crimson::Cpp::String value);

    void SetMotionDetected(bool value);

private:
    ::Crimson::Cpp::String deviceId_ = ::Crimson::Cpp::String{};
    ::Crimson::Cpp::String displayName_ = ::Crimson::Cpp::String{};
    bool motionDetected_ = false;
};
}
