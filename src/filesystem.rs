
use walkdir::WalkDir;

fn paths_recursively(path_glob: &String) {
    for dirEntry in WalkDir::new(path_glob).into_iter().filter_map(|e| e.ok()) {
        if dirEntry.metadata().unwrap().is_file() {
            println!("{}", dirEntry.path().display());
        }
    }
}
