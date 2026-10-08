#! /opt/homebrew/bin/python3

from textwrap import indent
import xml.etree.ElementTree as ET
import json
from pathlib import Path

def main():
    filename = 'RoleLevelUpAttributes.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    for ls in root.findall('Attribute'):
        level = int(ls.attrib['Level'])
        exp = int(ls.attrib['ExpNeed'])
        if level >= 95 and level < 99:
            exp = int(exp * 1.2)
        elif level >= 99 and level < 109:
            exp = int(exp * 1.3)
        elif level >= 109:
            exp = int(exp * 1.35)

        ls.attrib['ExpNeed'] = str(exp)

    root.write('role_modified.xml',
               encoding="UTF-8", xml_declaration=True)


if __name__ == '__main__':
    main()