#! /opt/homebrew/bin/python3

from textwrap import indent
import xml.etree.ElementTree as ET
import json
import csv
from pathlib import Path

horse_info = {}

def main():
    with open('/Volumes/Work/kv1/kv1-dump/setting/item/001/equip/general/horse.txt', 'r') as horse_csv:
        info = csv.DictReader(horse_csv, delimiter='\t')
        for row in info:
            res_id = row['ResMale']
            if len(res_id) == 0:
                res_id = row['ResFemale']

            if len(res_id) == 0:
                continue

            horse_info[res_id] = row
            horse_info[res_id]['ResMale'] = res_id
            horse_info[res_id]['ResFemale'] = res_id

    horse_list = [h for h in horse_info.values()]
    horse_list.sort(key = lambda x: int(x['ResMale']))

    filename = 'horse.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    item_id = 30000

    for horse_config in horse_list:
        horse = ET.SubElement(root.getroot(), 'ItemData')
        res_id = horse_config['ResMale'] if len(horse_config['ResMale']) > 0 else horse_config['ResFemale']
        attrib = {
            'ItemID': item_id + int(res_id),
            'Name': '<color=#ed8796>[Admin]</color> ' + horse_config['View'].removeprefix('\\image\\item\\equip\\horse\\').removesuffix('.spr'),
            'IconBundleDir': 'Icon/EquipIcon2.unity3d',
            'IconAtlasName': 'EquipIcon2',
            'MapSpriteBundleDir': 'MapSprite.unity3d',
            'MapSpriteAtlasName': 'MapSprite',
            'Kind': 'Thú cưỡi Admin',
            'QualityPrefix': 0,
            'Category': 1,
            'Genre': 1,
            'Stack': 1,
            'DetailType': 12,
            'ParticularType': 0,
            'Level': 5,
            'SuiteID': 0,
            'Icon': Path(horse_config['View'].replace('\\', '/').replace('.spr', '')).name,
            'View': Path(horse_config['Icon'].replace('\\', '/').replace('.spr', '')).name,
            'Intro': horse_config['Intro'],
            'Help': horse_config['Help'],
            'ObjID': 32,
            'BindType': 2,
            'Series': 0,
            'ReqLevel': 0,
            'CDType': 0,
            'Price': 1,
            'ItemValue': 7200000,
            'MakeCost': -1,
            'ResMale': res_id,
            'ResFemale': res_id,
            'FightPower': 0,
            'ScriptID': -1,
            'DeductOnUse': 'false',
            'BuffID': -1,
        }

        horse.attrib = { k: str(v) for k, v in attrib.items() }
        
        speed_prop = ET.SubElement(horse, 'RiderProp')
        speed_prop.attrib = {
            'RidePropType': 'fastwalkrun_p',
            'RidePropPA1Min': '200',
            'RidePropPA1Max': '200',
            'RidePropPA2Min': '-1',
            'RidePropPA2Max': '-1',
            'RidePropPA3Min': '0',
            'RidePropPA3Max': '0',
            'Index': '1',
        }

    root.write('horse_modified.xml',
            encoding="UTF-8", xml_declaration=True)



if __name__ == '__main__':
    main()