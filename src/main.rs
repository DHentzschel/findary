use std::fs;

fn print_paths() {
    let paths = fs::read_dir("./").unwrap();

    for path in paths {
        println!("Name: {}", path.unwrap().path().display())
    }
}

fn main() {
    print_paths();
    // println!("Hello, world!");
}
