#!/usr/bin/env python

from pathlib import Path
import glob
import xml.etree.ElementTree as ET
import shutil, os
import json

def add_default_expiry_time():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))

    root = ET.parse("KT_ConfigRandomBox.xml", parser=parser)
    for item in root.findall("Boxs/Items"):
        itemid = int(item.attrib['ItemID'])
        time_exp = item.attrib.get('ExpiryDate')
        if time_exp is None:
            item.attrib['ExpiryDate'] = ''
    
    filepath = f'KT_ConfigRandomBox_mod.xml'
    root.write(filepath, encoding="UTF-8", xml_declaration=True)

def minify_scriptitems():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))

    default_values = [
        ('QualityPrefix', 0),
        ('Category', 0),
        ('Genre', 18),
        ('Stack', 1),
        ('DetailType', 1),
        ('Level', 1),
        ('SuiteID', 0),
        ('Intro', ''),
        ('Help', ''),
        ('Kind', ''),
        ('Series', 0),
        ('ReqLevel', 0),
        ('CDType', 0),
        ('Price', 1),
        ('ItemValue', 0),
        ('MakeCost', -1),
        ('ResMale', 0),
        ('ResFemale', 0),
        ('FightPower', 0),
        ('ScriptID', -1),
        ('DeductOnUse', 'false'),
        ('BuffID', -1),
        ('BindType', 1)
    ]

    root = ET.parse("Other/scriptitem.xml", parser=parser)
    for item in root.findall("ItemData"):
        itemid = int(item.attrib['ItemID'])
        if item.attrib['IconBundleDir'] == 'Icon/EquipIcon1.unity3d' and item.attrib['IconAtlasName'] == 'EquipIcon1':
            item.attrib['IconAtlasName'] = '1'
            item.attrib.pop('IconBundleDir')
        elif item.attrib['IconBundleDir'] == 'Icon/EquipIcon2.unity3d' and item.attrib['IconAtlasName'] == 'EquipIcon2':
            item.attrib['IconAtlasName'] = '2'
            item.attrib.pop('IconBundleDir')

        if item.attrib['MapSpriteBundleDir'] == 'Resources/Item/MapSprite.unity3d' and item.attrib['MapSpriteAtlasName'] == 'MapSprite':
            item.attrib.pop('MapSpriteBundleDir')
            item.attrib.pop('MapSpriteAtlasName')

        for (k, v) in default_values:
            if item.attrib.get(k) is not None and item.attrib[k] == str(v):
                item.attrib.pop(k)

        item.attrib.pop('ObjID')
        unlock_interval = item.find('UnLockInterval')
        if unlock_interval is not None:
            item.remove(unlock_interval)
            
    
    filepath = f'scriptitems_mod.xml'
    root.write(filepath, encoding="UTF-8", xml_declaration=True)


def get_emperor_items():
    shop = ET.parse('../KT_Shop/ShopTab.xml')
    ids = []
    for tab in shop.findall('ShopTab'):
        shopid = int(tab.get('ShopID'))
        if shopid >= 156 and shopid <= 160:
            for item in tab.findall('ShopItem'):
                ids.append(int(item.text))

    items = []
    shopitem = ET.parse('../KT_Shop/ShopItem.xml')
    for item in shopitem.findall('ShopItem'):
        shopitem_id = int(item.get('ID'))
        shopitem_itemid = int(item.get('ItemID'))
        if shopitem_id in ids:
            items.append(shopitem_itemid)


    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    recipes = ET.parse('Refine.xml', parser=parser)
    
    for item in items:
        recipe = ET.SubElement(recipes.getroot(), 'Refine')
        recipe.set('RefineId', '524')
        recipe.set('SourceItem', str(item))
        recipe.set('ProduceItem', str(item))
        recipe.set('Fee', '10000')

    filepath = f'Refine_mod.xml'
    recipes.write(filepath, encoding="UTF-8", xml_declaration=True)

    pass


def get_team_battle_suit_items():
    value_set = set()
    green_items = ET.parse('SetItem/greenequip.xml')
    for item in green_items.findall('ItemData'):
        suitid = int(item.get('SuiteID'))
        value = int(item.get('ItemValue'))
        if suitid == 3 and value == 140000000:
            value_set.add(item.get('ItemID'))

    print(', '.join(value_set))


def load_items():
    not_found_icons = set()
    for p in glob.glob("**/*.xml", recursive=True):
        root = ET.parse(p)
        for itemData in root.findall("ItemData"):
            icon = itemData.get('Icon')
            if icon is None:
                # print(itemData.get('Name'))
                continue
            if not Path('/Volumes/Work/kv1/kv1-resources/UI/Icon/EquipIcon2').joinpath(f'{icon}.png').is_file():
                not_found_icons.add(icon)

    for p in glob.glob("**/Items.json", recursive=True):
        with open(p, 'r') as json_input:
            items = json.load(json_input)
            for item in items["RandomBoxes"]:
                icon = item['Icon']
                if icon is None:
                    # print(itemData.get('Name'))
                    continue
                if not Path('/Volumes/Work/kv1/kv1-resources/UI/Icon/EquipIcon2').joinpath(f'{icon}.png').is_file():
                    not_found_icons.add(icon)

            for item in items["Normal"]:
                icon = item['Icon']
                if icon is None:
                    # print(itemData.get('Name'))
                    continue
                if not Path('/Volumes/Work/kv1/kv1-resources/UI/Icon/EquipIcon2').joinpath(f'{icon}.png').is_file():
                    not_found_icons.add(icon)

    
    os.makedirs('/Volumes/Work/kv1_new_icons', exist_ok=True)
    for icon in not_found_icons:
        vng_icon_path = f'/Volumes/azStorage/work/kv1-vng/image/ui/**/{icon}_sample.png'
        # print(vng_icon_path)
        for p in glob.glob(vng_icon_path, recursive=True):
            print(p)
            shutil.copyfile(p, Path('/Volumes/Work/kv1_new_icons').joinpath(f'{icon}.png'))
        # if original.is_file():

    icon_list = [x for x in not_found_icons]
    icon_list.sort()
    print('\n'.join(icon_list))

def main():
    # minify_scriptitems()
    # get_emperor_items()
    # get_team_battle_suit_items()
    load_items()


if __name__ == '__main__':
    main()
