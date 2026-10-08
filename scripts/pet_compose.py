#!/usr/bin/env python

import hashlib, sys
from copy import deepcopy
import json
import glob
import xml.etree.ElementTree as ET

def load_items():
    data = {}
    for p in glob.glob("game-config/Config/Config/KT_Item/**/*.xml", recursive=True):
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib['ItemID'])
            data[itemid] = item.attrib['Name']

    for p in glob.glob("game-config/Config/Config/**/*Items.json", recursive=True):
        with open(p, 'r') as json_input:
            items = json.load(json_input)
            for item in items["RandomBoxes"]:
                data[item["ItemID"]] = item["Name"]

            for item in items["Normal"]:
                data[item["ItemID"]] = item["Name"]

    with open('game-config/Config/Config/KT_Item/Other/script_items.json', 'r') as json_input:
        items = json.load(json_input)

        for item in items:
            data[item["ItemID"]] = item["Name"]

    return data
        
def main():
    ITEMs = load_items()
    
    NEW_CONFIG = [
        [25113, 25112, 20, 7], # lưng đơn tâm 142
        [25125, 25124, 20, 7], # mũ đơn tâm 143
        [25129, 25128, 20, 3], # liên đơn tâm 144
        [25133, 25132, 20, 1], # bội đơn tâm 145
        [25117, 25116, 20, 0], # giày đơn tâm 146
        [25142, 25141, 15, 5], # vũ khí long đằng 147
        [25110, 25109, 15, 5], # áo long đằng 148
        [25122, 25121, 15, 5], # tay long đằng 149
        [25138, 25137, 15, 5], # nhẫn long đằng 150
        [25106, 25105, 15, 5], # phù long đằng 151
        [25114, 25113, 15, 5], # lưng long đằng 152
        [25126, 25125, 15, 5], # mũ long đằng 153
        [25130, 25129, 15, 5], # liên long đằng 154
        [25134, 25133, 10, 0], # bội long đằng 155
        [25118, 25117, 8, 0],   # giày long đằng 156
        
        [25201, 25200, 20, 7], # mũ băng đơn tâm 157
        [25205, 25204, 20, 7], # mũ hoả đơn tâm 158
        [25209, 25208, 20, 7], # mũ lôi đơn tâm 159
        [25213, 25212, 20, 7], # mũ vật đơn tâm 160
        [25217, 25216, 20, 7], # mũ độc đơn tâm 161
        
        [25202, 25201, 8, 0], # mũ băng đơn tâm 162
        [25206, 25205, 8, 0], # mũ hoả đơn tâm 163
        [25210, 25209, 8, 0], # mũ lôi đơn tâm 164
        [25214, 25213, 8, 0], # mũ vật đơn tâm 165
        [25218, 25217, 8, 0], # mũ độc đơn tâm 166
    ]
    
    start_id = 142
    
    TEMPLATE = {
            "compose_id": 107,
            "compose_name": "Nâng cấp Hộ uyển Đồng Hành - Đơn Tâm",
            "compose_description": "",
            "allow_multiple": False,
            "enable_source": False,
            "enable_rate_up": True,
            "rate_up_items": [20367, 20368, 20369, 20370, 20371],
            "insure_items": [20695],
            "materials": [
                {
                    "id": 25120,
                    "number": 1
                },
                {
                    "id": 25120,
                    "number": 1
                },
                {
                    "id": 25120,
                    "number": 1
                }
            ],
            "output": {
                "id": 25121,
                "number": 1
            },
            "output_based_on_input": False,
            "default_rate": 25
        }
    
    output = []
    for config in NEW_CONFIG:
        new = deepcopy(TEMPLATE)
        new['compose_id'] = start_id
        new['compose_name'] = f'Nâng cấp {ITEMs[config[0]]}'
        new['materials'] = [
            {
                'id': config[1],
                'number': 1
            },
            {
                'id': config[1],
                'number': 1
            },
            {
                'id': config[1],
                'number': 1
            }
        ]
        new['output'] = {
            'id': config[0],
            'number': 1
        }
        new['default_rate'] = config[2]
        new['actual_rate'] = config[3]
        
        output.append(new)
        start_id += 1
    
    with open('new_compose.json', 'w') as f:
        f.write(json.dumps(output, ensure_ascii=False))


if __name__ == "__main__":
    main()