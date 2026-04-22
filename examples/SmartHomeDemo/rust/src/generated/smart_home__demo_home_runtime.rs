#![allow(clippy::module_name_repetitions)]

pub trait DemoHomeRuntimeContract: crate::generated::smart_home__device_registry::DeviceRegistryContract + crate::generated::smart_home__automation_network::AutomationNetworkContract
{
    fn get_home_name(&self) -> crate::generated::crimson_support::String;
    fn set_home_name(&mut self, value: crate::generated::crimson_support::String);

    fn get_active_scene(&self) -> crate::generated::crimson_support::String;
    fn set_active_scene(&mut self, value: crate::generated::crimson_support::String);

    fn get_away_mode(&self) -> bool;
    fn set_away_mode(&mut self, value: bool);

    fn apply_scene(&mut self, command: crate::generated::crimson_support::InterfaceHandle<dyn crate::generated::smart_home__scene_command::SceneCommandContract>);

}

#[derive(Clone)]
pub struct DemoHomeRuntimeGenerated
{
    home_name: crate::generated::crimson_support::String,
    active_scene: crate::generated::crimson_support::String,
    away_mode: bool,
}

impl Default for DemoHomeRuntimeGenerated
{
    fn default() -> Self
    {
        Self
        {
            home_name: crate::generated::crimson_support::String::from("unknown"),
            active_scene: crate::generated::crimson_support::String::from("idle"),
            away_mode: false,
        }
    }
}

impl DemoHomeRuntimeGenerated
{
    pub fn new() -> Self
    {
        Self::default()
    }

    pub fn get_home_name(&self) -> crate::generated::crimson_support::String
    {
        self.home_name.clone()
    }

    pub fn set_home_name(&mut self, value: crate::generated::crimson_support::String)
    {
        self.home_name = value;
    }

    pub fn get_active_scene(&self) -> crate::generated::crimson_support::String
    {
        self.active_scene.clone()
    }

    pub fn set_active_scene(&mut self, value: crate::generated::crimson_support::String)
    {
        self.active_scene = value;
    }

    pub fn get_away_mode(&self) -> bool
    {
        self.away_mode.clone()
    }

    pub fn set_away_mode(&mut self, value: bool)
    {
        self.away_mode = value;
    }
}
