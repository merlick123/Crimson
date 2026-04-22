#include "SmartHome/EufyDoorbell.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String EufyDoorbellGenerated::GetDeviceId() const
{
    return deviceId_;
}

void EufyDoorbellGenerated::SetDeviceId(::Crimson::Cpp::String value)
{
    deviceId_ = value;
}

::Crimson::Cpp::String EufyDoorbellGenerated::GetDisplayName() const
{
    return displayName_;
}

void EufyDoorbellGenerated::SetDisplayName(::Crimson::Cpp::String value)
{
    displayName_ = value;
}

bool EufyDoorbellGenerated::GetMotionDetected() const
{
    return motionDetected_;
}

void EufyDoorbellGenerated::SetMotionDetected(bool value)
{
    motionDetected_ = value;
}

}
