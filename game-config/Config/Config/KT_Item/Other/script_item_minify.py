#!/usr/bin/env python

from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import json


def xmlNodeToJson(node: ET.Element, listAttr: list = None, mapping: dict = None) -> dict:
    d = {}
    for attr in node.attrib:
        if listAttr is not None and attr not in listAttr:
            continue
        mAttr = attr
        if mapping is not None and attr in mapping:
            mAttr = mapping[attr]
        try:
            d[mAttr] = int(node.attrib[attr])
        except:
            v = node.attrib[attr]
            if v == 'true':
                d[mAttr] = True
            elif v == 'false':
                d[mAttr] = False
            else:
                d[mAttr] = v
    return d


def minify():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))

    default_values = {
        'IconBundleDir': 'Icon/EquipIcon2.unity3d',
        'IconAtlasName': 'EquipIcon2',
        'MapSpriteBundleDir': 'Resources/Item/MapSprite.unity3d',
        'MapSpriteAtlasName': 'MapSprite',
        'Kind': '',
        'QualityPrefix': 0,
        'Category': 0,
        'Genre': 18,
        'Stack': 1,
        'DetailType': 1,
        'Level': 1,
        'SuiteID': 0,
        'Help': '',
        'ObjID': 1,
        'BindType': 1,
        'Series': 0,
        'ReqLevel': 0,
        'CDType': 0,
        'Price': 1,
        'MakeCost': -1,
        'ResMale': 0,
        'ResFemale': 0,
        'FightPower': 0,
        'ScriptID': -1,
        'DeductOnUse': False,
        'BuffID': -1,
        'ParticularType': 1,
        'ItemValue': 0
    }

    zhens = []
    data = {
        'defaults': default_values,
        'data': zhens
    }

    root = ET.parse("scriptitem.xml", parser=parser)
    for item in root.findall("ItemData"):
        item_json = xmlNodeToJson(item)
        for k, v in default_values.items():
            if item_json.get(k) is not None and item_json[k] == v:
                item_json.pop(k)

        if item_json.get('Icon') == item_json.get('View'):
            item_json.pop('View')

        if item_json.get('Kind') == item_json.get('Name'):
            item_json.pop('Kind')

        if item_json.get('Kind') == item_json.get('Intro'):
            item_json.pop('Kind')

        item_json['ListReqProp'] = []
        for reqProps in item.findall('ListReqProp'):
            prop_json = xmlNodeToJson(reqProps)
            item_json['ListReqProp'].append(prop_json)

        if item_json.get('ParticularType') is not None:
            item_json.pop('ParticularType')

        if item_json.get('ObjID') is not None:
            item_json.pop('ObjID')

        if item_json.get('Price') == 0:
            item_json.pop('Price')

        if len(item_json['ListReqProp']) == 0:
            item_json.pop('ListReqProp')

        ext_params_json = []
        for ext_params in item.findall('ListExtPram'):
            ext_param = xmlNodeToJson(ext_params)
            ext_params_json.append(ext_param)

        ext_params_json.sort(key=lambda x: x['Index'])

        if len(ext_params_json) > 0:
            item_json['ListExtPram'] = [x['Pram'] for x in ext_params_json]

        zhens.append(item_json)
    
    with open('script_item.json', 'w') as output:
        json.dump(data, output, ensure_ascii=False)

    cmd = f'prettier --write --print-width=160 --tab-width=4 script_item.json'
    # print(os.getcwd())
    subprocess.run(cmd, shell=True)


def main():
    minify()


if __name__ == '__main__':
    main()
