#pragma once

#include <map>
#include <set>
#include <vector>

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/DemoHomeRuntime.g.hpp"

namespace SmartHome
{

class DemoHomeRuntime : public DemoHomeRuntimeGenerated
{
public:
    DemoHomeRuntime();
    ~DemoHomeRuntime() override = default;

    void RegisterDevice(::Crimson::Cpp::InterfaceHandle<IDevice> device) override;
    ::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> ListDevices() override;
    ::Crimson::Cpp::InterfaceHandle<IDevice> GetDevice(::Crimson::Cpp::String deviceId) override;
    ::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> FindDevices(DeviceFeature feature) override;
    void ConnectDevices(::Crimson::Cpp::String upstreamDeviceId, ::Crimson::Cpp::String downstreamDeviceId) override;
    ::Crimson::Cpp::List<::Crimson::Cpp::String> TraceChain(::Crimson::Cpp::String originDeviceId) override;
    void ApplyScene(::Crimson::Cpp::InterfaceHandle<ISceneCommand> command) override;

private:
    void TraceFrom(const ::Crimson::Cpp::String& originDeviceId, ::Crimson::Cpp::List<::Crimson::Cpp::String>& chain, std::set<::Crimson::Cpp::String>& visited);

    std::map<::Crimson::Cpp::String, ::Crimson::Cpp::InterfaceHandle<IDevice>, std::less<>> devices_;
    std::map<::Crimson::Cpp::String, ::Crimson::Cpp::List<::Crimson::Cpp::String>, std::less<>> connections_;
};
}
