#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DeviceFeature.g.hpp"
#include "SmartHome/IAutomationNetwork.g.hpp"
#include "SmartHome/IDevice.g.hpp"
#include "SmartHome/IDeviceRegistry.g.hpp"
#include "SmartHome/ISceneCommand.g.hpp"

namespace SmartHome
{

class IDemoHomeRuntime : public virtual IDeviceRegistry, public virtual IAutomationNetwork
{
public:
    virtual ~IDemoHomeRuntime() = default;

    virtual ::Crimson::Cpp::String GetHomeName() const = 0;
    virtual void SetHomeName(::Crimson::Cpp::String value) = 0;

    virtual ::Crimson::Cpp::String GetActiveScene() const = 0;
    virtual void SetActiveScene(::Crimson::Cpp::String value) = 0;

    virtual bool GetAwayMode() const = 0;
    virtual void SetAwayMode(bool value) = 0;

    virtual void ApplyScene(::Crimson::Cpp::InterfaceHandle<ISceneCommand> command) = 0;

};
}
