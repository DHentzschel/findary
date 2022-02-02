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
    const BOMS: Vec<Vec<u8>> = Vec::new();

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
        self.is_binary = File::contains_null_byte(&mut buffer);

        return false;
    }

    pub fn init() {
        File::BOMS.push([0xFB, 0xEE, 0x28].to_vec()); // Bocu1
        File::BOMS.push([0x84, 0x31, 0x95, 0x33].to_vec()); // GB18030
        File::BOMS.push([0x0E, 0xFE, 0xFF].to_vec()); // SCSU
        File::BOMS.push([0xF7, 0x64, 0x4C].to_vec()); // UTF-1
        File::BOMS.push([0xFE, 0xFF].to_vec()); // UTF-16BE
        File::BOMS.push([0xFF, 0xFE].to_vec()); // UTF-16LE
        File::BOMS.push([0x00, 0x00, 0xFE, 0xFF].to_vec()); // UTF-32LE
        File::BOMS.push([0xFF, 0xFE, 0x00, 0x00].to_vec()); // UTF-7
        File::BOMS.push([0x38, 0x39, 0x2B, 0x2F].to_vec()); // UTF-7
        File::BOMS.push([0xEF, 0xBB, 0xBF].to_vec()); // UTF-8
        File::BOMS.push([0xDD, 0x73, 0x66, 0x73].to_vec()); // UTF-EBCDIC
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

    fn matches_bom(bytes: &mut [u8], bom: &mut Vec<u8>) -> bool {
        assert!(bytes.len() >= bom.len());

        for (dst, src) in bytes.iter_mut().zip(bom) {
            if *dst != *src {
                return false;
            }
        }
        return true;
    }

    fn to_hex_string(bytes: Vec<u8>) -> String {
        let strs: Vec<String> = bytes.iter()
            .map(|b| format!("{:02X}", b))
            .collect();
        strs.join(" ")
    }

    fn is_encoded_text_file(bytes: &mut [u8], verbose: bool) -> bool {
        if verbose {
            println!("Checking for boms");
        }
        for mut bom in File::BOMS {
            if File::matches_bom(bytes, &mut bom) {
                if verbose {
                    println!("Matches bom {}", File::to_hex_string(bom));
                }
                return true;
            }
        }
        return false;
    }
}
