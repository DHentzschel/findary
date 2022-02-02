extern crate algorithm;
// extern crate walkdir;

use structopt::StructOpt;

// use walkdir::WalkDir;
use opt::Opt;

use crate::bom::Boms;
use crate::file::File;
use crate::filetype::FileType;
use crate::filetype::FileType::Binary;

mod opt;
mod file;
mod filesystem;
mod filetype;
mod bom;

// fn print_paths_recursively() {
//     for dir_entry in WalkDir::new(".").into_iter().filter_map(|e| e.ok()) {
//         if dir_entry.metadata().unwrap().is_file() {
//             println!("{}", dir_entry.path().display());
//         }
//     }
// }

fn main() {
    let opt = Opt::from_args();
    // println!("Processing directory {}", opt.directory);
    println!("Verbose output {}", if opt.verbose { "on" } else { "off" });

    start(&opt);
}

fn start(opt: &Opt) {
    let mut boms = Boms::new();

    File::init_boms(&mut boms);
    let files = filesystem::scan_files_recursively(&opt.directory, opt.verbose);

    for mut file in files {
        // TODO implement
        let mut file_type: FileType = file.get_file_type(&mut boms, opt.verbose);
        file.file_type = file_type;
        let file_type: String;

        match file.file_type {
            FileType::None => file_type = "none".to_string(),
            FileType::Text => file_type = "text".to_string(),
            FileType::EncodedText => file_type = "encoded text".to_string(),
            FileType::Binary => file_type = "binary".to_string(),
        }

        println!("File is a {} file", file_type);
        println!("File {}, file_type {}, matching_glob {}", file.path, file_type, file.matching_glob);
    }
}
