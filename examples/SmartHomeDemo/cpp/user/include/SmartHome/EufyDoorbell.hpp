#pragma once

#include <cstdint>

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/EufyDoorbell.g.hpp"

namespace SmartHome
{

class EufyDoorbell : public EufyDoorbellGenerated
{
public:
    EufyDoorbell(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName);
    ~EufyDoorbell() override = default;

    ::Crimson::Cpp::Set<DeviceFeature> GetSupportedFeatures() override;
    ::Crimson::Cpp::String DescribeState() override;
    ::Crimson::Cpp::String CaptureSnapshot() override;
    void Ring() override;

private:
    std::int32_t ringCount_ = 0;
    ::Crimson::Cpp::String lastSnapshot_ = "Porch is quiet.";
};
}
