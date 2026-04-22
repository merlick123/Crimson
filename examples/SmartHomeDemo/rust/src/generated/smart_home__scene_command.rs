#![allow(clippy::module_name_repetitions)]

pub trait SceneCommandContract
{
    fn get_scene_name(&self) -> crate::generated::crimson_support::String;
    fn set_scene_name(&mut self, value: crate::generated::crimson_support::String);

    fn get_target_device_ids(&self) -> crate::generated::crimson_support::List<crate::generated::crimson_support::String>;
    fn set_target_device_ids(&mut self, value: crate::generated::crimson_support::List<crate::generated::crimson_support::String>);

    fn get_away_mode_enabled(&self) -> bool;
    fn set_away_mode_enabled(&mut self, value: bool);

    fn get_brightness_percent(&self) -> i32;
    fn set_brightness_percent(&mut self, value: i32);

    fn get_announcement(&self) -> crate::generated::crimson_support::String;
    fn set_announcement(&mut self, value: crate::generated::crimson_support::String);

}

#[derive(Clone)]
pub struct SceneCommandGenerated
{
    scene_name: crate::generated::crimson_support::String,
    target_device_ids: crate::generated::crimson_support::List<crate::generated::crimson_support::String>,
    away_mode_enabled: bool,
    brightness_percent: i32,
    announcement: crate::generated::crimson_support::String,
}

impl Default for SceneCommandGenerated
{
    fn default() -> Self
    {
        Self
        {
            scene_name: crate::generated::crimson_support::String::new(),
            target_device_ids: crate::generated::crimson_support::List::new(),
            away_mode_enabled: false,
            brightness_percent: 35,
            announcement: crate::generated::crimson_support::String::from("Welcome home."),
        }
    }
}

impl SceneCommandGenerated
{
    pub fn new() -> Self
    {
        Self::default()
    }

    pub fn get_scene_name(&self) -> crate::generated::crimson_support::String
    {
        self.scene_name.clone()
    }

    pub fn set_scene_name(&mut self, value: crate::generated::crimson_support::String)
    {
        self.scene_name = value;
    }

    pub fn get_target_device_ids(&self) -> crate::generated::crimson_support::List<crate::generated::crimson_support::String>
    {
        self.target_device_ids.clone()
    }

    pub fn set_target_device_ids(&mut self, value: crate::generated::crimson_support::List<crate::generated::crimson_support::String>)
    {
        self.target_device_ids = value;
    }

    pub fn get_away_mode_enabled(&self) -> bool
    {
        self.away_mode_enabled.clone()
    }

    pub fn set_away_mode_enabled(&mut self, value: bool)
    {
        self.away_mode_enabled = value;
    }

    pub fn get_brightness_percent(&self) -> i32
    {
        self.brightness_percent.clone()
    }

    pub fn set_brightness_percent(&mut self, value: i32)
    {
        self.brightness_percent = value;
    }

    pub fn get_announcement(&self) -> crate::generated::crimson_support::String
    {
        self.announcement.clone()
    }

    pub fn set_announcement(&mut self, value: crate::generated::crimson_support::String)
    {
        self.announcement = value;
    }
}
