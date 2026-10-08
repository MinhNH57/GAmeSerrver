#!/usr/bin/env python

from pathlib import Path
import glob
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
        'QualityPrefix': 0,
        'Category': 1,
        'Genre': 1,
        'Stack': 1,
        'DetailType': 15,
        'Level': 1,
        'SuiteID': 0,
        'Help': '',
        'ObjID': 32,
        'BindType': 1,
        'Series': 0,
        'ReqLevel': 0,
        'CDType': 0,
        'Price': 0,
        'MakeCost': -1,
        'ResMale': -1,
        'ResFemale': -1,
        'FightPower': 0,
        'ScriptID': -1,
        'DeductOnUse': False,
        'BuffID': -1,
    }

    zhens = []
    data = {
        'defaults': default_values,
        'data': zhens
    }

    root = ET.parse("zhen.xml", parser=parser)
    for item in root.findall("ItemData"):
        item_json = xmlNodeToJson(item)
        for k, v in default_values.items():
            if item_json.get(k) is not None and item_json[k] == v:
                item_json.pop(k)

        item_json['ListReqProp'] = []
        for reqProps in item.findall('ListReqProp'):
            prop_json = xmlNodeToJson(reqProps)
            item_json['ListReqProp'].append(prop_json)

        if len(item_json['ListReqProp']) == 0:
            item_json.pop('ListReqProp')

        zhens.append(item_json)
    
    with open('zhen.json', 'w') as output:
        json.dump(data, output, ensure_ascii=False)


def main():
    minify()


if __name__ == '__main__':
    main()
