#![allow(clippy::module_name_repetitions)]

pub trait DeviceContract
{
    fn get_device_id(&self) -> crate::generated::crimson_support::String;

    fn get_display_name(&self) -> crate::generated::crimson_support::String;
    fn set_display_name(&mut self, value: crate::generated::crimson_support::String);

    fn get_supported_features(&mut self) -> crate::generated::crimson_support::Set<crate::generated::smart_home__device_feature::DeviceFeature>;

    fn describe_state(&mut self) -> crate::generated::crimson_support::String;

    fn as_smart_home__camera(&self) -> Option<&dyn crate::generated::smart_home__camera::CameraContract>
    {
        None
    }

    fn as_smart_home__camera_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__camera::CameraContract>
    {
        None
    }

    fn as_smart_home__doorbell(&self) -> Option<&dyn crate::generated::smart_home__doorbell::DoorbellContract>
    {
        None
    }

    fn as_smart_home__doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__doorbell::DoorbellContract>
    {
        None
    }

    fn as_smart_home__eufy_doorbell(&self) -> Option<&dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract>
    {
        None
    }

    fn as_smart_home__eufy_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract>
    {
        None
    }

    fn as_smart_home__hue_bulb(&self) -> Option<&dyn crate::generated::smart_home__hue_bulb::HueBulbContract>
    {
        None
    }

    fn as_smart_home__hue_bulb_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__hue_bulb::HueBulbContract>
    {
        None
    }

    fn as_smart_home__light(&self) -> Option<&dyn crate::generated::smart_home__light::LightContract>
    {
        None
    }

    fn as_smart_home__light_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__light::LightContract>
    {
        None
    }

    fn as_smart_home__motion_sensor(&self) -> Option<&dyn crate::generated::smart_home__motion_sensor::MotionSensorContract>
    {
        None
    }

    fn as_smart_home__motion_sensor_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__motion_sensor::MotionSensorContract>
    {
        None
    }

    fn as_smart_home__nest_thermostat(&self) -> Option<&dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract>
    {
        None
    }

    fn as_smart_home__nest_thermostat_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__nest_thermostat::NestThermostatContract>
    {
        None
    }

    fn as_smart_home__ring_doorbell(&self) -> Option<&dyn crate::generated::smart_home__ring_doorbell::RingDoorbellContract>
    {
        None
    }

    fn as_smart_home__ring_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__ring_doorbell::RingDoorbellContract>
    {
        None
    }

    fn as_smart_home__sonos_speaker(&self) -> Option<&dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract>
    {
        None
    }

    fn as_smart_home__sonos_speaker_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract>
    {
        None
    }

    fn as_smart_home__speaker(&self) -> Option<&dyn crate::generated::smart_home__speaker::SpeakerContract>
    {
        None
    }

    fn as_smart_home__speaker_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__speaker::SpeakerContract>
    {
        None
    }

    fn as_smart_home__thermostat(&self) -> Option<&dyn crate::generated::smart_home__thermostat::ThermostatContract>
    {
        None
    }

    fn as_smart_home__thermostat_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__thermostat::ThermostatContract>
    {
        None
    }

}
