use walkdir::WalkDir;

use crate::file::File;

pub fn scan_files_recursively(path_glob: &String, verbose: bool) -> Vec<File> {
    let mut result = Vec::new();
    if verbose {
        println!("Scanning directory {}", path_glob);
    }
    for dir_entry in WalkDir::new(path_glob).into_iter().filter_map(|e| e.ok()) {
        if dir_entry.metadata().unwrap().is_file() {
            let dir_path = dir_entry.path().display().to_string();
            let mut file = File::new(dir_path);
            if verbose {
                println!("{}", file.path);
            }
            result.push(file);
        }
    }
    return result;
}
