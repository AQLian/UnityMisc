use std::sync::Arc;
use std::sync::Mutex;

#[derive(Debug)]
struct ItemA {
    name: String,
    item_b: Option<Arc<ItemB>>, // ItemA holds an Arc<ItemB>
}

impl Drop for ItemA {
    fn drop(&mut self) {
        println!("Dropping ItemA: {}", self.name);
    }
}

#[derive(Debug)]
struct ItemB {
    name: String,
    item_a: Mutex<Option<Arc<ItemA>>>, // ItemB holds an Arc<ItemA>
}

impl Drop for ItemB {
    fn drop(&mut self) {
        println!("Dropping ItemB: {}", self.name);
    }
}

fn main() {
    let mut item_a = Arc::new(ItemA {
        name: "Item A".to_string(),
        item_b: None,
    });

    let item_b = Arc::new(ItemB {
        name: "Item B".to_string(),
        item_a: Mutex::new(None),
    });

    {
        let mut_item = Arc::get_mut(&mut item_a).unwrap();
        mut_item.item_b = Some(Arc::clone(&item_b));
    }    

    {
        let mut item_b_lock = item_b.item_a.lock().unwrap();
        *item_b_lock = Some(Arc::clone(&item_a));
    }
}