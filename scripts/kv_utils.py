#!/usr/bin/env python

import xml.etree.ElementTree as ET
from pathlib import Path
import json
from collections import defaultdict
from enum import Enum


class ITEM_DETAILTYPE(Enum):
    MELEEWEAPON = 1
    RANGEWEAPON = 2
    ARMOR = 3
    RING = 4
    NECKLACE = 5
    AMULET = 6
    BOOTS = 7
    BELT = 8
    HELM = 9
    CUFF = 10
    PENDANT = 11
    HORSE = 12
    MASK = 13
    BOOK = 14
    ZHEN = 15
    SIGNET = 16
    MANTLE = 17
    CHOP = 18


def load_items():
    data = {}
    weapon16_ids = []
    for p in Path(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    ).rglob("*.xml"):
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib["ItemID"])
            data[itemid] = item.attrib["Name"]

    print(", ".join([str(x) for x in weapon16_ids]))

    for p in Path(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    ).rglob("*Items.json"):
        with open(p, "r") as json_input:
            items = json.load(json_input)
            for item in items["RandomBoxes"]:
                data[item["ItemID"]] = item["Name"]

            for item in items["Normal"]:
                data[item["ItemID"]] = item["Name"]

    with open(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item/Other/script_items.json",
        "r",
    ) as input:
        items = json.load(input)
        for item in items:
            data[item["ItemID"]] = item["Name"]
    return data


def change_price_bachngan_hoangkim():
    total_bachngan = 0
    total_hoangkim = 0
    bachngan_ids = [
        7741,
        7742,
        7743,
        7744,
        7745,
        7731,
        7732,
        7733,
        7734,
        7735,
        7939,
        7959,
        7979,
        7999,
        8019,
        7940,
        7960,
        7980,
        8000,
        8020,
        7475,
        7477,
        7479,
        7481,
        7483,
        7476,
        7478,
        7480,
        7482,
        7484,
        6753,
        6755,
        6757,
        6759,
        6761,
        6754,
        6756,
        6758,
        6760,
        6762,
        11888,
        11889,
        11890,
        11891,
        11892,
        13057,
        13058,
        13059,
        13060,
        13061,
        12608,
        12610,
        12612,
        12614,
        12616,
        12609,
        12611,
        12613,
        12615,
        12617,
    ]
    hoangkim_ids = [
        7746,
        7747,
        7748,
        7749,
        7750,
        7736,
        7737,
        7738,
        7739,
        7740,
        6221,
        6222,
        6223,
        6224,
        6225,
        6226,
        6227,
        6228,
        6229,
        6230,
        7485,
        7487,
        7489,
        7491,
        7493,
        7486,
        7488,
        7490,
        7492,
        7494,
        6763,
        6765,
        6767,
        6769,
        6771,
        6764,
        6766,
        6768,
        6770,
        6772,
        11893,
        11894,
        11895,
        11896,
        11897,
        13062,
        13063,
        13064,
        13065,
        13066,
        12618,
        12620,
        12622,
        12624,
        12626,
        12619,
        12621,
        12623,
        12625,
        12627,
    ]
    for p in Path(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    ).rglob("*.xml"):
        print(p)
        modified = False
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib["ItemID"])
            if itemid in bachngan_ids:
                modified = True
                item.set("Price", "500000")
                total_bachngan += 1

            if itemid in hoangkim_ids:
                modified = True
                item.set("Price", "1000000")
                total_hoangkim += 1

        if modified:
            root.write(p, encoding="utf-8", xml_declaration=True)

    print(total_bachngan)
    print(total_hoangkim)


