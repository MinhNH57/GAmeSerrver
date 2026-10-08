#!/usr/bin/env python

from pathlib import Path
import glob
import xml.etree.ElementTree as ET
import shutil, os
import json
import copy


def load_items() -> dict[int, ET.Element]:
    data = {}
    for p in glob.glob("**/*.xml", recursive=True):
        if p.endswith("item_cloned.xml"):
            continue
        root = ET.parse(p)
        for itemData in root.findall("ItemData"):
            item_id = int(itemData.get("ItemID"))
            data[item_id] = itemData
    return data


modify_configuration = [
    # {
    #     "ids": [8034, 7871, 7887, 7903, 7919, 7855, 7863, 7879, 7895, 7911],
    #     "print": 20292,
    #     "name": "Chân Long Chi Liên",
    #     # "suite": 17,
    #     "basic_add": {"lifereplenish_p": 30},
    #     "basic": {},
    #     "green": [
    #         "lifemax_v",
    #         "manamax_v",
    #         "staminamax_v",
    #         "ignoredefenseenhance_v",
    #         "ignoredefenseenhance_p"
    #     ],
    #     "hidden": [
    #         "damage_cold_resist",
    #         "damage_fire_resist",
    #         "damage_poison_resist",
    #         "damage_light_resist",
    #         "damage_physics_resist",
    #         "state_stun_resisttime",
    #         "state_burn_resisttime",
    #         "state_slowall_resisttime",
    #         "state_weak_resisttime",
    #         "state_hurt_resisttime",
    #         "deadlystrikeenhance_r"
    #     ],
    # },
    # {
    #     "ids": [7549, 7550, 7555, 7556, 7561, 7562, 7567, 7568, 7573, 7574],
    #     "print": 20312,
    #     # "suite": 15,
    #     "name": "Chân Long Vân Bội",
    #     "basic_add": {"state_palsy_resisttime": 36},
    #     "green": [
    #         "lifemax_v",
    #         "manamax_v",
    #         "staminamax_v",
    #     ],
    #     "hidden": [
    #         "damage_cold_resist",
    #         "damage_fire_resist",
    #         "damage_poison_resist",
    #         "damage_light_resist",
    #         "damage_physics_resist",
    #         "state_stun_resisttime",
    #         "state_burn_resisttime",
    #         "state_slowall_resisttime",
    #         "state_weak_resisttime",
    #         "state_hurt_resisttime",
    #         "deadlystrikeenhance_r"
    #     ],
    # },
    {
        "ids": [7495, 7496, 7501, 7502, 7513, 7514, 7525, 7526, 7537, 7538],
        "print": 20313,
        # "suite": 15,
        "basic_add": {"state_fixed_resisttime": 36},
        "name": "Chân Long Thúc Thiết",
        "green": [
            "lifemax_v",
            "manamax_v",
            "staminareplenish_v",
        ],
        "hidden": [
            "damage_cold_resist",
            "damage_fire_resist",
            "damage_poison_resist",
            "damage_light_resist",
            "damage_physics_resist",
            "state_stun_resisttime",
            "state_burn_resisttime",
            "state_slowall_resisttime",
            "state_weak_resisttime",
            "state_hurt_resisttime",
        ],
    },
    # {
    #     "ids": [7649, 7650, 7637, 7638, 7655, 7656, 7661, 7662, 7625, 7626],
    #     "print": 20314,
    #     "basic_add": {"rangedamagereturn_p": 5},
    #     # "suite": 16,
    #     "name": "Chân Long Thúc Thiết Giáp",
    #     "green": [
    #         "lifemax_v",
    #         "manamax_v",
    #         "meleedamagereturn_p",
    #     ],
    #     "hidden": [
    #         "damage_cold_resist",
    #         "damage_fire_resist",
    #         "damage_poison_resist",
    #         "damage_light_resist",
    #         "damage_physics_resist",
    #         "state_stun_resisttime",
    #         "state_burn_resisttime",
    #         "state_slowall_resisttime",
    #         "state_weak_resisttime",
    #         "state_hurt_resisttime",
    #     ],
    # },
    # {
    #     "ids": [7937, 7938, 7957, 7958, 7977, 7978, 7997, 7998, 8017, 8018],
    #     "print": 20315,
    #     # "suite": 17,
    #     "name": "Chân Long Yêu Đái",
    #     "basic_add": {"state_slowrun_resisttime": 36},
    #     "green": [
    #         "lifemax_v",
    #         "manamax_v",
    #         "staminamax_v",
    #     ],
    #     "hidden": [
    #         "damage_cold_resist",
    #         "damage_fire_resist",
    #         "damage_poison_resist",
    #         "damage_light_resist",
    #         "damage_physics_resist",
    #         "state_stun_resisttime",
    #         "state_burn_resisttime",
    #         "state_slowall_resisttime",
    #         "state_weak_resisttime",
    #         "state_hurt_resisttime",
    #     ],
    # },
    # {
    #     "ids": [7667, 7668],
    #     "print": 20314,
    #     "basic_add": {"rangedamagereturn_p": 5},
    #     # "suite": 16,
    #     "name": "Chân Long Thúc Thiết Giáp",
    #     "green": [
    #         "lifemax_v",
    #         "manamax_v",
    #         "meleedamagereturn_p",
    #     ],
    #     "hidden": [
    #         "damage_cold_resist",
    #         "damage_fire_resist",
    #         "damage_poison_resist",
    #         "damage_light_resist",
    #         "damage_physics_resist",
    #         "state_stun_resisttime",
    #         "state_burn_resisttime",
    #         "state_slowall_resisttime",
    #         "state_weak_resisttime",
    #         "state_hurt_resisttime",
    #     ],
    # },
]


