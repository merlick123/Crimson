#![allow(clippy::module_name_repetitions)]

pub trait HueBulbContract: crate::generated::smart_home__light::LightContract
{
}

#[derive(Clone)]
pub struct HueBulbGenerated
{
    device_id: crate::generated::crimson_support::String,
    display_name: crate::generated::crimson_support::String,
    brightness_percent: i32,
}

impl Default for HueBulbGenerated
{
    fn default() -> Self
    {
        Self
        {
            device_id: crate::generated::crimson_support::String::new(),
            display_name: crate::generated::crimson_support::String::new(),
            brightness_percent: 0,
        }
    }
}

impl HueBulbGenerated
{
    pub fn new() -> Self
    {
        Self::default()
    }

    pub fn get_device_id(&self) -> crate::generated::crimson_support::String
    {
        self.device_id.clone()
    }

    pub fn set_device_id(&mut self, value: crate::generated::crimson_support::String)
    {
        self.device_id = value;
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
