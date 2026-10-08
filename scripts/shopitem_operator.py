#!/usr/bin/env python

from pathlib import Path
import glob
import xml.etree.ElementTree as ET
import json

def load_items():
    data = {}
    for p in glob.glob("game-config/Config/Config/KT_Item/**/*.xml", recursive=True):
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib['ItemID'])
            data[itemid] = item.attrib['Name']

    for p in glob.glob("game-config/Config/Config/**/*Items.json", recursive=True):
        with open(p, 'r') as json_input:
            items = json.load(json_input)
            for item in items["RandomBoxes"]:
                data[item["ItemID"]] = item["Name"]

            for item in items["Normal"]:
                data[item["ItemID"]] = item["Name"]

    with open('game-config/Config/Config/KT_Item/Other/script_items.json', 'r') as json_input:
        items = json.load(json_input)

        for item in items:
            data[item["ItemID"]] = item["Name"]

    return data

def load_shop_tabs():
    emperor_items = []
    root = ET.parse("/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopTab.xml")
    for tab in root.findall("ShopTab"):
        tabid = int(tab.attrib['ShopID'])
        if tabid < 156 or tabid > 160:
            continue
        for item in tab.findall("ShopItem"):
            emperor_items.append(int(item.text))
           
    emperors_2 = []
    emperors_3 = [] 
    root = ET.parse("/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Shop/ShopItem.xml")
    for item in root.findall("ShopItem"):
        shopitemid = int(item.attrib['ID'])
        if shopitemid in emperor_items:
            repute_level = int(item.attrib['ReputeLevel'])
            
            if repute_level == 2:
                emperors_2.append(int(item.attrib['ItemID']))
            elif repute_level == 3:
                emperors_3.append(int(item.attrib['ItemID']))
                        
    print(emperors_2)
    print(emperors_3)
    
    return emperor_items

def hint():
    item_dict = load_items()
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse("game-config/Config/Config/KT_Shop/ShopItem.xml", parser=parser)
    for item in root.findall("ShopItem"):
        itemid = int(item.attrib['ItemID'])

        item.set('Hint', item_dict[itemid])
            
    
    filepath = f'game-config/Config/Config/KT_Shop/ShopItem.xml'
    root.write(filepath, encoding="UTF-8", xml_declaration=True)


def main():
    # clone_baihutang()
    hint()
    # print(load_shop_tabs())


if __name__ == '__main__':
    main()
