#pragma once

#include "Crimson/Cpp/Support.g.hpp"
#include "SmartHome/ISceneCommand.g.hpp"

namespace SmartHome
{

class SceneCommandGenerated : public ISceneCommand
{
public:
    SceneCommandGenerated() = default;
    ~SceneCommandGenerated() override = default;

    ::Crimson::Cpp::String GetSceneName() const override;
    void SetSceneName(::Crimson::Cpp::String value) override;

    ::Crimson::Cpp::List<::Crimson::Cpp::String> GetTargetDeviceIds() const override;
    void SetTargetDeviceIds(::Crimson::Cpp::List<::Crimson::Cpp::String> value) override;

    bool GetAwayModeEnabled() const override;
    void SetAwayModeEnabled(bool value) override;

    std::int32_t GetBrightnessPercent() const override;
    void SetBrightnessPercent(std::int32_t value) override;

    ::Crimson::Cpp::String GetAnnouncement() const override;
    void SetAnnouncement(::Crimson::Cpp::String value) override;

private:
    ::Crimson::Cpp::String sceneName_ = ::Crimson::Cpp::String{};
    ::Crimson::Cpp::List<::Crimson::Cpp::String> targetDeviceIds_ = {};
    bool awayModeEnabled_ = false;
    std::int32_t brightnessPercent_ = 35;
    ::Crimson::Cpp::String announcement_ = "Welcome home.";
};
}
