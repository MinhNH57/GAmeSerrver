#! /opt/homebrew/bin/python3

from textwrap import indent
import xml.etree.ElementTree as ET
import json
from pathlib import Path

def main():
    filename = 'Faction.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    faction_name_mapping = {}

    for faction in root.findall('Faction'):
        faction_id = faction.attrib['ID']
        faction_name = faction.attrib['Name']
        faction_name_mapping[(faction_id, '0')] = f'{faction_name} [Chung]'
        for sub in faction.findall('Sub'):
            sub_id = sub.attrib['ID']
            sub_name = sub.attrib['Name']

            faction_name_mapping[(faction_id, sub_id)] = f'{faction_name} [{sub_name}]'

    faction_name_mapping[('0', '0')] = 'Tân thủ'

    filename = 'FactionAttributes.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    for faction in root.findall('Attribute'):
        faction_id = faction.attrib['FactionID']
        sub_id = faction.attrib['SubID']
        faction.set('Name', faction_name_mapping[(faction_id, sub_id)])

    root.write('FactionAttributes_Modified.xml',
            encoding="UTF-8", xml_declaration=True)


if __name__ == '__main__':
    main()