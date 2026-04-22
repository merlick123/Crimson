#![allow(clippy::module_name_repetitions)]

pub trait DeviceRegistryContract
{
    fn register_device(&mut self, device: crate::generated::crimson_support::InterfaceHandle<dyn crate::generated::smart_home__device::DeviceContract>);

    fn list_devices(&mut self) -> crate::generated::crimson_support::List<crate::generated::crimson_support::InterfaceHandle<dyn crate::generated::smart_home__device::DeviceContract>>;

    fn get_device(&mut self, device_id: crate::generated::crimson_support::String) -> crate::generated::crimson_support::InterfaceHandle<dyn crate::generated::smart_home__device::DeviceContract>;

    fn find_devices(&mut self, feature: crate::generated::smart_home__device_feature::DeviceFeature) -> crate::generated::crimson_support::List<crate::generated::crimson_support::InterfaceHandle<dyn crate::generated::smart_home__device::DeviceContract>>;

    fn as_smart_home__demo_home_runtime(&self) -> Option<&dyn crate::generated::smart_home__demo_home_runtime::DemoHomeRuntimeContract>
    {
        None
    }

    fn as_smart_home__demo_home_runtime_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__demo_home_runtime::DemoHomeRuntimeContract>
    {
        None
    }

}
