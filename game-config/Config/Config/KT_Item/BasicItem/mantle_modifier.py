#! /opt/homebrew/bin/python3

from textwrap import indent
import xml.etree.ElementTree as ET
import json
from pathlib import Path

def modify_props():
    filename = 'mantle.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    modification = {
        10: {
            # "lifemax_p": (100, 175),
            # "lifemax_v": (1000, 1750),
            # "manamax_p": (40, 90),
            # "manamax_v": (0, 900),
            # "damage_all_resist": (120, 200),
            "skilldamageptrim": (45, 50),
            "state_hurt_attackrate": (250, 300),
            "state_weak_attackrate": (250, 300),
            "state_slowall_attackrate": (250, 300),
            "state_burn_attackrate": (250, 300),
            "state_stun_attackrate": (250, 300),
            # "allseriesstateresistrate": (250, 300),
            # "allseriesstateresisttime": (250, 300),
        },
        9: {
            "lifemax_p": (80, 130),
            "lifemax_v": (800, 1300),
            "manamax_p": (40, 75),
            "manamax_v": (0, 750),
            "damage_all_resist": (120, 150),
            "skilldamageptrim": (30, 40),
            "state_hurt_attackrate": (150, 200),
            "state_weak_attackrate": (150, 200),
            "state_slowall_attackrate": (150, 200),
            "state_burn_attackrate": (150, 200),
            "state_stun_attackrate": (150, 200),
            "allseriesstateresistrate": (200, 250),
            "allseriesstateresisttime": (200, 250),
        },
        8: {
            "lifemax_p": (75, 100),
            "lifemax_v": (750, 1000),
            "manamax_p": (35, 55),
            "manamax_v": (0, 550),
            "damage_all_resist": (95, 120),
            "skilldamageptrim": (25, 30),
            "state_hurt_attackrate": (100, 125),
            "state_weak_attackrate": (100, 125),
            "state_slowall_attackrate": (100, 125),
            "state_burn_attackrate": (100, 125),
            "state_stun_attackrate": (100, 125),
            "allseriesstateresistrate": (120, 150),
            "allseriesstateresisttime": (120, 150),
        },
        7: {
            "lifemax_p": (75, 90),
            "lifemax_v": (750, 900),
            "manamax_p": (35, 45),
            "manamax_v": (0, 450),
            "damage_all_resist": (90, 120),
            "skilldamageptrim": (15, 20),
            "state_hurt_attackrate": (100, 125),
            "state_weak_attackrate": (100, 125),
            "state_slowall_attackrate": (100, 125),
            "state_burn_attackrate": (100, 125),
            "state_stun_attackrate": (100, 125),
            "allseriesstateresisttime": (110, 125),
        },
    }

    for level in range(10, 11):
        for mantle in root.findall(f'ItemData[@Level="{level}"]'):
            # prop_index = 0
            for prop in mantle.findall('ListBasicProp'):
                prop_type = prop.attrib['BasicPropType']
                prop_min = int(prop.attrib['BasicPropPA1Min'])
                prop_max = int(prop.attrib['BasicPropPA1Max'])
                if modification[level].get(prop_type) is not None:
                    (prop_before, prop_after) = modification[level][prop_type]
                    if prop_before == prop_max and prop_before == prop_min:
                        prop.attrib['BasicPropPA1Min'] = str(prop_after)
                        prop.attrib['BasicPropPA1Max'] = str(prop_after)
                
                # if prop_type == 'manamax_p':
                #     prop_type = 'manamax_v'
                #     (prop_before, prop_after) = modification[level][prop_type]
                #     print('create new prop')
                #     ET.SubElement(mantle, 'ListBasicProp', {
                #         'BasicPropType': prop_type,
                #         'BasicPropPA1Min': str(prop_after),
                #         'BasicPropPA1Max': str(prop_after),
                #         'BasicPropPA2Min': '-1',
                #         'BasicPropPA2Max': '-1',
                #         'BasicPropPA3Min': '0',
                #         'BasicPropPA3Max': '0',
                #         'Index': f'{prop_index}'
                #     })
                # prop_index += 1

    for mantle in root.findall('ItemData[@Level="10"]'):
        mantle.set('ResMale', '8')
        mantle.set('ResFemale', '8')

    root.write('mantle_Modified.xml',
            encoding="UTF-8", xml_declaration=True)


def reindex_props():
    filename = 'mantle_Final.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)
    for mantle in root.findall(f'ItemData'):
            prop_index = 0
            for prop in mantle.findall('ListBasicProp'):
                prop.set('Index', f'{prop_index + 1}')
                prop_index += 1
    root.write('mantle_Final_2.xml',
        encoding="UTF-8", xml_declaration=True)


def export_mantle():
    ids = []
    filename = 'mantle.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)
    for mantle in root.findall(f'ItemData'):
        level = int(mantle.get('Level'))
        if level >= 9:
            ids.append(mantle.get('ItemID'))
    
    print(','.join(ids))

def main():
    export_mantle()    
    # reindex_props()



if __name__ == '__main__':
    main()