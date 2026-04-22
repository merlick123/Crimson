#![allow(clippy::module_name_repetitions)]

#[derive(Clone, Debug, Default)]
pub struct LightDevice
{
    generated: crate::generated::smart_home__light_device::LightDeviceGenerated,
}

impl LightDevice
{
    pub fn new() -> Self
    {
        Self::default()
    }
}

impl crate::generated::smart_home__light_device::LightDeviceContract for LightDevice
{
    fn get_display_name(&self) -> crate::generated::crimson_support::String
    {
        self.generated.get_display_name()
    }

    fn set_display_name(&mut self, value: crate::generated::crimson_support::String)
    {
        self.generated.set_display_name(value);
    }

    fn get_brightness_percent(&self) -> i32
    {
        self.generated.get_brightness_percent()
    }

    fn set_brightness_percent(&mut self, value: i32)
    {
        self.generated.set_brightness_percent(value);
    }

}
