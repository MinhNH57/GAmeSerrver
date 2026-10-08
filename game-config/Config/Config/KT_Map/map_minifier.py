#!/usr/bin/env python

from pathlib import Path
import json
import xml.etree.ElementTree as ET
import re, subprocess
import copy

def minify_maps():

    maps = []
    categories = {}

    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))

    default_values = [
        ('MusicBundle', 'Resources/Map/{}/Music.unity3d'),
        ('ImageFolder', 'Resources/Map/{}/Image'),
        ('Obstruction', 'Assets/MapConfig/{}/obs.xml'),
        ('teleports', 'Assets/MapConfig/{}/teleports.xml'),
        ('zones', 'Assets/MapConfig/{}/zones.xml'),
        ('npcs', 'Assets/MapConfig/{}/npcs.xml'),
        ('Monsters', 'Assets/MapConfig/{}/Monsters.xml'),
        ('Setting', 'Assets/MapConfig/{}/MapSetting.xml'),
        ('ShowMiniMap', 'true'),
        ('GrowPoints', 'Assets/MapConfig/{}/GrowPoints.xml'),
        ('GrowPoints', ''),
        ('MapLevel', '0')
    ]

    root = ET.parse("MapConfig.xml", parser=parser)
    for mapItem in root.findall("Map"):
        mapCode = mapItem.get('MapCode')

        for (key, default_value) in default_values:
            current_value = mapItem.get(key)
            if '{}' in default_value:
                if current_value == default_value.format(mapCode):
                    mapItem.attrib.pop(key)
            else:
                if current_value == default_value:
                    mapItem.attrib.pop(key)

        width = int(mapItem.get('Width'))
        height = int(mapItem.get('Height'))
        part_width = int(mapItem.get('PartWidth'))
        part_height = int(mapItem.get('PartHeight'))
        h_count = int(mapItem.get('HorizontalCount'))
        v_count = int(mapItem.get('VerticalCount'))

        if h_count == round(width / part_width):
            mapItem.attrib.pop('HorizontalCount')

        if v_count == round(height / part_height):
            mapItem.attrib.pop('VerticalCount')


        music_bundle = mapItem.get('MusicBundle')
        image_folder = mapItem.get('ImageFolder')
        if music_bundle is not None and image_folder is not None:
            cloned_music = re.search('Resources/Map/(.*?)/Music.unity3d', music_bundle).group(1)
            cloned_image = re.search('Resources/Map/(.*?)/Image', image_folder).group(1)
            if cloned_image is not None and cloned_music is not None and cloned_music == cloned_image:
                mapItem.attrib.pop('MusicBundle')
                mapItem.attrib.pop('ImageFolder')
                mapItem.set('ClonedMapCode', cloned_image)


        map_dict = copy.deepcopy(mapItem.attrib)
        map_dict['ID'] = int(map_dict['ID'])
        map_dict['Size'] = [int(map_dict['Width']), int(map_dict['Height'])]
        map_dict.pop('Width')
        map_dict.pop('Height')
        map_dict['PartSize'] = [int(map_dict['PartWidth']), int(map_dict['PartHeight'])]
        map_dict.pop('PartWidth')
        map_dict.pop('PartHeight')

        maptype = mapItem.get('MapType')
        if categories.get(maptype) is None:
            categories[maptype] = []

        categories[maptype].append(mapCode)
        map_dict.pop('MapType')

        maps.append(map_dict)




        # item.attrib.pop('ObjID')
        # unlock_interval = item.find('UnLockInterval')
        # if unlock_interval is not None:
        #     item.remove(unlock_interval)
            
    
    # print(json.dumps(maps))
    filepath = 'MapConfig.json'
    with open(filepath, 'w') as output:
        json.dump({
            'Maps': maps,
            'Categories': categories
        }, output, ensure_ascii=False)

    cmd = f'prettier --write --print-width=240 --tab-width=4 {filepath}'
    # print(os.getcwd())
    subprocess.run(cmd, shell=True)

    filepath = f'MapConfig_mod.xml'
    root.write(filepath, encoding="UTF-8", xml_declaration=True)


def main():
    minify_maps()


if __name__ == '__main__':
    main()
