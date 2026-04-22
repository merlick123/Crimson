#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/ISpeaker.g.hpp"

namespace SmartHome
{

class ISonosSpeaker : public virtual ISpeaker
{
public:
    virtual ~ISonosSpeaker() = default;

};
}
