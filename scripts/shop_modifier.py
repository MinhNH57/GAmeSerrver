#! /opt/homebrew/bin/python3

from textwrap import indent
import xml.etree.ElementTree as ET
import json
from pathlib import Path
import copy

def item_clone_fixed_price(itemid: int, price: int):
    filename = "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopItem.xml"
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    item = root.find(f'ShopItem[@ItemID="{itemid}"]')
    
    old_id = item.get("ID")

    new_id = max([int(tab.attrib["ID"]) for tab in root.findall("ShopItem") if int(tab.attrib["ID"]) < 9000 ]) + 1
    print(f"cloning item {new_id} from {old_id} with new_price {price}%")
    item = copy.deepcopy(item)
    item.attrib["ID"] = str(new_id)
    if item.get("GoodsPrice") is not None:
        item.set("GoodsPrice", str(price))
    # ET.SubElement(root.getroot(), copy.deepcopy(shop))
    root.getroot().append(item)

    root.write("/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopItem.xml", encoding="utf-8", xml_declaration=True)
    return new_id


def item_clone_by_id(itemid: int, discount: int):
    filename = "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopItem.xml"
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    item = root.find(f'ShopItem[@ID="{itemid}"]')
    
    if item is None:
        return
                
    new_id = max([int(tab.attrib["ID"]) for tab in root.findall("ShopItem") if int(tab.attrib["ID"]) < 9000 ]) + 1
    print(f"cloning item {itemid} with discount {discount}%")
    item = copy.deepcopy(item)
    item.attrib["ID"] = str(new_id)
    if item.get("GoodsPrice") is not None:
        item.attrib["GoodsPrice"] = str(
            int(float(item.attrib["GoodsPrice"]) * discount / 100)
        )
    if item.get("TongFund") is not None:
        item.set("TongFund", str(int(float(item.attrib["TongFund"]) * discount / 100)))
    # ET.SubElement(root.getroot(), copy.deepcopy(shop))
    root.getroot().append(item)

    root.write("/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopItem.xml", encoding="utf-8", xml_declaration=True)
    return new_id


def shop_clone(shopid: int, discount: int):
    items = []
    filename = "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopTab.xml"
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    shop = root.find(f'ShopTab[@ShopID="{shopid}"]')
    for item in shop.findall("ShopItem"):
        items.append(item_clone_by_id(int(item.text), discount))

    new_id = max([int(tab.attrib["ShopID"]) for tab in root.findall("ShopTab") if int(tab.attrib["ShopID"]) < 9999]) + 1
    shop = copy.deepcopy(shop)
    [shop.remove(item) for item in shop.findall("ShopItem")]
    shop.attrib["ShopID"] = str(new_id)
    for item in items:
        new_element = ET.SubElement(shop, "ShopItem")
        new_element.text = str(item)

    # ET.SubElement(root.getroot(), copy.deepcopy(shop))
    root.getroot().append(shop)

    root.write("/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopTab.xml", encoding="utf-8", xml_declaration=True)


def item_clone():
    ids = [
        100,
        101,
        102,
        103,
        104,
        111,
        112,
        113,
        114,
        115,
        122,
        123,
        124,
        125,
        126,
        133,
        134,
        135,
        136,
        137,
        274,
        284,
        294,
        796,
        105,
        116,
        127,
        13090,
        13088,
        13092,
    ]
    data = {}
    for p in Path(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    ).rglob("*.xml"):
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib["ItemID"])
            data[itemid] = item

    current_id = 13126
    root = ET.Element("ArrayOfItemData")
    for item_id in ids:
        item_data = data[item_id]
        item_data.set('ItemID', str(current_id))
        current_price = int(item_data.get('Price'))
        item_data.set('Price', str(current_price * 3))
        current_kind = item_data.get('Kind')
        item_data.set('Kind', current_kind + ' (thuốc LSV 89)')
        root.append(item_data)
        current_id += 1
        
    doc = ET.ElementTree(root)
    doc.write("medicine_lsv_89.xml", encoding="utf-8", xml_declaration=True)

def main():
    # shop_clone(212, 50)
    # shop_clone(211, 50)
    # shop_clone(213, 50)
    # shop_clone(214, 50)
    # shop_clone(215, 50)
    # shop_clone(149, 200)
    # shop_clone(150, 200)
    # shop_clone(151, 200)
    # shop_clone(152, 200)
    # shop_clone(153, 200)

    # shop_clone(230, 100)
    # item_clone_fixed_price(2418, 12888)
    
    # shop_clone(1492, 100)
    # shop_clone(1486, 100)
    # shop_clone(1487, 100)
    shop_clone(1493, 100)
    
    # data = {}
    # for p in Path(
    #     "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    # ).rglob("mantle.xml"):
        
        


if __name__ == "__main__":
    main()
