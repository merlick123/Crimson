#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/SceneCommand.g.hpp"

namespace SmartHome
{

class SceneCommand : public SceneCommandGenerated
{
public:
    SceneCommand() = default;
    ~SceneCommand() override = default;

    void SetSceneName(::Crimson::Cpp::String value) override;
    void SetTargetDeviceIds(::Crimson::Cpp::List<::Crimson::Cpp::String> value) override;
    void SetBrightnessPercent(std::int32_t value) override;
    void SetAnnouncement(::Crimson::Cpp::String value) override;
};
}