def clone_items():
    refine_doc = ET.parse("Refine.xml")

    items = load_items()

    start_id = 14162

    root = ET.Element("ArrayOfItemData")
    for config in modify_configuration:
        for item_id in config["ids"]:
            item_data = items[item_id]

            if config.get('suite') is not None:
                item_data.set('SuiteID', str(config['suite']))

            basic_add_props = config["basic_add"]
            for prop_name in basic_add_props.keys():
                current_attr = item_data.find(
                    f'ListBasicProp[@BasicPropType="{prop_name}"]'
                )

                if current_attr is not None:
                    continue

                new_index = len(item_data.findall("ListBasicProp")) + 1

                ET.SubElement(
                    item_data,
                    "ListBasicProp",
                    {
                        "BasicPropType": str(prop_name),
                        "BasicPropPA1Min": str(basic_add_props[prop_name]),
                        "BasicPropPA1Max": str(basic_add_props[prop_name]),
                        "BasicPropPA2Min": "-1",
                        "BasicPropPA2Max": "-1",
                        "BasicPropPA3Min": "-1",
                        "BasicPropPA3Max": "-1",
                        "Index": str(new_index),
                    },
                )

            green_props = config["green"]
            for prop_name in green_props:
                current_attr = item_data.find(f'GreenProp[@MagicName="{prop_name}"]')
                if current_attr is None:
                    continue
                current_level = int(current_attr.get("MagicLevel"))
                current_attr.set("MagicLevel", str(current_level + 1))

            hidden_props = config["hidden"]
            for prop_name in hidden_props:
                current_attr = item_data.find(f'HiddenProp[@MagicName="{prop_name}"]')
                if current_attr is None:
                    continue
                current_level = int(current_attr.get("MagicLevel"))
                current_attr.set("MagicLevel", str(current_level + 1))

            old_name = item_data.get("Name")
            old_id = item_data.get("ItemID")
            new_name = config.get('name')
            if 'Uyển' in old_name:
                new_name = 'Chân Long Thúc Thiết Uyển'
            elif 'Trạc' in old_name:
                new_name = 'Chân Long Thúc Thiết Trạc'
            elif 'Hương Nang' in old_name:
                new_name = 'Chân Long Hương Nang'
            item_data.set("Name", new_name)
            item_data.set("ItemID", str(start_id))
            root.append(item_data)

            ET.SubElement(
                refine_doc.getroot(),
                "Refine",
                {
                    "RefineId": str(config["print"]),
                    "SourceItem": str(old_id),
                    "ProduceItem": str(start_id),
                    "Fee": "10000",
                },
            )
            start_id += 1

    doc = ET.ElementTree(root)
    doc.write("item_cloned_2.xml", encoding="utf-8", xml_declaration=True)

    refine_doc.write("Refine_clone.xml", encoding="utf-8", xml_declaration=True)


def clone_ff_event():
    data = {}
    p = "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item/BasicItem/mantle.xml"
    p_modified = "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item/BasicItem/mantle_added.xml"
    root = ET.parse(p)
    for item in root.findall("ItemData"):
        itemid = int(item.attrib["ItemID"])
        data[itemid] = item
            
    start_id = 13156
    
    mantles = [i for i in data.values()]
    for v in mantles:
        mantle_level = int(v.get("Level"))
        if mantle_level < 8 or mantle_level > 10:
            continue
            
        item_data = copy.deepcopy(v)
        old_id = item_data.get("ItemID")
        print(f'clone {old_id} to {start_id}')
        item_data.set("ItemID", str(start_id))
        old_name = item_data.get("Name")
        item_data.set("Name", f"{old_name} [Sự kiện]")
        old_intro = item_data.get("Intro")
        item_data.set("Intro", f"{old_intro}\nPhi Phong [sự kiện] không thể tách thành Ngũ Hành Hồn Thạch.")
        start_id += 1
        print('appending...')
        root.getroot().append(item_data)
        
    root.write(p_modified, encoding="utf-8", xml_declaration=True)

def main():
    clone_ff_event()


if __name__ == "__main__":
    main()
