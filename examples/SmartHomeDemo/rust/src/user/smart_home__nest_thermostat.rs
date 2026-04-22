#![allow(clippy::module_name_repetitions)]

use crate::generated::crimson_support::{Set, String};
use crate::generated::smart_home__device::DeviceContract;
use crate::generated::smart_home__device_feature::DeviceFeature;
use crate::generated::smart_home__thermostat::ThermostatContract;

pub struct NestThermostat
{
    generated: crate::generated::smart_home__nest_thermostat::NestThermostatGenerated,
    current_temperature: f64,
}

impl Default for NestThermostat
{
    fn default() -> Self
    {
        Self
        {
            generated: crate::generated::smart_home__nest_thermostat::NestThermostatGenerated::default(),
            current_temperature: 19.3,
        }
    }
}

impl NestThermostat
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

impl DeviceContract for NestThermostat
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
        vec![DeviceFeature::Climate, DeviceFeature::Automation].into()
    }

    fn describe_state(&mut self) -> String
    {
        format!(
            "target={:.1}C current={:.1}C",
            self.generated.get_target_temperature(),
            self.read_temperature()
        )
    }

    fn as_smart_home__nest_thermostat(&self) -> Option<&dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract> { Some(self) }
    fn as_smart_home__nest_thermostat_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract> { Some(self) }
    fn as_smart_home__thermostat(&self) -> Option<&dyn crate::generated::smart_home__thermostat::ThermostatContract> { Some(self) }
    fn as_smart_home__thermostat_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__thermostat::ThermostatContract> { Some(self) }
}

impl crate::generated::smart_home__thermostat::ThermostatContract for NestThermostat
{
    fn get_target_temperature(&self) -> f64
    {
        self.generated.get_target_temperature()
    }

    fn set_target_temperature(&mut self, value: f64)
    {
        self.generated.set_target_temperature(value);
    }

    fn read_temperature(&mut self) -> f64
    {
        self.current_temperature = ((self.current_temperature + self.generated.get_target_temperature()) / 2.0 * 10.0).round() / 10.0;
        self.current_temperature
    }

    fn as_smart_home__nest_thermostat(&self) -> Option<&dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract> { Some(self) }
    fn as_smart_home__nest_thermostat_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract> { Some(self) }
}

impl crate::generated::smart_home__nest_thermostat::NestThermostatContract for NestThermostat
{
}
