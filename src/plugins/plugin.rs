struct Plugin {
    path: String,
    glob: String
}

trait PluginTrait {
    fn ignore_path(glob: &String, path: &String) -> bool;
}
