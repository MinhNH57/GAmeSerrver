#!/usr/bin/env python

import time
import requests
import copy

BASE_URL = "http://103.47.195.143:15392/main/admin/mail"
# BASE_URL = "http://127.0.0.1:15392/main/admin/mail"
ROLES = []

MAIL_CONFIGs = [
    {
        "roles": [1800220055, 1999808717, 1800203441, 1999802640, 1800220075, 1999817418, 1800203651, 1999819531, 1800203804, 1999817183, 1999802124, 1800203592, 1999819650, 1800219987, 1999808435, 1800204715, 1800203498, 1800216386, 1999809411, 1800220423, 1800227514, 1999818309, 1999822649, 1999818039],
        "title": "Quà tham gia Võ Lâm Minh Chủ",
        "content": "Hoa Nhi thân gửi quý đại hiệp phần thưởng tham gia hoạt động Võ Lâm Minh Chủ.",
        "bac": 0,
        "knb": 0,
        "items": "25197_3_1_0",
    },
    {
        "roles": [1800203498, 1800216386, 1999817418, 1800203651, 1999817183, 1800220423, 1800219987, 1999802640],
        "title": "Quà trao thưởng TOP 16 Võ Lâm Minh Chủ",
        "content": "Trong Võ Lâm Minh Chủ, quý đại hiệp đã có phần thi đấu xuất sắc. Hoa Nhi thân gửi đại hiệp phần thưởng xếp hạng Võ Lâm Minh Chủ.",
        "bac": 0,
        "knb": 0,
        "items": "190_1_1_0|506_100_1_0|20051_20_1_0|20345_2_1_0|20344_1_1_0",
    },
    {
        "roles": [1999808435, 1999819531, 1999809411, 1800203804],
        "title": "Quà trao thưởng TOP 8 Võ Lâm Minh Chủ",
        "content": "Trong Võ Lâm Minh Chủ, quý đại hiệp đã có phần thi đấu xuất sắc. Hoa Nhi thân gửi đại hiệp phần thưởng xếp hạng Võ Lâm Minh Chủ.",
        "bac": 0,
        "knb": 0,
        "items": "190_2_1_0|506_200_1_0|20051_30_1_0|20345_4_1_0|20344_2_1_0",
    },
    {
        "roles": [1800204715, 1800220055],
        "title": "Quà trao thưởng TOP 4 Võ Lâm Minh Chủ",
        "content": "Trong Võ Lâm Minh Chủ, quý đại hiệp đã có phần thi đấu xuất sắc. Hoa Nhi thân gửi đại hiệp phần thưởng xếp hạng Võ Lâm Minh Chủ.",
        "bac": 0,
        "knb": 0,
        "items": "191_1_1_0|506_400_1_0|20379_30_1_0|20345_7_1_0|20344_3_1_0",
    },
    {
        "roles": [1999808717],
        "title": "Quà trao thưởng TOP 2 Võ Lâm Minh Chủ",
        "content": "Trong Võ Lâm Minh Chủ, quý đại hiệp đã có phần thi đấu xuất sắc. Hoa Nhi thân gửi đại hiệp phần thưởng xếp hạng Võ Lâm Minh Chủ.",
        "bac": 0,
        "knb": 0,
        "items": "191_2_1_0|506_700_1_0|20379_70_1_0|20345_14_1_0|20344_7_1_0",
    },
    {
        "roles": [1999819650],
        "title": "Quà trao thưởng TOP 1 Võ Lâm Minh Chủ",
        "content": "Trong Võ Lâm Minh Chủ, quý đại hiệp đã có phần thi đấu xuất sắc. Hoa Nhi thân gửi đại hiệp phần thưởng xếp hạng Võ Lâm Minh Chủ.",
        "bac": 0,
        "knb": 0,
        "items": "192_1_1_0|506_1000_1_0|20379_100_1_0|20345_20_1_0|20344_10_1_0|20449_1_1_0",
    },
]

BET_MAIL_TEMPLATE = {
    "title": "Quà tham gia Võ Lâm Minh Chủ",
    "content": "Hoa Nhi thân gửi quý đại hiệp phần thưởng tham gia hoạt động Võ Lâm Minh Chủ.",
    "bac": 0,
    "knb": 0,
}

BET_REWARDS = {1999822649: (1, 45),
1999817183: (20, 909),
1999819650: (10, 454)}


def send_mail():
    for config in MAIL_CONFIGs:
        mail_data = copy.deepcopy(config)
        mail_data.pop("roles")
        # mail_data.remove('roles')
        for role in config["roles"]:
            mail_data["id"] = role
            print(f"sending to {role}")
            print(mail_data)
            requests.post(BASE_URL, data=mail_data)
            time.sleep(0.3)

    for rid, (bac, ruong) in BET_REWARDS.items():
        mail_data = copy.deepcopy(BET_MAIL_TEMPLATE)
        mail_data["id"] = rid
        mail_data["items"] = f"943_{bac}_1_0|25192_{ruong}_1_0"
        print(f"sending to {rid}")
        print(mail_data)
        requests.post(BASE_URL, data=mail_data)
        time.sleep(0.3)


def main():
    send_mail()


if __name__ == "__main__":
    main()
