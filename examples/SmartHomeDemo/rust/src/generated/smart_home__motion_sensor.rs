#![allow(clippy::module_name_repetitions)]

pub trait MotionSensorContract: crate::generated::smart_home__device::DeviceContract
{
    fn get_motion_detected(&self) -> bool;

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
