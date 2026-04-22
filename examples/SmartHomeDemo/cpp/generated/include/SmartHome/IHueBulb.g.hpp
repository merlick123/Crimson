#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/ILight.g.hpp"

namespace SmartHome
{

class IHueBulb : public virtual ILight
{
public:
    virtual ~IHueBulb() = default;

};
}
