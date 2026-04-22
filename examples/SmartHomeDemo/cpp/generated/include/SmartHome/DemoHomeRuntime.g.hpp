#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/IDemoHomeRuntime.g.hpp"

namespace SmartHome
{

class DemoHomeRuntimeGenerated : public IDemoHomeRuntime
{
public:
    DemoHomeRuntimeGenerated() = default;
    ~DemoHomeRuntimeGenerated() override = default;

    ::Crimson::Cpp::String GetHomeName() const override;
    void SetHomeName(::Crimson::Cpp::String value) override;

    ::Crimson::Cpp::String GetActiveScene() const override;
    void SetActiveScene(::Crimson::Cpp::String value) override;

    bool GetAwayMode() const override;
    void SetAwayMode(bool value) override;

private:
    ::Crimson::Cpp::String homeName_ = "unknown";
    ::Crimson::Cpp::String activeScene_ = "idle";
    bool awayMode_ = false;
};
}
