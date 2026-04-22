#![allow(clippy::module_name_repetitions)]

use crate::generated::crimson_support::{Set, String};
use crate::generated::smart_home__device::DeviceContract;
use crate::generated::smart_home__device_feature::DeviceFeature;

pub struct EufyDoorbell
{
    generated: crate::generated::smart_home__eufy_doorbell::EufyDoorbellGenerated,
    ring_count: i32,
    last_snapshot: String,
}

impl Default for EufyDoorbell
{
    fn default() -> Self
    {
        Self
        {
            generated: crate::generated::smart_home__eufy_doorbell::EufyDoorbellGenerated::default(),
            ring_count: 0,
            last_snapshot: "Porch is quiet.".into(),
        }
    }
}

impl EufyDoorbell
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

impl DeviceContract for EufyDoorbell
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
        vec![
            DeviceFeature::Camera,
            DeviceFeature::Motion,
            DeviceFeature::Doorbell,
            DeviceFeature::Automation,
        ].into()
    }

    fn describe_state(&mut self) -> String
    {
        format!(
            "motion={}, rings={}, snapshot=\"{}\"",
            if self.generated.get_motion_detected() { "detected" } else { "clear" },
            self.ring_count,
            self.last_snapshot
        )
    }

    fn as_smart_home__camera(&self) -> Option<&dyn crate::generated::smart_home__camera::CameraContract> { Some(self) }
    fn as_smart_home__camera_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__camera::CameraContract> { Some(self) }
    fn as_smart_home__doorbell(&self) -> Option<&dyn crate::generated::smart_home__doorbell::DoorbellContract> { Some(self) }
    fn as_smart_home__doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__doorbell::DoorbellContract> { Some(self) }
    fn as_smart_home__eufy_doorbell(&self) -> Option<&dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
    fn as_smart_home__eufy_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
    fn as_smart_home__motion_sensor(&self) -> Option<&dyn crate::generated::smart_home__motion_sensor::MotionSensorContract> { Some(self) }
    fn as_smart_home__motion_sensor_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__motion_sensor::MotionSensorContract> { Some(self) }
}

impl crate::generated::smart_home__camera::CameraContract for EufyDoorbell
{
    fn capture_snapshot(&mut self) -> String
    {
        self.generated.set_motion_detected(true);
        self.last_snapshot = "Package courier visible at the porch.".into();
        self.last_snapshot.clone()
    }

    fn as_smart_home__eufy_doorbell(&self) -> Option<&dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
    fn as_smart_home__eufy_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
}

impl crate::generated::smart_home__motion_sensor::MotionSensorContract for EufyDoorbell
{
    fn get_motion_detected(&self) -> bool
    {
        self.generated.get_motion_detected()
    }

    fn as_smart_home__eufy_doorbell(&self) -> Option<&dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
    fn as_smart_home__eufy_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
}

impl crate::generated::smart_home__doorbell::DoorbellContract for EufyDoorbell
{
    fn ring(&mut self)
    {
        self.ring_count += 1;
        self.generated.set_motion_detected(true);
        self.last_snapshot = "Visitor pressed the porch doorbell.".into();
    }

    fn as_smart_home__eufy_doorbell(&self) -> Option<&dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
    fn as_smart_home__eufy_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract> { Some(self) }
}

impl crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract for EufyDoorbell
{
}
