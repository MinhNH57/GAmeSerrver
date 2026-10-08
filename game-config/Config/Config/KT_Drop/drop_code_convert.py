#! /opt/homebrew/bin/python3

from textwrap import indent
import xml.etree.ElementTree as ET
import json
from pathlib import Path
import base64

def main():
    filename = 'DropBuild.xml'
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(filename, parser=parser)

    for ls in root.findall('DropProfile'):
        dropid = ls.attrib['DropProfileID']
        try:
            dropconvert = base64.b64decode(dropid).decode('ascii')
            ls.attrib['DropProfileID'] = dropconvert
        except:
            continue

    root.write('DropBuild_new.xml',
               encoding="UTF-8", xml_declaration=True)


if __name__ == '__main__':
    main()