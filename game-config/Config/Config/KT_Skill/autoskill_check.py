#!/usr/bin/env python

from pathlib import Path
import json
import xml.etree.ElementTree as ET
import re, subprocess
import copy

def minify_autoskills():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))

    default_values = [
        ('CastSkillLevel', -1),
        ('ParentSkillID', -1),

        ('MaxCastCount', -1), #="-1" 
        ('ActivateIfLostBuff', -1), #="-1" 
        ('ActivateIfHitTarget', 'false'), #="false" 
        ('ActivateWhenHPPercentDropBelow', -1), #="-1" 
        ('ActivateWhenBeHit', 'false'), #="false" 
        ('ActivateWhenDoCritHit', 'false'), #="false" 
        ('ActivateAfterDie', 'false'), #="false" 
        ('ActivateAfterDoTotalCritHit', -1), #="-1" 
        ('ActivateWhenBeHitBySkills', -1), #="-1" 
        ('ActivateWhenHitWithSkills', -1), #="787,148" 
        ('ActivateIfNoUseSkillForFrame', -1), #="-1"
        ('ActivateIfNoUseSkillCauseLostInvisibilityForFrame', -1), #="-1" 
        ('ActivateEachFrame', -1), #="-1"
    ]

    root = ET.parse("AutoSkill.xml", parser=parser)
    for autoSkill in root.findall("AutoSkill"):
        

        for (key, default_value) in default_values:
            current_value = autoSkill.get(key)
            if current_value == str(default_value):
                autoSkill.attrib.pop(key)
            
            if key == 'ActivateIfHitTarget' and current_value == 'true':
                print(autoSkill.get('CastSkillID'))

    filepath = f'AutoSkill_mod.xml'
    root.write(filepath, encoding="UTF-8", xml_declaration=True)


def main():
    minify_autoskills()


if __name__ == '__main__':
    main()
