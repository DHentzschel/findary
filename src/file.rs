use std::collections::HashMap;
use std::io::prelude::*;
use std::path::Path;

use glob::Pattern;

use crate::{bom, filetype};
use crate::bom::Bom;
use crate::filetype::FileType;

pub struct File {
    pub matching_glob: String,
    pub matching_bom: String,
    pub path: String,
    pub file_type: FileType,
}

impl File {
    // const BOMS: HashMap<String, Vec<u8>> = HashMap::new();
    const BOMS: Vec<bom::Bom> = Vec::new();

    pub fn read_to_string(path: &String) -> String {
        std::fs::read_to_string(path).unwrap()
    }

    pub fn exists(&self) -> bool {
        Path::new(&self.path).is_file()
    }

    pub fn new(full_path: String) -> File {
        File {
            matching_glob: "".to_string(),
            matching_bom: "".to_string(),
            path: full_path,
            file_type: FileType::None,
        }
    }

    pub fn is_binary_type(&mut self, verbose: bool) -> bool {
        self.matching_glob = File::get_matching_glob();
        if !self.exists() {
            if verbose {
                println!("{} - no such file or directory", self.path);
            }
            return false;
        }
        let mut file_stream = std::fs::File::open(self.path.to_string()).unwrap();
        let mut buffer = [0; 10];

        // read up to 10 bytes
        file_stream.read(&mut buffer).unwrap();

        if File::is_encoded_text_file(&mut buffer, verbose) {
            self.file_type = FileType::EncodedText;
            if verbose {
                println!("File {} is encoded text file", self.path);
            }
        }

        let mut buffer: [u8; 1024] = [0; 1024];
        file_stream.read(&mut buffer).unwrap();

        let mut contains_null_byte = false;
        contains_null_byte = File::contains_null_byte(&mut buffer);

        while !contains_null_byte {
            let mut buffer: [u8; 1024] = [0; 1024];
            file_stream.read(&mut buffer).unwrap();
            contains_null_byte = File::contains_null_byte(&mut buffer);

            if contains_null_byte {
                self.file_type = FileType::Binary;
                return true;
            }
        }

        self.file_type = FileType::Text;
        return false;
    }

    pub fn init() {
        File::BOMS.push(Bom {
            key: "BOCU-1".to_string(),
            value: [0xFB, 0xEE, 0x28].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "GB18030".to_string(),
            value: [0x84, 0x31, 0x95, 0x33].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "SCSU".to_string(),
            value: [0x0E, 0xFE, 0xFF].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-1".to_string(),
            value: [0xF7, 0x64, 0x4C].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-16BE".to_string(),
            value: [0xFE, 0xFF].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-16LE".to_string(),
            value: [0xFF, 0xFE].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-32LE".to_string(),
            value: [0x00, 0x00, 0xFE, 0xFF].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-7".to_string(),
            value: [0xFF, 0xFE, 0x00, 0x00].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-7".to_string(),
            value: [0x38, 0x39, 0x2B, 0x2F].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-8".to_string(),
            value: [0xEF, 0xBB, 0xBF].to_vec(),
        });
        File::BOMS.push(Bom {
            key: "UTF-EBCDIC".to_string(),
            value: [0xDD, 0x73, 0x66, 0x73].to_vec(),
        });
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

    fn matches_bom(bytes: Vec<u8>, bom: &mut Vec<u8>) -> bool {
        assert!(bytes.len() >= bom.len());

        for (dst, src) in bytes.iter().zip(bom) {
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

    fn is_encoded_text_file(bytes: &[u8; 10], verbose: bool) -> bool {
        if verbose {
            println!("Checking for boms");
        }
        for mut bom in File::BOMS {
            if File::matches_bom(bytes.to_vec(), &mut bom.value) {
                if verbose {
                    println!("Matches bom {}", bom.key);
                }
                return true;
            }
        }
        return false;
    }
}
