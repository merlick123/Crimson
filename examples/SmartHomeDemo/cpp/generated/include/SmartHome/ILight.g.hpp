#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class ILight : public virtual IDevice
{
public:
    virtual ~ILight() = default;

    virtual std::int32_t GetBrightnessPercent() const = 0;
    virtual void SetBrightnessPercent(std::int32_t value) = 0;

    virtual void SetBrightness(std::int32_t value) = 0;

};
}
