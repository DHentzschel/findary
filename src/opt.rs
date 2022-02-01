use structopt::StructOpt;

#[derive(StructOpt)]
pub struct Opt {
    #[structopt(short = "d", long)]
    pub directory: String,
    
    #[structopt(short = "t", long)]
    pub track: bool,

    #[structopt(short = "v", long)]
    pub verbose: bool,
}
