#![allow(clippy::module_name_repetitions)]

pub trait NestThermostatContract: crate::generated::smart_home__thermostat::ThermostatContract
{
}

#[derive(Clone)]
pub struct NestThermostatGenerated
{
    device_id: crate::generated::crimson_support::String,
    display_name: crate::generated::crimson_support::String,
    target_temperature: f64,
}

impl Default for NestThermostatGenerated
{
    fn default() -> Self
    {
        Self
        {
            device_id: crate::generated::crimson_support::String::new(),
            display_name: crate::generated::crimson_support::String::new(),
            target_temperature: 21.0,
        }
    }
}

impl NestThermostatGenerated
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

    pub fn get_target_temperature(&self) -> f64
    {
        self.target_temperature.clone()
    }

    pub fn set_target_temperature(&mut self, value: f64)
    {
        self.target_temperature = value;
    }
}
