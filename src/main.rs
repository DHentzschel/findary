extern crate algorithm;
// extern crate walkdir;

use structopt::StructOpt;

// use walkdir::WalkDir;
use opt::Opt;

use crate::file::File;
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
    File::init();
    let files = filesystem::scan_files_recursively(&opt.directory, opt.verbose);
    for mut file in files {
        // TODO implement
        let is_binary = file.is_binary_type(opt.verbose);
        if is_binary {
            println!("File is binary");
        }
        println!("File {}, is_binary {}, matching_glob {}", file.path, is_binary, file.matching_glob);
    }
}