def change_level_required_items():
    total_modified = 0

    ids_to_modified = [
        7613,
        7619,
        7625,
        7614,
        7620,
        7626,
        7631,
        7637,
        7643,
        7632,
        7638,
        7644,
        7649,
        7655,
        7661,
        7667,
        7650,
        7656,
        7662,
        7668,
        7741,
        7742,
        7743,
        7744,
        7745,
        7731,
        7732,
        7733,
        7734,
        7735,
        7746,
        7747,
        7748,
        7749,
        7750,
        7736,
        7737,
        7738,
        7739,
        7740,
        7706,
        7709,
        7715,
        7721,
        7727,
        7712,
        7718,
        7724,
        7730,
        8486,
        8488,
        8490,
        8492,
        8494,
        8485,
        8487,
        8489,
        8491,
        8493,
        8496,
        8498,
        8500,
        8502,
        8504,
        8495,
        8497,
        8499,
        8501,
        8503,
        8215,
        8219,
        8223,
        8227,
        8231,
        8216,
        8220,
        8224,
        8228,
        8232,
        8217,
        8221,
        8225,
        8229,
        8233,
        8218,
        8222,
        8226,
        8230,
        8234,
        8705,
        8709,
        8713,
        8717,
        8721,
        8706,
        8710,
        8714,
        8718,
        8722,
        8707,
        8711,
        8715,
        8719,
        8723,
        8708,
        8712,
        8716,
        8720,
        8724,
        8725,
        8729,
        8733,
        8737,
        8741,
        8726,
        8730,
        8734,
        8738,
        8742,
        8727,
        8731,
        8735,
        8739,
        8743,
        8728,
        8732,
        8736,
        8740,
        8744,
        8375,
        8377,
        8379,
        8381,
        8383,
        8376,
        8378,
        8380,
        8382,
        8384,
        8425,
        8427,
        8429,
        8426,
        8428,
        8430,
        8431,
        8433,
        8435,
        8432,
        8434,
        8436,
        8437,
        8439,
        8441,
        8443,
        8438,
        8440,
        8442,
        8444,
        8445,
        8446,
        8447,
        8448,
        8449,
        8450,
        8451,
        8452,
        8453,
        8454,
        8465,
        8466,
        8467,
        8468,
        8469,
        8470,
        8471,
        8472,
        8473,
        8474,
        14000,
        14001,
        14002,
        14003,
        14004,
        14005,
        14006,
        14007,
        14008,
        14009,
        14010,
        14012,
        14014,
        14016,
        14018,
        14011,
        14013,
        14015,
        14017,
        14019,
        14020,
        14022,
        14024,
        14026,
        14028,
        14021,
        14023,
        14025,
        14027,
        14029,
        14162,
        14164,
        14166,
        14168,
        14170,
        14163,
        14165,
        14167,
        14169,
        14171,
        14040,
        14042,
        14044,
        14046,
        14048,
        14041,
        14043,
        14045,
        14047,
        14049,
        14173,
        14175,
        14038,
        14174,
        14176,
        14039,
        14172,
        14032,
        14177,
        14033,
        14178,
        14030,
        14034,
        14036,
        14031,
        14035,
        14037,
    ]
    id_count = defaultdict(lambda: 0)
    for i in ids_to_modified:
        id_count[i] += 1
        if id_count[i] == 2:
            print(i)

    print(len(ids_to_modified))
    ids_modified = []
    for p in Path(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    ).rglob("*.xml"):
        # print(p)
        modified = False
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib["ItemID"])
            if itemid not in ids_to_modified:
                continue

            modified = True
            item.find("ListReqProp[@ReqPropType='5']").set("ReqPropValue", "70")
            total_modified += 1
            ids_modified.append(itemid)

        if modified:
            root.write(p, encoding="utf-8", xml_declaration=True)

    print(total_modified)
    print(len(ids_modified))
    print([x for x in ids_to_modified if x not in ids_modified])


def unused_item_ids():
    items = load_items()
    counter = 0
    while counter < 33000:
        if items.get(counter) is not None:
            counter += 1
            continue

        first = counter
        while items.get(counter) is None and counter < 33000:
            counter += 1

        last = counter - 1
        print(f"{first} -> {last}: range {last - first + 1}")


