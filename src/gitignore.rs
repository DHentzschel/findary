use std::fs;

use crate::filesystem;
use crate::plugin::{Ignorable, Plugin};

struct Gitignore {}

impl Ignorable for Gitignore {
    fn read_globs(plugin: &mut Plugin, path: &String) {
        let result = Vec::new();
        if filesystem::exists(path) {
            let content = fs::read_to_string(path).unwrap().to_string();
            let content_split = content.split('\n');
            for line in content_split {
                println!("{}", line);
            }
        }
        plugin.globs = result;
    }

    fn ignore_path(glob: &String, path: &String) -> bool {
        return false;
    }

    fn new() -> Plugin {
        Plugin {
            path: ".gitignore".to_string(),
            globs: Vec::new(),
        }
    }
}
