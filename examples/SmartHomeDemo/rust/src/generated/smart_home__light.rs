#![allow(clippy::module_name_repetitions)]

pub trait LightContract: crate::generated::smart_home__device::DeviceContract
{
    fn get_brightness_percent(&self) -> i32;
    fn set_brightness_percent(&mut self, value: i32);

    fn set_brightness(&mut self, value: i32);

    fn as_smart_home__hue_bulb(&self) -> Option<&dyn crate::generated::smart_home__hue_bulb::HueBulbContract>
    {
        None
    }

    fn as_smart_home__hue_bulb_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__hue_bulb::HueBulbContract>
    {
        None
    }

}
