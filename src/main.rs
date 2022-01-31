extern crate walkdir;

use argparse::{ArgumentParser, Print, Store, StoreTrue};
use walkdir::WalkDir;

mod file;

fn print_paths_recursively() {
    for dir_entry in WalkDir::new(".").into_iter().filter_map(|e| e.ok()) {
        if dir_entry.metadata().unwrap().is_file() {
            println!("{}", dir_entry.path().display());
        }
    }
}

fn main() {
    let mut verbose = false;
    let mut directory = "";
    let mut ap = ArgumentParser::new();
    ap.set_description("A tool to find and track binaries in git large file storage (LFS)");
    ap.refer(&mut verbose)
        .add_option(&["-v", "--verbose"], StoreTrue,
                    "Be verbose");
    ap.refer(&mut directory)
        .add_option(&["-d", "--directory"], Store,
                    "The directory to scan");

    ap.add_option(&["-V", "--version"],
                  Print(env!("CARGO_PKG_VERSION").to_string()), "Show version");
    ap.parse_args_or_exit();
    print_paths_recursively();
}
