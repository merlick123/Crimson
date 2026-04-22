#![allow(clippy::module_name_repetitions)]

pub trait CameraContract: crate::generated::smart_home__device::DeviceContract
{
    fn capture_snapshot(&mut self) -> crate::generated::crimson_support::String;

    fn as_smart_home__eufy_doorbell(&self) -> Option<&dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract>
    {
        None
    }

    fn as_smart_home__eufy_doorbell_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__eufy_doorbell::EufyDoorbellContract>
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

}
