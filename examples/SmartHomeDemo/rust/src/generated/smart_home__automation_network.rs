#![allow(clippy::module_name_repetitions)]

pub trait AutomationNetworkContract
{
    fn connect_devices(&mut self, upstream_device_id: crate::generated::crimson_support::String, downstream_device_id: crate::generated::crimson_support::String);

    fn trace_chain(&mut self, origin_device_id: crate::generated::crimson_support::String) -> crate::generated::crimson_support::List<crate::generated::crimson_support::String>;

    fn as_smart_home__demo_home_runtime(&self) -> Option<&dyn crate::generated::smart_home__demo_home_runtime::DemoHomeRuntimeContract>
    {
        None
    }

    fn as_smart_home__demo_home_runtime_mut(&mut self) -> Option<&mut dyn crate::generated::smart_home__demo_home_runtime::DemoHomeRuntimeContract>
    {
        None
    }

}
