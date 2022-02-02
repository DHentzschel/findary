use std::collections::HashMap;
use std::io::prelude::*;
use std::path::Path;

use glob::Pattern;

use crate::{bom, filetype};
use crate::bom::{Bom, Boms};
use crate::filetype::FileType;
use crate::stats::Stats;

pub struct File {
    pub matching_glob: String,
    pub matching_bom: String,
    pub path: String,
    pub file_type: FileType,
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
            matching_bom: "".to_string(),
            path: full_path,
            file_type: FileType::None,
        }
    }

    pub fn get_file_type(&mut self, boms: &mut Boms, verbose: bool) -> FileType {
        self.matching_glob = File::get_matching_glob();
        if !self.exists() {
            if verbose {
                println!("{} - no such file or directory", self.path);
            }
            println!("none");
            return FileType::None;
        }
        let mut file_stream = std::fs::File::open(self.path.to_string()).unwrap();
        let mut buffer = [0; 10];

        // read up to 10 bytes
        file_stream.read(&mut buffer).unwrap();

        if File::is_encoded_text_file(&mut buffer, boms, verbose) {
            println!("encoded text");
            return FileType::EncodedText;
        }

        let mut buffer: [u8; 1024] = [0; 1024];
        file_stream.read(&mut buffer).unwrap();

        let mut contains_null_byte = File::contains_null_byte(&mut buffer);

        while !contains_null_byte {
            let mut buffer: [u8; 1024] = [0; 1024];
            file_stream.read(&mut buffer).unwrap();
            contains_null_byte = File::contains_null_byte(&mut buffer);

            if contains_null_byte {
                println!("binary");
                return FileType::Binary;
            }
        }

        println!("text");
        return FileType::Text;
    }

    pub fn get_updated_file_type(&self, file: &File, stats: &mut Stats) -> String {
        let result;
        match file.file_type {
            FileType::None => {
                stats.none_files += 1;
                result = "none".to_string()
            }
            FileType::Text => {
                stats.text_files += 1;
                result = "text".to_string()
            }
            FileType::EncodedText => {
                stats.encoded_text_files += 1;
                result = "encoded text".to_string()
            }
            FileType::Binary => {
                stats.binary_files += 1;
                result = "binary".to_string()
            }
        }
        return result;
    }

    pub fn init_boms(boms: &mut Boms) {
        boms.list.push(Bom {
            key: "BOCU-1".to_string(),
            value: [0xFB, 0xEE, 0x28].to_vec(),
        });
        boms.list.push(Bom {
            key: "GB18030".to_string(),
            value: [0x84, 0x31, 0x95, 0x33].to_vec(),
        });
        boms.list.push(Bom {
            key: "SCSU".to_string(),
            value: [0x0E, 0xFE, 0xFF].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-1".to_string(),
            value: [0xF7, 0x64, 0x4C].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-16BE".to_string(),
            value: [0xFE, 0xFF].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-16LE".to_string(),
            value: [0xFF, 0xFE].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-32LE".to_string(),
            value: [0x00, 0x00, 0xFE, 0xFF].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-7".to_string(),
            value: [0xFF, 0xFE, 0x00, 0x00].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-7".to_string(),
            value: [0x38, 0x39, 0x2B, 0x2F].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-8".to_string(),
            value: [0xEF, 0xBB, 0xBF].to_vec(),
        });
        boms.list.push(Bom {
            key: "UTF-EBCDIC".to_string(),
            value: [0xDD, 0x73, 0x66, 0x73].to_vec(),
        });
        println!("Searching for {} BOMs", boms.list.len());
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
                // println!("not matching bom");
                return false;
            }
        }
        // println!("Matches bom");
        return true;
    }

    // fn to_hex_string(bytes: Vec<u8>) -> String {
    //     let strs: Vec<String> = bytes.iter()
    //         .map(|b| format!("{:02X}", b))
    //         .collect();
    //     strs.join(" ")
    // }

    fn is_encoded_text_file(bytes: &mut [u8; 10], boms: &mut Boms, verbose: bool) -> bool {
        if verbose {
            println!("Checking for boms");
        }

        for mut bom in &mut boms.list {
            if File::matches_bom(bytes.to_vec(), &mut bom.value) {
                if verbose {}
                return true;
            }
        }
        return false;
    }
}
