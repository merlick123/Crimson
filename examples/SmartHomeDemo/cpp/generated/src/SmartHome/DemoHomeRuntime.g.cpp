#include "SmartHome/DemoHomeRuntime.g.hpp"

namespace SmartHome
{

::Crimson::Cpp::String DemoHomeRuntimeGenerated::GetHomeName() const
{
    return homeName_;
}

void DemoHomeRuntimeGenerated::SetHomeName(::Crimson::Cpp::String value)
{
    homeName_ = value;
}

::Crimson::Cpp::String DemoHomeRuntimeGenerated::GetActiveScene() const
{
    return activeScene_;
}

void DemoHomeRuntimeGenerated::SetActiveScene(::Crimson::Cpp::String value)
{
    activeScene_ = value;
}

bool DemoHomeRuntimeGenerated::GetAwayMode() const
{
    return awayMode_;
}

void DemoHomeRuntimeGenerated::SetAwayMode(bool value)
{
    awayMode_ = value;
}

}
