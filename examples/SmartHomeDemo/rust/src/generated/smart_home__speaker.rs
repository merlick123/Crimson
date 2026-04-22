#![allow(clippy::module_name_repetitions)]

pub trait SpeakerContract: crate::generated::smart_home__device::DeviceContract
{
    fn play_announcement(&mut self, message: crate::generated::crimson_support::String);

    fn as_smart_home__sonos_speaker(&self) -> Option<&dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract>
    {
        None
    }

    fn as_smart_home__sonos_speaker_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__sonos_speaker::SonosSpeakerContract>
    {
        None
    }

}
