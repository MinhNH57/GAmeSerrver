#!/usr/bin/env python

import sys
import xml.etree.ElementTree as ET
from pathlib import Path
import re

def main():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    filepath = 'SystemTasks.xml'

    et = ET.parse(filepath, parser)
    root = et.getroot()
    for task in root.findall('Tasks/Task'):
        task_class = task.attrib['TaskClass']
        if (task_class != '0'):
            continue
        task_name = task.attrib['Title']
        exp = int(task.attrib['Experienceaward'])
        new_exp = str(int(exp * 1.25))
        print(f'{task_name} [{task_class}]: {exp} -> {new_exp}')
        task.attrib['Experienceaward'] = new_exp

    filepath = f'{filepath}.mod'
    et.write(filepath, encoding="UTF-8", xml_declaration=True)

if __name__ == '__main__':
    main()
