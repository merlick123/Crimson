use std::collections::BTreeMap;
use std::string::String as StdString;
use std::sync::Arc;
use std::vec::Vec;

pub type String = StdString;
pub type Optional<T> = core::option::Option<T>;
pub type List<T> = Vec<T>;
pub type Map<K, V> = BTreeMap<K, V>;
pub type InterfaceHandle<T> = core::option::Option<Arc<T>>;

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct Set<T>(pub Vec<T>);

impl<T> Set<T>
{
    pub fn new() -> Self
    {
        Self(Vec::new())
    }
}
