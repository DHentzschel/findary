extern crate algorithm;

use structopt::StructOpt;

use opt::Opt;

use crate::bom::Boms;
use crate::file::File;
use crate::filetype::FileType;
use crate::filetype::FileType::Binary;
use crate::stats::Stats;

mod opt;
mod file;
mod filesystem;
mod filetype;
mod bom;
mod stats;
mod plugin;
mod gitignore;
mod gitattributes;

fn main() {
    let opt = Opt::from_args();
    if opt.verbose {
        println!("Verbose output on");
    }
    start(&opt);
}

fn start(opt: &Opt) {
    let mut boms = Boms::new();

    File::init_boms(&mut boms);
    let files = filesystem::scan_files_recursively(&opt.directory, opt.verbose);
    let mut stats = Stats {
        none_files: 0,
        text_files: 0,
        encoded_text_files: 0,
        binary_files: 0,
    };
    for mut file in files {
        if opt.verbose {
            println!("File {}", file.path);
        }
        // TODO implement
        let mut file_type: FileType = file.get_file_type(&mut boms, opt.verbose);
        file.file_type = file_type;
        let file_type: String = file.get_updated_file_type(&file, &mut stats);
        if opt.verbose {
            println!("File is a {} file", file_type);
        }
    }

    if !opt.verbose {
        return;
    }
    println!("-----------------");
    println!("Statistics:");
    println!("{} text files", stats.text_files);
    println!("{} encoded text files", stats.encoded_text_files);
    println!("{} binary files", stats.binary_files);
    println!("{} none files", stats.none_files);

    let total_files = stats.text_files + stats.encoded_text_files + stats.binary_files + stats.none_files;
    println!("{} total", total_files);


}
