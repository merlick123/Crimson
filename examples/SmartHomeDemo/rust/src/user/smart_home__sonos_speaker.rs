#![allow(clippy::module_name_repetitions)]

use crate::generated::crimson_support::{Set, String};
use crate::generated::smart_home__device::DeviceContract;
use crate::generated::smart_home__device_feature::DeviceFeature;

pub struct SonosSpeaker
{
    generated: crate::generated::smart_home__sonos_speaker::SonosSpeakerGenerated,
    last_announcement: String,
}

impl Default for SonosSpeaker
{
    fn default() -> Self
    {
        Self
        {
            generated: crate::generated::smart_home__sonos_speaker::SonosSpeakerGenerated::default(),
            last_announcement: "Silence.".into(),
        }
    }
}

impl SonosSpeaker
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

impl DeviceContract for SonosSpeaker
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
        vec![DeviceFeature::Speaker, DeviceFeature::Automation].into()
    }

    fn describe_state(&mut self) -> String
    {
        format!("last announcement=\"{}\"", self.last_announcement)
    }

    fn as_smart_home__sonos_speaker(&self) -> Option<&dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract> { Some(self) }
    fn as_smart_home__sonos_speaker_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract> { Some(self) }
    fn as_smart_home__speaker(&self) -> Option<&dyn crate::generated::smart_home__speaker::SpeakerContract> { Some(self) }
    fn as_smart_home__speaker_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__speaker::SpeakerContract> { Some(self) }
}

impl crate::generated::smart_home__speaker::SpeakerContract for SonosSpeaker
{
    fn play_announcement(&mut self, message: String)
    {
        let trimmed = message.trim();
        self.last_announcement = if trimmed.is_empty() { "Silent update.".into() } else { trimmed.to_string() };
    }

    fn as_smart_home__sonos_speaker(&self) -> Option<&dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract> { Some(self) }
    fn as_smart_home__sonos_speaker_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract> { Some(self) }
}

impl crate::generated::smart_home__sonos_speaker::SonosSpeakerContract for SonosSpeaker
{
}
