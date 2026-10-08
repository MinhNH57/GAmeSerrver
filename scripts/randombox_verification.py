#!/usr/bin/env python

import sys
from pathlib import Path
import glob
import json
import xml.etree.ElementTree as ET
import subprocess
import os

item_info = {}

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


def verify(path):
    print(path)

    item_info = load_items()
    item_info[-1] = "Không thưởng"
    item_info[1] = "Bạc khóa"
    item_info[2] = "Bạc"
    item_info[3] = "KNB khóa"
    item_info[4] = "KNB"
    item_info[5] = "EXP"
    item_info[7] = "Danh vọng"
    item_info[8] = "Danh hiệu"
    item_info[9] = "Hoạt lực"
    item_info[10] = "Tinh lực"
    item_info[11] = "Tinh hoạt lực"
    
    with open(path, 'r', encoding='utf-8') as json_file:
        jObj = json.load(json_file)
        for box in jObj:
            print(box["BoxName"])
            rate_sum = 0
            for item in box["Items"]:
                rate = item["Rate"]
                rate_sum += rate if rate != -1 and item.get("Sex") is None else 0
                if item.get("ItemID") is not None:
                    count = item.get("Number")
                    if count is None:
                        count = 1
                    item["Info"] = f'{count} x {item_info[item["ItemID"]]}'
                
                if item.get("Details") is None:
                    continue

                item_rate = 0
                for d in item["Details"]:
                    item_rate += d["Rate"] if d["Rate"] != -1 else 0
                    if d.get("ItemID") is not None:
                        count = d.get("Number")
                        if count is None:
                            count = 1
                        d["Info"] = f'{count} x {item_info[d["ItemID"]]}'

                if round(item_rate, 6) != 100:
                    print(f'--- WARNING {item["Info"]} có rate không chính xác: {item_rate}')
                    return

            if round(rate_sum, 6) != 100 and rate_sum != 0:
                print(f'--- WARNING: {box["BoxName"]} rate không chính xác: {rate_sum}')
                return
            else:
                print(f'{box["BoxName"]} rate: {rate_sum}')


    with open(f'{path}', 'w', encoding='utf-8') as output:
        json.dump(jObj, output, ensure_ascii=False)

    cmd = f'prettier --write --print-width=200 --tab-width=4 {path}'
    # print(os.getcwd())
    subprocess.run(cmd, shell=True)


def main(args):
    if len(args) == 0:
        for p in Path('game-config/Config/Config/KT_Item/RandomBox').rglob('randomboxes_*.json'):
            verify(p)
        verify('game-config/Config/Config/KT_Item/randomboxes.json')
    elif args[0] == 'current':
        verify('game-config/Config/Config/KT_Item/Events/Boxes.json')
    else:
        for p in glob.glob(f'game-config/Config/Config/KT_Item/Events/{args[0]}/Boxes*.json', recursive=True):
            verify(p)


if __name__ == '__main__':
    args = sys.argv[1:]
    main(args)

