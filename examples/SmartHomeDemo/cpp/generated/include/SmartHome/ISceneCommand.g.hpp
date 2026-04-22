#pragma once

#include "Crimson/Cpp/Support.g.hpp"

namespace SmartHome
{

class ISceneCommand
{
public:
    virtual ~ISceneCommand() = default;

    virtual ::Crimson::Cpp::String GetSceneName() const = 0;
    virtual void SetSceneName(::Crimson::Cpp::String value) = 0;

    virtual ::Crimson::Cpp::List<::Crimson::Cpp::String> GetTargetDeviceIds() const = 0;
    virtual void SetTargetDeviceIds(::Crimson::Cpp::List<::Crimson::Cpp::String> value) = 0;

    virtual bool GetAwayModeEnabled() const = 0;
    virtual void SetAwayModeEnabled(bool value) = 0;

    virtual std::int32_t GetBrightnessPercent() const = 0;
    virtual void SetBrightnessPercent(std::int32_t value) = 0;

    virtual ::Crimson::Cpp::String GetAnnouncement() const = 0;
    virtual void SetAnnouncement(::Crimson::Cpp::String value) = 0;

};
}
