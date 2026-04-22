#![allow(clippy::module_name_repetitions)]

#[derive(Clone, Copy, Debug, PartialEq, Eq, Default)]
pub enum DeviceFeature
{
    #[default]
    Camera,
    Motion,
    Doorbell,
    Lighting,
    Climate,
    Speaker,
    Automation,
}
