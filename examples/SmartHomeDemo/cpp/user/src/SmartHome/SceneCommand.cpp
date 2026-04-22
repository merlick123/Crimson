#include "SmartHome/SceneCommand.hpp"

#include <algorithm>
#include <cctype>
#include <utility>

namespace SmartHome
{

namespace
{
::Crimson::Cpp::String Trim(::Crimson::Cpp::String value)
{
    const auto notSpace = [](unsigned char character) { return !std::isspace(character); };
    value.erase(value.begin(), std::find_if(value.begin(), value.end(), notSpace));
    value.erase(std::find_if(value.rbegin(), value.rend(), notSpace).base(), value.end());
    return value;
}
}

void SceneCommand::SetSceneName(::Crimson::Cpp::String value)
{
    value = Trim(std::move(value));
    SceneCommandGenerated::SetSceneName(value.empty() ? "Unnamed Scene" : std::move(value));
}

void SceneCommand::SetTargetDeviceIds(::Crimson::Cpp::List<::Crimson::Cpp::String> value)
{
    ::Crimson::Cpp::List<::Crimson::Cpp::String> normalized;
    for (auto& item : value)
    {
        auto trimmed = Trim(std::move(item));
        if (!trimmed.empty() && std::find(normalized.begin(), normalized.end(), trimmed) == normalized.end())
        {
            normalized.push_back(std::move(trimmed));
        }
    }

    SceneCommandGenerated::SetTargetDeviceIds(std::move(normalized));
}

void SceneCommand::SetBrightnessPercent(std::int32_t value)
{
    SceneCommandGenerated::SetBrightnessPercent(std::clamp<std::int32_t>(value, 0, 100));
}

void SceneCommand::SetAnnouncement(::Crimson::Cpp::String value)
{
    value = Trim(std::move(value));
    SceneCommandGenerated::SetAnnouncement(value.empty() ? "Welcome home." : std::move(value));
}

}
