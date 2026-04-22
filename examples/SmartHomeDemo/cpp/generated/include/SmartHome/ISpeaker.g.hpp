#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IDevice.g.hpp"

namespace SmartHome
{

class ISpeaker : public virtual IDevice
{
public:
    virtual ~ISpeaker() = default;

    virtual void PlayAnnouncement(::Crimson::Cpp::String message) = 0;

};
}
