#!/usr/bin/env python

import sys
import openpyxl
import json
import subprocess
from pathlib import Path
from enum import Enum
import traceback

global RateType
RateType = Enum("RateType", ["PARTICULAR", "JOIN"])


def from_row_to_items(row, rate_type, default_item_id=-1):
    (
        item_id,
        item_name,
        count,
        particular_rate,
        join_rate,
        expiry,
        notify,
        preview,
        *_,
    ) = row
    item = {
        "ItemID": default_item_id if item_id is None else int(item_id),
        "Info": str(item_name),
        "Number": 1 if count is None else int(count),
    }
    if rate_type == RateType.PARTICULAR:
        item["Rate"] = float(particular_rate)
    else:
        item["Rate"] = float(join_rate)

    if expiry is not None:
        item["Time"] = int(expiry) * 24 * 60

    if notify is not None:
        # print(notify)
        item["Notify"] = int(notify) == 1

    if preview is not None:
        # print(preview)
        item["IsPreview"] = int(preview) == 1

    return item


def main(filepath):
    wb = openpyxl.open(filepath, data_only=True)

    for sheet_name in wb.sheetnames:
        try:
            ws = wb[sheet_name]
            # print(sheet_name)
            rows = [r for r in ws.values]

            # tên
            box = {"BoxName": rows[0][0], "IDBox": -1, "Binding": 1, "Items": []}
            box["BoxName"] = rows[0][0]
            # print(rows[0][0])

            i = 2
            while i < len(rows):
                print(rows[i])
                (item_id, item_name, saved_count, particular_rate, join_rate, expiry, notify, preview, *_) = rows[i]
                if item_id is None and item_name is None:
                    break
                if item_name == "TỔNG":
                    break

                group_items = {
                    "Rate": float(join_rate),
                    "Info": item_name,
                    "ItemID": -1 if item_id is None else int(item_id),
                    "Details": []
                }
                
                if expiry is not None:
                    group_items["Time"] = int(expiry) * 24 * 60

                if notify is not None:
                    # print(notify)
                    group_items["Notify"] = int(notify) == 1

                if preview is not None:
                    # print(preview)
                    group_items["IsPreview"] = int(preview) == 1
                # if item_id is None:
                #     group_items.pop("ItemID")

                while i + 1 < len(rows):
                    i += 1
                    (item_id, item_name, count, particular_rate, join_rate, *_) = rows[
                        i
                    ]
                    if join_rate is not None:
                        break
                    item = from_row_to_items(
                        rows[i],
                        RateType.PARTICULAR,
                        default_item_id=group_items.get("ItemID"),
                    )
                    group_items["Details"].append(item)
                    
                if len(group_items["Details"]) == 0:
                    # không có vật phẩm trong nhóm -> là vật phẩm thường
                    group_items.pop('Details')
                    group_items['Number'] = 1 if saved_count is None else int(saved_count)
                else:
                    # là nhóm -> xóa itemid do đã đưa vào các vật phẩm con
                    group_items.pop('ItemID')
                    if group_items.get('Notify') is not None: group_items.pop('Notify')
                    if group_items.get('IsPreview') is not None: group_items.pop('IsPreview')
                    if group_items.get('Time') is not None: group_items.pop('Time')

                box["Items"].append(group_items)
                
            # print(box)
            json_name = Path(filepath).parent.joinpath(
                f"event_jsons/{sheet_name}.json".replace(" ", "_")
            )
            print(json_name)
            Path(json_name).parent.mkdir(exist_ok=True)
            with open(json_name, "w", encoding="utf-8") as output:
                json.dump(box, output, ensure_ascii=False)

            cmd = f"prettier --write --print-width=160 --tab-width=4 {json_name}"
            # print(os.getcwd())
            subprocess.run(cmd, shell=True)
        except:
            print(traceback.format_exc())
            


if __name__ == "__main__":
    args = sys.argv[1:]
    main(args[0])
