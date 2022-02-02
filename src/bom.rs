pub(crate) struct Bom {
    pub(crate) key: String,
    pub(crate) value: Vec<u8>,
}

pub struct Boms {
    pub(crate) list: Vec<Bom>,
}

impl Boms {
    pub fn new() -> Boms {
        Boms {
            list: Vec::new()
        }
    }
}
