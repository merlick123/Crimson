#![allow(clippy::module_name_repetitions)]

pub trait ThermostatContract: crate::generated::smart_home__device::DeviceContract
{
    fn get_target_temperature(&self) -> f64;
    fn set_target_temperature(&mut self, value: f64);

    fn read_temperature(&mut self) -> f64;

    fn as_smart_home__nest_thermostat(&self) -> Option<&dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract>
    {
        None
    }

    fn as_smart_home__nest_thermostat_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract>
    {
        None
    }

}
