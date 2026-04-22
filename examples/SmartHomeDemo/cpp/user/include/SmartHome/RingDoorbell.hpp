#pragma once

#include <cstdint>

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/RingDoorbell.g.hpp"

namespace SmartHome
{

class RingDoorbell : public RingDoorbellGenerated
{
public:
    RingDoorbell(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName);
    ~RingDoorbell() override = default;

    ::Crimson::Cpp::Set<DeviceFeature> GetSupportedFeatures() override;
    ::Crimson::Cpp::String DescribeState() override;
    ::Crimson::Cpp::String CaptureSnapshot() override;
    void Ring() override;

private:
    std::int32_t ringCount_ = 0;
    ::Crimson::Cpp::String lastSnapshot_ = "Garage is calm.";
};
}
