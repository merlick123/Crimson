#include "SmartHome/EufyDoorbell.hpp"

#include <utility>

namespace SmartHome
{

EufyDoorbell::EufyDoorbell(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName)
{
    SetDeviceId(std::move(deviceId));
    SetDisplayName(std::move(displayName));
}

::Crimson::Cpp::Set<DeviceFeature> EufyDoorbell::GetSupportedFeatures()
{
    return
    {
        DeviceFeature::Camera,
        DeviceFeature::Motion,
        DeviceFeature::Doorbell,
        DeviceFeature::Automation,
    };
}

::Crimson::Cpp::String EufyDoorbell::DescribeState()
{
    return "motion=" + ::Crimson::Cpp::String(GetMotionDetected() ? "detected" : "clear")
        + ", rings=" + std::to_string(ringCount_)
        + ", snapshot=\"" + lastSnapshot_ + "\"";
}

::Crimson::Cpp::String EufyDoorbell::CaptureSnapshot()
{
    SetMotionDetected(true);
    lastSnapshot_ = "Package courier visible at the porch.";
    return lastSnapshot_;
}

void EufyDoorbell::Ring()
{
    ++ringCount_;
    SetMotionDetected(true);
    lastSnapshot_ = "Visitor pressed the porch doorbell.";
}

}
