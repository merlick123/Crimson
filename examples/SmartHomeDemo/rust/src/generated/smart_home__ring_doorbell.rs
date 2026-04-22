#![allow(clippy::module_name_repetitions)]

pub trait RingDoorbellContract: crate::generated::smart_home__camera::CameraContract + crate::generated::smart_home__motion_sensor::MotionSensorContract + crate::generated::smart_home__doorbell::DoorbellContract
{
}

#[derive(Clone)]
pub struct RingDoorbellGenerated
{
    device_id: crate::generated::crimson_support::String,
    display_name: crate::generated::crimson_support::String,
    motion_detected: bool,
}

impl Default for RingDoorbellGenerated
{
    fn default() -> Self
    {
        Self
        {
            device_id: crate::generated::crimson_support::String::new(),
            display_name: crate::generated::crimson_support::String::new(),
            motion_detected: false,
        }
    }
}

impl RingDoorbellGenerated
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

    pub fn get_motion_detected(&self) -> bool
    {
        self.motion_detected.clone()
    }

    pub fn set_motion_detected(&mut self, value: bool)
    {
        self.motion_detected = value;
    }
}
