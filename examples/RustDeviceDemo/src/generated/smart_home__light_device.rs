#![allow(clippy::module_name_repetitions)]

pub trait LightDeviceContract
{
    fn get_display_name(&self) -> crate::generated::crimson_support::String;
    fn set_display_name(&mut self, value: crate::generated::crimson_support::String);

    fn get_brightness_percent(&self) -> i32;
    fn set_brightness_percent(&mut self, value: i32);

}

#[derive(Clone)]
pub struct LightDeviceGenerated
{
    display_name: crate::generated::crimson_support::String,
    brightness_percent: i32,
}

impl Default for LightDeviceGenerated
{
    fn default() -> Self
    {
        Self
        {
            display_name: crate::generated::crimson_support::String::new(),
            brightness_percent: 35,
        }
    }
}

impl LightDeviceGenerated
{
    pub fn new() -> Self
    {
        Self::default()
    }

    pub fn get_display_name(&self) -> crate::generated::crimson_support::String
    {
        self.display_name.clone()
    }

    pub fn set_display_name(&mut self, value: crate::generated::crimson_support::String)
    {
        self.display_name = value;
    }

    pub fn get_brightness_percent(&self) -> i32
    {
        self.brightness_percent.clone()
    }

    pub fn set_brightness_percent(&mut self, value: i32)
    {
        self.brightness_percent = value;
    }
}
