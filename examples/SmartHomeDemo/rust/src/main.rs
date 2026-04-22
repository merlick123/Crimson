mod generated;
mod user;

use std::boxed::Box;

use crate::generated::crimson_support::{interface_handle, with_interface, with_interface_mut, InterfaceHandle};
use crate::generated::smart_home__automation_network::AutomationNetworkContract;
use crate::generated::smart_home__demo_home_runtime::DemoHomeRuntimeContract;
use crate::generated::smart_home__device::DeviceContract;
use crate::generated::smart_home__device_feature::DeviceFeature;
use crate::generated::smart_home__device_registry::DeviceRegistryContract;
use crate::generated::smart_home__scene_command::SceneCommandContract;
use crate::user::smart_home__demo_home_runtime::DemoHomeRuntime;
use crate::user::smart_home__scene_command::SceneCommand;

fn feature_name(feature: DeviceFeature) -> &'static str
{
    match feature
    {
        DeviceFeature::Camera => "Camera",
        DeviceFeature::Motion => "Motion",
        DeviceFeature::Doorbell => "Doorbell",
        DeviceFeature::Lighting => "Lighting",
        DeviceFeature::Climate => "Climate",
        DeviceFeature::Speaker => "Speaker",
        DeviceFeature::Automation => "Automation",
    }
}

fn device_display_name(device: &InterfaceHandle<dyn DeviceContract>) -> String
{
    with_interface(device, |contract| contract.get_display_name()).unwrap_or_default()
}

fn device_id(device: &InterfaceHandle<dyn DeviceContract>) -> String
{
    with_interface(device, |contract| contract.get_device_id()).unwrap_or_default()
}

fn supported_features(device: &InterfaceHandle<dyn DeviceContract>) -> String
{
    let mut features = with_interface_mut(device, |contract|
    {
        contract
            .get_supported_features()
            .iter()
            .map(|feature| feature_name(*feature).to_string())
            .collect::<Vec<_>>()
    })
    .unwrap_or_default();

    features.sort();
    features.join(", ")
}

fn run_scenario(runtime: &mut DemoHomeRuntime, command: InterfaceHandle<dyn SceneCommandContract>)
{
    println!("Home: {}", runtime.get_home_name());
    println!("Scene before apply: {}", runtime.get_active_scene());
    println!("Away mode before apply: {}", runtime.get_away_mode());
    println!();

    let devices = runtime.list_devices();
    println!("Registered devices:");
    for device in &devices
    {
        println!("- {} :: {} :: {}", device_id(device), device_display_name(device), supported_features(device));
    }

    println!();
    println!("Doorbell-capable devices discovered through the shared runtime contract:");
    for device in runtime.find_devices(DeviceFeature::Doorbell)
    {
        let display_name = device_display_name(&device);
        let rang = with_interface_mut(&device, |contract|
        {
            if let Some(doorbell) = contract.as_smart_home__doorbell_mut()
            {
                doorbell.ring();
                true
            }
            else
            {
                false
            }
        })
        .unwrap_or(false);

        if rang
        {
            println!("- {} rang without the caller knowing the concrete vendor type.", display_name);
        }
    }

    println!();
    runtime.connect_devices("porch.eufy".into(), "hall.hue".into());
    runtime.connect_devices("hall.hue".into(), "living.sonos".into());
    runtime.connect_devices("garage.ring".into(), "living.sonos".into());
    println!("Automation chain from porch.eufy:");
    println!("  {}", runtime.trace_chain("porch.eufy".into()).join(" -> "));
    println!();

    println!("Capability queries:");
    for device in &devices
    {
        let display_name = device_display_name(device);

        if let Some(snapshot) = with_interface_mut(device, |contract|
        {
            contract
                .as_smart_home__camera_mut()
                .map(|camera| camera.capture_snapshot())
        })
        .flatten()
        {
            println!("- camera snapshot from {}: {}", display_name, snapshot);
        }

        if let Some(motion_state) = with_interface(device, |contract|
        {
            contract
                .as_smart_home__motion_sensor()
                .map(|sensor| sensor.get_motion_detected())
        })
        .flatten()
        {
            println!("- motion state for {}: {}", display_name, motion_state);
        }

        if let Some((target_temperature, current_temperature)) = with_interface_mut(device, |contract|
        {
            contract.as_smart_home__thermostat_mut().map(|thermostat|
            {
                thermostat.set_target_temperature(20.5);
                (thermostat.get_target_temperature(), thermostat.read_temperature())
            })
        })
        .flatten()
        {
            println!(
                "- thermostat {}: target={:.1}C current={:.1}C",
                display_name,
                target_temperature,
                current_temperature
            );
        }
    }

    println!();
    runtime.apply_scene(command);
    println!("Scene after apply: {}", runtime.get_active_scene());
    println!("Away mode after apply: {}", runtime.get_away_mode());
    println!();

    println!("Device state after scene:");
    for device in devices
    {
        let display_name = device_display_name(&device);
        if let Some(state) = with_interface_mut(&device, |contract| contract.describe_state())
        {
            println!("- {}: {}", display_name, state);
        }
    }
}

fn main() {
    let mut runtime = DemoHomeRuntime::new();
    runtime.set_home_name("Willow Lane".into());

    let mut command = SceneCommand::new();
    command.set_scene_name("  Evening Arrival  ".into());
    command.set_target_device_ids(vec!["porch.eufy".into(), "upstairs.nest".into()]);
    command.set_brightness_percent(42);
    command.set_announcement("Welcome home. Evening mode is active.".into());
    command.set_away_mode_enabled(false);

    let command_handle = interface_handle(Box::new(command) as Box<dyn SceneCommandContract>);
    run_scenario(&mut runtime, command_handle);
}
