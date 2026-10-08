#!/usr/bin/env python

from PIL import Image
import glob
import xml.etree.ElementTree as ET
from pathlib import Path

def main():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    mapconfig = '../../Config/Config/KT_Map/MapConfig.xml'
    map_root = ET.parse(mapconfig)
    map_dict = {}
    for map in map_root.findall('Map'):
        id = map.attrib['ID']
        code = map.attrib['MapCode']
        name = map.attrib['Name']
        map_dict[code] = (id, name)

    for f in glob.glob("**/npcs.xml", recursive=True):
        map_name = Path(f).parent.name
        parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
        root = ET.parse(f, parser=parser)

        for npc in root.findall('.//NPC'):
            if npc.attrib.get('ScriptID') is None:
                continue
            scriptID = npc.attrib['ScriptID']
            if (scriptID == '27'):
                x = npc.attrib['X']
                y = npc.attrib['Y']
                (id, name) = map_dict[map_name]
                print(f'<Pos MapCode="{map_name}" MapID="{id}" MapName="{name}" X="{x}" Y="{y}" />')
    

if __name__ == '__main__':
    main()
