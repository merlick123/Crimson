#![allow(clippy::module_name_repetitions)]

use crate::generated::crimson_support::{Set, String};
use crate::generated::smart_home__device::DeviceContract;
use crate::generated::smart_home__device_feature::DeviceFeature;

pub struct HueBulb
{
    generated: crate::generated::smart_home__hue_bulb::HueBulbGenerated,
}

impl Default for HueBulb
{
    fn default() -> Self
    {
        Self
        {
            generated: crate::generated::smart_home__hue_bulb::HueBulbGenerated::default(),
        }
    }
}

impl HueBulb
{
    pub fn new() -> Self
    {
        Self::default()
    }

    pub fn with_identity(device_id: String, display_name: String) -> Self
    {
        let mut device = Self::default();
        device.generated.set_device_id(device_id);
        device.generated.set_display_name(display_name);
        device
    }
}

impl DeviceContract for HueBulb
{
    fn get_device_id(&self) -> String
    {
        self.generated.get_device_id()
    }

    fn get_display_name(&self) -> String
    {
        self.generated.get_display_name()
    }

    fn set_display_name(&mut self, value: String)
    {
        self.generated.set_display_name(value);
    }

    fn get_supported_features(&mut self) -> Set<DeviceFeature>
    {
        vec![DeviceFeature::Lighting, DeviceFeature::Automation].into()
    }

    fn describe_state(&mut self) -> String
    {
        if self.generated.get_brightness_percent() == 0
        {
            "off".into()
        }
        else
        {
            format!("on at {}% brightness", self.generated.get_brightness_percent())
        }
    }

    fn as_smart_home__hue_bulb(&self) -> Option<&dyn crate::generated::smart_home__hue_bulb::HueBulbContract> { Some(self) }
    fn as_smart_home__hue_bulb_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__hue_bulb::HueBulbContract> { Some(self) }
    fn as_smart_home__light(&self) -> Option<&dyn crate::generated::smart_home__light::LightContract> { Some(self) }
    fn as_smart_home__light_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__light::LightContract> { Some(self) }
}

impl crate::generated::smart_home__light::LightContract for HueBulb
{
    fn get_brightness_percent(&self) -> i32
    {
        self.generated.get_brightness_percent()
    }

    fn set_brightness_percent(&mut self, value: i32)
    {
        self.generated.set_brightness_percent(value);
    }

    fn set_brightness(&mut self, value: i32)
    {
        self.generated.set_brightness_percent(value.clamp(0, 100));
    }

    fn as_smart_home__hue_bulb(&self) -> Option<&dyn crate::generated::smart_home__hue_bulb::HueBulbContract> { Some(self) }
    fn as_smart_home__hue_bulb_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__hue_bulb::HueBulbContract> { Some(self) }
}

impl crate::generated::smart_home__hue_bulb::HueBulbContract for HueBulb
{
}
