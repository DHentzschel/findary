use std::path::Path;

use glob::Pattern;

struct File {
    matching_glob: String,
    pub path: String,
}

impl File {
    fn is_match(&self, glob: &String) -> bool {
        return Pattern::new(glob).unwrap().matches(&self.path);
    }

    pub fn exists(&self) -> bool {
        return Path::new(&self.path).is_file();
    }
}
