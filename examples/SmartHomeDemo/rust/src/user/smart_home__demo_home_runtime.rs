#![allow(clippy::module_name_repetitions)]

use std::boxed::Box;
use std::collections::BTreeMap;

use crate::generated::crimson_support::{interface_handle, with_interface, with_interface_mut, InterfaceHandle, List, String};
use crate::generated::smart_home__automation_network::AutomationNetworkContract;
use crate::generated::smart_home__demo_home_runtime::{DemoHomeRuntimeContract, DemoHomeRuntimeGenerated};
use crate::generated::smart_home__device::DeviceContract;
use crate::generated::smart_home__device_feature::DeviceFeature;
use crate::generated::smart_home__device_registry::DeviceRegistryContract;
use crate::generated::smart_home__scene_command::SceneCommandContract;
use crate::user::smart_home__eufy_doorbell::EufyDoorbell;
use crate::user::smart_home__hue_bulb::HueBulb;
use crate::user::smart_home__nest_thermostat::NestThermostat;
use crate::user::smart_home__ring_doorbell::RingDoorbell;
use crate::user::smart_home__sonos_speaker::SonosSpeaker;

pub struct DemoHomeRuntime
{
    generated: DemoHomeRuntimeGenerated,
    devices: BTreeMap<String, InterfaceHandle<dyn DeviceContract>>,
    connections: BTreeMap<String, List<String>>,
}

impl Default for DemoHomeRuntime
{
    fn default() -> Self
    {
        let mut runtime = Self
        {
            generated: DemoHomeRuntimeGenerated::default(),
            devices: BTreeMap::new(),
            connections: BTreeMap::new(),
        };

        runtime.register_device(interface_handle(Box::new(EufyDoorbell::with_identity("porch.eufy".into(), "Front Porch Eufy Doorbell".into())) as Box<dyn DeviceContract>));
        runtime.register_device(interface_handle(Box::new(RingDoorbell::with_identity("garage.ring".into(), "Garage Ring Doorbell".into())) as Box<dyn DeviceContract>));
        runtime.register_device(interface_handle(Box::new(HueBulb::with_identity("hall.hue".into(), "Hallway Hue Bulb".into())) as Box<dyn DeviceContract>));
        runtime.register_device(interface_handle(Box::new(NestThermostat::with_identity("upstairs.nest".into(), "Upstairs Nest Thermostat".into())) as Box<dyn DeviceContract>));
        runtime.register_device(interface_handle(Box::new(SonosSpeaker::with_identity("living.sonos".into(), "Living Room Sonos".into())) as Box<dyn DeviceContract>));
        runtime
    }
}

impl DemoHomeRuntime
{
    pub fn new() -> Self
    {
        Self::default()
    }

    fn trace_from(&self, origin_device_id: &str, chain: &mut List<String>, visited: &mut List<String>)
    {
        if visited.iter().any(|device_id| device_id == origin_device_id)
        {
            return;
        }

        visited.push(origin_device_id.to_string());
        chain.push(origin_device_id.to_string());

        if let Some(downstream) = self.connections.get(origin_device_id)
        {
            let mut ordered = downstream.clone();
            ordered.sort();
            for next in ordered
            {
                self.trace_from(&next, chain, visited);
            }
        }
    }
}

impl crate::generated::smart_home__device_registry::DeviceRegistryContract for DemoHomeRuntime
{
    fn register_device(&mut self, device: InterfaceHandle<dyn DeviceContract>)
    {
        let device_id = with_interface(&device, |contract| contract.get_device_id())
            .unwrap_or_else(|| panic!("Registered devices must not be null."));

        if device_id.trim().is_empty()
        {
            panic!("Registered devices must have a stable device id.");
        }

        self.devices.insert(device_id, device);
    }

    fn list_devices(&mut self) -> List<InterfaceHandle<dyn DeviceContract>>
    {
        self.devices.values().cloned().collect()
    }

    fn get_device(&mut self, device_id: String) -> InterfaceHandle<dyn DeviceContract>
    {
        self.devices.get(device_id.as_str()).cloned().unwrap_or(None)
    }

    fn find_devices(&mut self, feature: DeviceFeature) -> List<InterfaceHandle<dyn DeviceContract>>
    {
        self.list_devices()
            .into_iter()
            .filter(|device| with_interface_mut(device, |contract| contract.get_supported_features().contains(&feature)).unwrap_or(false))
            .collect()
    }

