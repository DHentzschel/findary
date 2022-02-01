extern crate algorithm;
// extern crate walkdir;

use structopt::StructOpt;

// use walkdir::WalkDir;
use opt::Opt;
use crate::file::File;

mod opt;
mod file;
mod filesystem;

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
    let files = filesystem::scan_files_recursively(&opt.directory, opt.verbose);
    for mut file in files {
        // TODO implement
        file.is_binary = file.is_binary_type();
        println!("File {}, is_binary {}, matching_glob {}", file.path, file.is_binary, file.matching_glob);
    }
}
