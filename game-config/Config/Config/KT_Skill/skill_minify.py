#!/usr/bin/env python

from pathlib import Path
import glob
import xml.etree.ElementTree as ET
import json


def minify():
    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))

    default_values = [
        ('StartPoint', 0),
        ('Form', -1),
        ('SkillStype', ''),
        ('ShortDesc', ''),
        ('FullDesc', ''),
        ('AppendDamageP', 0),
        ('ShootCount', -1),
        ('FactionID', 0),
        ('SubID', 0),
        ('IconBundleDir', 'Icon/SkillIcon.unity3d'),
        ('IconAtlasName', 'SkillIcon'),
        ('CastEffectID', 0),
        ('MaleCastSound', ''),
        ('FemaleCastSound', ''),
        ('StateEffectID', 0),
        ('IsStateNoClearOnDead', 'false'),
        ('IsArua', 'false'),
        ('AttackRadius', 0),
        ('IgnoreDistance', 'false'),
        ('IsBullet', 'false'),
        ('BulletID', 0),
        ('BulletCount', 0),
        ('CastActionID', 0),
        ('IsMelee', 'false'),
        ('IsPhysical', 'false'),
        ('IsDamageSkill', 'false'),
        ('IsUseAR', 'false'),
        ('TargetType', ''),
        ('WaitTime', 0),
        ('StartSkillID', 0),
        ('BulletSkillID', 0),
        ('FinishSkillID', 0),
        ('VanishSkillID', 0),
        ('RequireLevel', 0),
        ('MaxSkillLevel', 0),
        ('WeaponLimit', 0),
        ('HorseLimit', 'false'),
        ('IsUseRage', 'false'),
        ('IsBasicWeaponSkill', 'false'),
        ('IsCanUseEverywhere', 'false'),
        ('IsExpSkill', 'false'),
        ('ElementalSeries', 0),
        ('Param1', 0),
        ('Param2', 0),
        ('Param3', 0),
        ('Param4', 0),
        ('Param5', 0),
        ('Properties', 'empty'),
        ('BulletRoundTime', 0),
        ('IsCanAddPoint', 'false'),
        ('IsNeedCompleteQuestID', -1),
        ('FixedAttackActionCount', -1),
        ('IsSkillNoTarget', 'false'),
        ('SkillCostType', 1),
        ('IsSkillNoAddAttackSpeedCooldown', 'false'),
        ('AlwaysHit', 'false')
    ]

    root = ET.parse("SkillData.xml", parser=parser)
    for item in root.findall("Skill"):
        
        for (k, v) in default_values:
            if item.attrib.get(k) is not None and item.attrib[k] == str(v):
                item.attrib.pop(k)
            
    
    filepath = f'SkillData_mod.xml'
    root.write(filepath, encoding="UTF-8", xml_declaration=True)


def main():
    minify()


if __name__ == '__main__':
    main()
