#![allow(clippy::module_name_repetitions)]

pub trait SonosSpeakerContract: crate::generated::smart_home__speaker::SpeakerContract
{
}

#[derive(Clone)]
pub struct SonosSpeakerGenerated
{
    device_id: crate::generated::crimson_support::String,
    display_name: crate::generated::crimson_support::String,
}

impl Default for SonosSpeakerGenerated
{
    fn default() -> Self
    {
        Self
        {
            device_id: crate::generated::crimson_support::String::new(),
            display_name: crate::generated::crimson_support::String::new(),
        }
    }
}

impl SonosSpeakerGenerated
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
}
