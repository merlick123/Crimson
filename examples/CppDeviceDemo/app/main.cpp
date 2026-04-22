#include <iostream>

#include "SmartHome/LightDevice.hpp"

int main()
{
    SmartHome::LightDevice light;
    light.SetDisplayName("Porch Light");
    light.SetBrightnessPercent(42);
    std::cout << light.GetDisplayName() << ": " << light.GetBrightnessPercent() << "%" << std::endl;
    return 0;
}
