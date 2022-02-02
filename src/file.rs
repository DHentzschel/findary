use std::io;
use std::io::prelude::*;
use std::path::Path;

use glob::Pattern;

pub struct File {
    pub matching_glob: String,
    pub path: String,
    pub is_binary: bool,
}

impl File {
    pub fn read_to_string(path: &String) -> String {
        std::fs::read_to_string(path).unwrap()
    }

    pub fn exists(&self) -> bool {
        Path::new(&self.path).is_file()
    }

    pub fn new(full_path: String) -> File {
        File {
            matching_glob: "".to_string(),
            path: full_path,
            is_binary: false,
        }
    }

    pub fn is_binary_type(&mut self) -> bool {
        self.matching_glob = File::get_matching_glob();
        if !self.exists() {
            return false;
        }
        let mut file_stream = std::fs::File::open(self.path.to_string()).unwrap();
        let mut buffer = [0; 10];

        // read up to 10 bytes
        file_stream.read(&mut buffer).unwrap();

        self.is_binary = File::contains_null_byte(&mut buffer);

        if self.is_binary {
            return true;
        }

        let mut buffer: [u8; 1024] = [0; 1024];
        file_stream.read(&mut buffer).unwrap();
        self.is_binary = File::contains_null_byte( &mut buffer);

        return false;
    }

    fn contains_null_byte(array: &mut [u8]) -> bool {
        for byte in array {
            if *byte as char == '\0' {
                return true;
            }
        }
        return false;
    }

    fn get_matching_glob() -> String {
        "<none>".to_string()
    }

    fn is_match(&self, glob: &String) -> bool {
        Pattern::new(glob).unwrap().matches(&self.path)
    }
}
