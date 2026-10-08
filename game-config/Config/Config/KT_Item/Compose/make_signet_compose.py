#!/usr/bin/env python

from pathlib import Path
import glob
import xml.etree.ElementTree as ET
import shutil, os
import json


SIGNET_CONFIG = {
    1: {"name": "Bồi Nhung", "flags": 20, "hunshi": 200, "gathermake": 3000},
    2: {"name": "Triều Nghĩa", "flags": 60, "hunshi": 600, "gathermake": 3500},
    3: {"name": "Nhân Dũng", "flags": 100, "hunshi": 1000, "gathermake": 4000},
    4: {"name": "Tư Trí", "flags": 140, "hunshi": 1400, "gathermake": 4500},
    5: {"name": "Ngự Võ", "flags": 180, "hunshi": 1800, "gathermake": 5000},
    6: {"name": "Tuyên Tiết", "flags": 220, "hunshi": 3300, "gathermake": 5500},
    7: {"name": "Dực Huy", "flags": 260, "hunshi": 3900, "gathermake": 6000},
    8: {"name": "Trí Quả", "flags": 300, "hunshi": 4500, "gathermake": 6500},
    9: {"name": "Chấn Uy", "flags": 340, "hunshi": 5100, "gathermake": 7000},
    10: {"name": "Chiêu Võ", "flags": 380, "hunshi": 5700, "gathermake": 7500},
    11: {"name": "Du Kích", "flags": 420, "hunshi": 6300, "gathermake": 8000},
    12: {"name": "Du Kỵ", "flags": 460, "hunshi": 6900, "gathermake": 8500},
    13: {"name": "Ninh Viễn", "flags": 500, "hunshi": 7500, "gathermake": 9000},
    14: {"name": "Định Viễn", "flags": 540, "hunshi": 8100, "gathermake": 9500},
    15: {"name": "Minh Uy", "flags": 580, "hunshi": 8700, "gathermake": 10000},
    16: {"name": "Tuyên Uy", "flags": 620, "hunshi": 12400, "gathermake": 10500},
    17: {"name": "Tráng Võ", "flags": 660, "hunshi": 13200, "gathermake": 11000},
    18: {"name": "Trung Võ", "flags": 700, "hunshi": 14000, "gathermake": 11500},
    19: {"name": "Quy Đức", "flags": 740, "hunshi": 14800, "gathermake": 12000},
    20: {"name": "Vân Huy", "flags": 780, "hunshi": 15600, "gathermake": 12500},
    21: {"name": "Hoài Hóa", "flags": 820, "hunshi": 20500, "gathermake": 13000},
    22: {"name": "Quán Quân", "flags": 860, "hunshi": 21500, "gathermake": 13500},
    23: {"name": "Trấn Quốc", "flags": 900, "hunshi": 22500, "gathermake": 14000},
    24: {"name": "Phụ Quốc", "flags": 940, "hunshi": 23500, "gathermake": 14500},
    25: {"name": "Phiêu Kỵ", "flags": 980, "hunshi": 24500, "gathermake": 15000}
}


def clone_items():
    list_recipes = []
    for index in range(1, len(SIGNET_CONFIG) + 1):
        recipe = {}
        config = SIGNET_CONFIG[index]
        recipe["compose_id"] = 10 + index
        recipe["compose_name"] = f'Nâng cấp Lôi Đình Ấn - {config["name"]} [{index}/25]'
        recipe[
            "compose_description"
        ] = f'Nâng cấp Lôi Đình Ấn lên cấp {index} - {config["name"]}. Sau khi nâng cấp sẽ giữ nguyên cấp độ cường/nhược hóa của ấn.'
        recipe["enable_source"] = True
        recipe["enable_rate_up"] = False
        recipe["default_rate"] = 100
        recipe["output_based_on_input"] = True
        recipe["allow_multiple"] = False
        recipe["source"] = {"ids": [13100 + index - 1], "forge_level": -1}
        recipe["materials"] = [
            {"id": 20344, "number": config["flags"]},
            {"id": 506, "number": config["hunshi"]},
        ]
        recipe["fees"] = [
            {"type": 4, "number": config["gathermake"]},
            {"type": 5, "number": config["gathermake"]},
        ]
        recipe["output"] = {"id": 13100 + index, "number": 1, "series": -1}
        list_recipes.append(recipe)

    with open("temp_compose.json", "w") as output:
        json.dump(list_recipes, output, ensure_ascii=False)


def main():
    clone_items()


if __name__ == "__main__":
    main()
