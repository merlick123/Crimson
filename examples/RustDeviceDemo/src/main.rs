mod generated;
mod user;

use crate::generated::smart_home__light_device::LightDeviceContract;
use crate::user::smart_home__light_device::LightDevice;

fn main() {
    let mut light = LightDevice::new();
    light.set_display_name("Porch Light".into());
    light.set_brightness_percent(42);
    println!("{}: {}%", light.get_display_name(), light.get_brightness_percent());
}
