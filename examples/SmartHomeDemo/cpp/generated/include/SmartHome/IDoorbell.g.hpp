#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class IDoorbell : public virtual IDevice
{
public:
    virtual ~IDoorbell() = default;

    virtual void Ring() = 0;

};
}