def advanced_enhance_items():
    ids_modified = []
    total_modified = 0
    ids_to_modified_level_2 = [
        11244,        11245,        11246,        11247,        11248,        11249,
        11250,        11251,        11252,        11253,        11254,        11255,        11256,        11257,
        11258,        11259,        11260,        11261,        11262,        11263,        11264,        11265,
        11266,        11267,        11268,        11269,        11270,        11271,        11272,        11273,
        11274,        11275,        11276,        11277,        11278,        11279,        11280,        11281,
        11282,        11283,        11284,        11285,        11286,        11287,        11288,        11289,
        11290,        11291,        11292,        11293,        11294,        11295,        11296,        11297,
        11298,        11299,        11300,        11301,        11302,        11303,        11304,        11305,
        11306,        11307,        11308,        11309,        11310,        11311,        11312,        11313,
        12752,        12753,        12754,        12755,        12756,        12757,        12758,        12759,
        12760,        12761,        12772,        12773
    ]

    ids_to_modified_level_3 = [
        11314,        11315,        11316,        11317,        11318,        11319,        11320,        11321,
        11322,        11323,        11324,        11325,        11326,        11327,        11328,        11329,
        11330,        11331,        11332,        11333,        11334,        11335,        11336,        11337,
        11338,        11339,        11340,        11341,        11342,        11343,        11344,        11345,
        11346,        11347,        11348,        11349,        11350,        11351,        11352,        11353,
        11354,        11355,        11356,        11357,        11358,        11359,        11360,        11361,
        11362,        11363,        11364,        11365,        11366,        11367,        11368,        11369,
        11370,        11371,        11372,        11373,        11374,        11375,        11376,        11377,
        11378,        11379,        11380,        11381,        11382,        11383,        12762,        12763,
        12764,        12765,        12766,        12767,        12768,        12769,        12770,        12771,
        12774,        12775
    ]

    for p in Path(
        "/Volumes/Work/kv1/kv1-game-server/game-config/Config/Config/KT_Item"
    ).rglob("*.xml"):
        # print(p)
        modified = False
        root = ET.parse(p)
        for item in root.findall("ItemData"):
            itemid = int(item.attrib["ItemID"])
            if itemid not in ids_to_modified_level_2:
                continue
            
            try:
                item.remove(item.find("ListEnhance[@EnhTimes='18']"))
                item.remove(item.find("ListEnhance[@EnhTimes='20']"))
            except:
                pass
            
            is_physics_dmg = item.find("GreenProp[@MagicName='addphysicsdamage_p']") is not None
            is_physics_mag = item.find("GreenProp[@MagicName='addphysicsmagic_p']") is not None
            
            if not is_physics_dmg and not is_physics_mag:
                continue

            ET.SubElement(
                item,
                "ListEnhance",
                {
                    "EnhTimes": "18",
                    "EnhMAName": "fatallystrikeenhance_p",
                    "EnhMAPA1Min": "5",
                    "EnhMAPA1Max": "5",
                    "EnhMAPA2Min": "1",
                    "EnhMAPA2Max": "1",
                    "EnhMAPA3Min": "0",
                    "EnhMAPA3Max": "0",
                },
            )
            ET.SubElement(
                item,
                "ListEnhance",
                {
                    "EnhTimes": "20",
                    "EnhMAName": "addphysicsdamage_p" if is_physics_dmg else "addphysicsmagic_p",
                    "EnhMAPA1Min": "10",
                    "EnhMAPA1Max": "10",
                    "EnhMAPA2Min": "1",
                    "EnhMAPA2Max": "1",
                    "EnhMAPA3Min": "0",
                    "EnhMAPA3Max": "0",
                },
            )

            modified = True
            total_modified += 1
            ids_modified.append(itemid)

        if modified:
            root.write(p, encoding="utf-8", xml_declaration=True)

    print(total_modified)
    print(len(ids_to_modified_level_2))

def main():
    # advanced_enhance_items()
    # change_price_bachngan_hoangkim()
    unused_item_ids()
    # change_level_required_items()


if __name__ == "__main__":
    main()
