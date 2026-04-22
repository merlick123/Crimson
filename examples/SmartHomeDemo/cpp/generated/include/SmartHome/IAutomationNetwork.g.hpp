#pragma once

#include "Crimson/Cpp/Support.g.hpp"

namespace SmartHome
{

class IAutomationNetwork
{
public:
    virtual ~IAutomationNetwork() = default;

    virtual void ConnectDevices(::Crimson::Cpp::String upstreamDeviceId, ::Crimson::Cpp::String downstreamDeviceId) = 0;

    virtual ::Crimson::Cpp::List<::Crimson::Cpp::String> TraceChain(::Crimson::Cpp::String originDeviceId) = 0;

};
}
