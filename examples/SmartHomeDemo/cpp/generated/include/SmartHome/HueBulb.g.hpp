#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/IHueBulb.g.hpp"

namespace SmartHome
{

class HueBulbGenerated : public IHueBulb
{
public:
    HueBulbGenerated() = default;
    ~HueBulbGenerated() override = default;

    ::Crimson::Cpp::String GetDeviceId() const override;

    ::Crimson::Cpp::String GetDisplayName() const override;
    void SetDisplayName(::Crimson::Cpp::String value) override;

    std::int32_t GetBrightnessPercent() const override;
    void SetBrightnessPercent(std::int32_t value) override;

protected:
    void SetDeviceId(::Crimson::Cpp::String value);

private:
    ::Crimson::Cpp::String deviceId_ = ::Crimson::Cpp::String{};
    ::Crimson::Cpp::String displayName_ = ::Crimson::Cpp::String{};
    std::int32_t brightnessPercent_ = 0;
};
}
