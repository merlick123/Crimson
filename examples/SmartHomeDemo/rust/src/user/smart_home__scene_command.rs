#![allow(clippy::module_name_repetitions)]

use crate::generated::crimson_support::{List, String};

#[derive(Clone, Default)]
pub struct SceneCommand
{
    generated: crate::generated::smart_home__scene_command::SceneCommandGenerated,
}

impl SceneCommand
{
    pub fn new() -> Self
    {
        Self::default()
    }
}

impl crate::generated::smart_home__scene_command::SceneCommandContract for SceneCommand
{
    fn get_scene_name(&self) -> String
    {
        self.generated.get_scene_name()
    }

    fn set_scene_name(&mut self, value: String)
    {
        let trimmed = value.trim();
        self.generated.set_scene_name(if trimmed.is_empty() { "Unnamed Scene".into() } else { trimmed.to_string() });
    }

    fn get_target_device_ids(&self) -> List<String>
    {
        self.generated.get_target_device_ids()
    }

    fn set_target_device_ids(&mut self, value: List<String>)
    {
        let mut normalized = List::new();
        for item in value
        {
            let trimmed = item.trim();
            if !trimmed.is_empty() && !normalized.iter().any(|candidate| candidate == trimmed)
            {
                normalized.push(trimmed.to_string());
            }
        }

        self.generated.set_target_device_ids(normalized);
    }

    fn get_away_mode_enabled(&self) -> bool
    {
        self.generated.get_away_mode_enabled()
    }

    fn set_away_mode_enabled(&mut self, value: bool)
    {
        self.generated.set_away_mode_enabled(value);
    }

    fn get_brightness_percent(&self) -> i32
    {
        self.generated.get_brightness_percent()
    }

    fn set_brightness_percent(&mut self, value: i32)
    {
        self.generated.set_brightness_percent(value.clamp(0, 100));
    }

    fn get_announcement(&self) -> String
    {
        self.generated.get_announcement()
    }

    fn set_announcement(&mut self, value: String)
    {
        let trimmed = value.trim();
        self.generated.set_announcement(if trimmed.is_empty() { "Welcome home.".into() } else { trimmed.to_string() });
    }
}
