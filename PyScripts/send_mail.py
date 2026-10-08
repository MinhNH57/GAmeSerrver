#!/usr/bin/env python

import time
import requests
import copy

BASE_URL = "http://103.47.195.143:15392/main/admin/mail"
# BASE_URL = "http://127.0.0.1:15392/main/admin/mail"
ROLES = []

MAIL_CONFIG_TOP_RANKINGSs = [
    # {
    #     "roles": [],
    #     "title": "Phần thưởng TOP 1 Tài phú",
    #     "content": "BQT gửi bằng hữu phần thưởng TOP 1 Bảng Xếp hạng Tài phú.",
    #     "bac": 4000000,
    #     "knb": 50000,
    #     "items": "15174_1_1_0|191_3_1_0|490_1_1_0|492_1_1_0|746_100_1_0|20329_1_1_0",
    # },
    # {
    #     "roles": [],
    #     "title": "Phần thưởng TOP 2 Tài phú",
    #     "content": "BQT gửi bằng hữu phần thưởng TOP 2 Bảng Xếp hạng Tài phú.",
    #     "bac": 2000000,
    #     "knb": 20000,
    #     "items": "3468_1_1_0|191_2_1_0|490_1_1_0|492_1_1_0|746_100_1_0|20330_1_1_0",
    # },
    # {
    #     "roles": [],
    #     "title": "Phần thưởng TOP 3 Tài phú",
    #     "content": "BQT gửi bằng hữu phần thưởng TOP 3 Bảng Xếp hạng Tài phú.",
    #     "bac": 2000000,
    #     "knb": 20000,
    #     "items": "191_1_1_0|490_1_1_0|492_1_1_0|746_100_1_0|20331_1_1_0",
    # },
    # {
    #     "roles": [],
    #     "title": "Phần thưởng TOP 1 Uy danh",
    #     "content": "BQT gửi bằng hữu phần thưởng TOP 1 Bảng Xếp hạng Uy danh.",
    #     "bac": 4000000,
    #     "knb": 50000,
    #     "items": "191_1_1_0|490_1_1_0|492_1_1_0|746_50_1_0",
    # },
    # {
    #     "roles": [],
    #     "title": "Phần thưởng TOP 2 Uy danh",
    #     "content": "BQT gửi bằng hữu phần thưởng TOP 2 Bảng Xếp hạng Uy danh.",
    #     "bac": 2000000,
    #     "knb": 20000,
    #     "items": "190_2_1_0|490_1_1_0|492_1_1_0|746_50_1_0",
    # },
    # {
    #     "roles": [],
    #     "title": "Phần thưởng TOP 3 Uy danh",
    #     "content": "BQT gửi bằng hữu phần thưởng TOP 3 Bảng Xếp hạng Uy danh.",
    #     "bac": 2000000,
    #     "knb": 20000,
    #     "items": "190_1_1_0|490_1_1_0|492_1_1_0|746_50_1_0",
    # },
    {
        "roles": [],
        "title": "",
        "content": "",
        "bac": 0,
        "knb": 0,
        "items": "",
    }
]


def send_mail():
    for config in MAIL_CONFIG_TOP_RANKINGSs:
        mail_data = copy.deepcopy(config)
        mail_data.pop("roles")
        # mail_data.remove('roles')
        for role in config["roles"]:
            mail_data["id"] = role
            print(f"sending to {role}")
            print(mail_data)
            requests.post(BASE_URL, data=mail_data)
            time.sleep(0.2)


def main():
    send_mail()


if __name__ == "__main__":
    main()
