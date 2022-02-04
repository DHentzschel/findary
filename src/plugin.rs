pub struct Plugin {
    pub path: String,
    pub globs: Vec<String>
}

pub trait Ignorable {
    fn read_globs(plugin: &mut Plugin, path: &String);
    fn ignore_path(glob: &String, path: &String) -> bool;
    fn new() -> Plugin;
}
