#include "SmartHome/RingDoorbell.hpp"

#include <utility>

namespace SmartHome
{

RingDoorbell::RingDoorbell(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName)
{
    SetDeviceId(std::move(deviceId));
    SetDisplayName(std::move(displayName));
}

::Crimson::Cpp::Set<DeviceFeature> RingDoorbell::GetSupportedFeatures()
{
    return
    {
        DeviceFeature::Camera,
        DeviceFeature::Motion,
        DeviceFeature::Doorbell,
        DeviceFeature::Automation,
    };
}

::Crimson::Cpp::String RingDoorbell::DescribeState()
{
    return "motion=" + ::Crimson::Cpp::String(GetMotionDetected() ? "detected" : "clear")
        + ", rings=" + std::to_string(ringCount_)
        + ", snapshot=\"" + lastSnapshot_ + "\"";
}

::Crimson::Cpp::String RingDoorbell::CaptureSnapshot()
{
    SetMotionDetected(false);
    lastSnapshot_ = "Driveway is clear and the garage is shut.";
    return lastSnapshot_;
}

void RingDoorbell::Ring()
{
    ++ringCount_;
    SetMotionDetected(true);
    lastSnapshot_ = "Motion spotted near the garage side entrance.";
}

}
