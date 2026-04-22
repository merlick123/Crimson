#include "SmartHome/RingDoorbell.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String RingDoorbellGenerated::GetDeviceId() const
{
    return deviceId_;
}

void RingDoorbellGenerated::SetDeviceId(::Crimson::Cpp::String value)
{
    deviceId_ = value;
}

::Crimson::Cpp::String RingDoorbellGenerated::GetDisplayName() const
{
    return displayName_;
}

void RingDoorbellGenerated::SetDisplayName(::Crimson::Cpp::String value)
{
    displayName_ = value;
}

bool RingDoorbellGenerated::GetMotionDetected() const
{
    return motionDetected_;
}

void RingDoorbellGenerated::SetMotionDetected(bool value)
{
    motionDetected_ = value;
}

}
