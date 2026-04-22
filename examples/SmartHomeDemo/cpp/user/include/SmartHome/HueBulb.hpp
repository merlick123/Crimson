#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/HueBulb.g.hpp"

namespace SmartHome
{

class HueBulb : public HueBulbGenerated
{
public:
    HueBulb(::Crimson::Cpp::String deviceId, ::Crimson::Cpp::String displayName);
    ~HueBulb() override = default;

    ::Crimson::Cpp::Set<DeviceFeature> GetSupportedFeatures() override;
    ::Crimson::Cpp::String DescribeState() override;
    void SetBrightness(std::int32_t value) override;
};
}
