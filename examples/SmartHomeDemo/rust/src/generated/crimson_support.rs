use core::cell::RefCell;
use std::boxed::Box;
use std::collections::BTreeMap;
use std::rc::Rc;
use std::string::String as StdString;
use std::vec::Vec;

pub type String = StdString;
pub type Optional<T> = core::option::Option<T>;
pub type List<T> = Vec<T>;
pub type Map<K, V> = BTreeMap<K, V>;
pub type InterfaceHandle<T> = core::option::Option<Rc<RefCell<Box<T>>>>;

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct Set<T>(pub Vec<T>);

impl<T> Set<T>
{
    pub fn new() -> Self
    {
        Self(Vec::new())
    }

    pub fn from_items(items: Vec<T>) -> Self
    {
        Self(items)
    }
}

impl<T> From<Vec<T>> for Set<T>
{
    fn from(value: Vec<T>) -> Self
    {
        Self(value)
    }
}

impl<T> Set<T>
{
    pub fn iter(&self) -> core::slice::Iter<'_, T>
    {
        self.0.iter()
    }
}

impl<T: PartialEq> Set<T>
{
    pub fn contains(&self, value: &T) -> bool
    {
        self.0.contains(value)
    }

    pub fn insert(&mut self, value: T)
    {
        if !self.contains(&value)
        {
            self.0.push(value);
        }
    }
}

pub fn interface_handle<T: ?Sized>(value: Box<T>) -> InterfaceHandle<T>
{
    Some(Rc::new(RefCell::new(value)))
}

pub fn with_interface<T: ?Sized, TResult>(handle: &InterfaceHandle<T>, f: impl FnOnce(&T) -> TResult) -> Option<TResult>
{
    handle.as_ref().map(|value|
    {
        let borrowed = value.borrow();
        f(&**borrowed)
    })
}

pub fn with_interface_mut<T: ?Sized, TResult>(handle: &InterfaceHandle<T>, f: impl FnOnce(&mut T) -> TResult) -> Option<TResult>
{
    handle.as_ref().map(|value|
    {
        let mut borrowed = value.borrow_mut();
        f(&mut **borrowed)
    })
}
