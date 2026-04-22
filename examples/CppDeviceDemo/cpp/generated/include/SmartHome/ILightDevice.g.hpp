#pragma once

#include "Crimson/Cpp/Support.g.hpp"

namespace SmartHome
{

class ILightDevice
{
public:
    virtual ~ILightDevice() = default;

    virtual ::Crimson::Cpp::String GetDisplayName() const = 0;
    virtual void SetDisplayName(::Crimson::Cpp::String value) = 0;

    virtual std::int32_t GetBrightnessPercent() const = 0;
    virtual void SetBrightnessPercent(std::int32_t value) = 0;

};
}