    fn as_smart_home__demo_home_runtime(&self) -> Option<&dyn DemoHomeRuntimeContract>
    {
        Some(self)
    }

    fn as_smart_home__demo_home_runtime_mut(&mut self) -> Option<&mut dyn DemoHomeRuntimeContract>
    {
        Some(self)
    }
}

impl crate::generated::smart_home__automation_network::AutomationNetworkContract for DemoHomeRuntime
{
    fn connect_devices(&mut self, upstream_device_id: String, downstream_device_id: String)
    {
        if self.get_device(upstream_device_id.clone()).is_none()
        {
            panic!("Device '{}' is not registered.", upstream_device_id);
        }

        if self.get_device(downstream_device_id.clone()).is_none()
        {
            panic!("Device '{}' is not registered.", downstream_device_id);
        }

        let downstream = self.connections.entry(upstream_device_id).or_default();
        if !downstream.contains(&downstream_device_id)
        {
            downstream.push(downstream_device_id);
        }
    }

    fn trace_chain(&mut self, origin_device_id: String) -> List<String>
    {
        if self.get_device(origin_device_id.clone()).is_none()
        {
            panic!("Device '{}' is not registered.", origin_device_id);
        }

        let mut chain = List::new();
        let mut visited = List::new();
        self.trace_from(&origin_device_id, &mut chain, &mut visited);
        chain
    }

    fn as_smart_home__demo_home_runtime(&self) -> Option<&dyn DemoHomeRuntimeContract>
    {
        Some(self)
    }

    fn as_smart_home__demo_home_runtime_mut(&mut self) -> Option<&mut dyn DemoHomeRuntimeContract>
    {
        Some(self)
    }
}

impl DemoHomeRuntimeContract for DemoHomeRuntime
{
    fn get_home_name(&self) -> String
    {
        self.generated.get_home_name()
    }

    fn set_home_name(&mut self, value: String)
    {
        self.generated.set_home_name(value);
    }

    fn get_active_scene(&self) -> String
    {
        self.generated.get_active_scene()
    }

    fn set_active_scene(&mut self, value: String)
    {
        self.generated.set_active_scene(value);
    }

    fn get_away_mode(&self) -> bool
    {
        self.generated.get_away_mode()
    }

    fn set_away_mode(&mut self, value: bool)
    {
        self.generated.set_away_mode(value);
    }

    fn apply_scene(&mut self, command: InterfaceHandle<dyn SceneCommandContract>)
    {
        let scene_name = with_interface(&command, |contract| contract.get_scene_name())
            .unwrap_or_else(|| panic!("Scene command must not be null."));
        let away_mode_enabled = with_interface(&command, |contract| contract.get_away_mode_enabled())
            .unwrap_or_else(|| panic!("Scene command must not be null."));
        let brightness_percent = with_interface(&command, |contract| contract.get_brightness_percent())
            .unwrap_or_else(|| panic!("Scene command must not be null."));
        let announcement = with_interface(&command, |contract| contract.get_announcement())
            .unwrap_or_else(|| panic!("Scene command must not be null."));
        let target_device_ids = with_interface(&command, |contract| contract.get_target_device_ids())
            .unwrap_or_else(|| panic!("Scene command must not be null."));

        self.set_active_scene(scene_name);
        self.set_away_mode(away_mode_enabled);

        let mut affected_device_ids = List::new();
        for device_id in target_device_ids
        {
            for traced_device_id in self.trace_chain(device_id)
            {
                if !affected_device_ids.contains(&traced_device_id)
                {
                    affected_device_ids.push(traced_device_id);
                }
            }
        }

        for device_id in affected_device_ids
        {
            let device = self.get_device(device_id.clone());
            if device.is_none()
            {
                panic!("Device '{}' is not registered.", device_id);
            }

            let _ = with_interface_mut(&device, |contract|
            {
                if let Some(light) = contract.as_smart_home__light_mut()
                {
                    light.set_brightness(brightness_percent);
                }

                if let Some(speaker) = contract.as_smart_home__speaker_mut()
                {
                    speaker.play_announcement(announcement.clone());
                }

                if let Some(thermostat) = contract.as_smart_home__thermostat_mut()
                {
                    thermostat.set_target_temperature(if away_mode_enabled { 17.5 } else { 20.5 });
                }
            });
        }
    }
}
