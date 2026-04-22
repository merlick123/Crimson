#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/LightDevice.g.hpp"

namespace SmartHome
{

class LightDevice : public LightDeviceGenerated
{
public:
    LightDevice() = default;
    ~LightDevice() override = default;
};
}
