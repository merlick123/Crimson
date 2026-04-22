#include "SmartHome/SceneCommand.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String SceneCommandGenerated::GetSceneName() const
{
    return sceneName_;
}

void SceneCommandGenerated::SetSceneName(::Crimson::Cpp::String value)
{
    sceneName_ = value;
}

::Crimson::Cpp::List<::Crimson::Cpp::String> SceneCommandGenerated::GetTargetDeviceIds() const
{
    return targetDeviceIds_;
}

void SceneCommandGenerated::SetTargetDeviceIds(::Crimson::Cpp::List<::Crimson::Cpp::String> value)
{
    targetDeviceIds_ = value;
}

bool SceneCommandGenerated::GetAwayModeEnabled() const
{
    return awayModeEnabled_;
}

void SceneCommandGenerated::SetAwayModeEnabled(bool value)
{
    awayModeEnabled_ = value;
}

std::int32_t SceneCommandGenerated::GetBrightnessPercent() const
{
    return brightnessPercent_;
}

void SceneCommandGenerated::SetBrightnessPercent(std::int32_t value)
{
    brightnessPercent_ = value;
}

::Crimson::Cpp::String SceneCommandGenerated::GetAnnouncement() const
{
    return announcement_;
}

void SceneCommandGenerated::SetAnnouncement(::Crimson::Cpp::String value)
{
    announcement_ = value;
}

}
