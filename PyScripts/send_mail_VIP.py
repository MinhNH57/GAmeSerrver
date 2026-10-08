#!/usr/bin/env python

import time
import requests
import copy

BASE_URL = "http://103.47.195.143:15392/main/admin/mail"
# BASE_URL = "http://127.0.0.1:15392/main/admin/mail"
ROLES = []

MAIL_CONFIG_TOP_RANKINGSs = [
    {
        "roles": [
            1999833136,
            214823,
            303981,
            1999883122,
            246248,
            205092,
            352062,
            240727,
            219002,
            206233,
            226423,
            362736,
            1999830840,
            1999831917,
            999851328,
            1999893329,
            1800220423,
        ],
        "title": "Phần quà tri ân S-VIP",
        "content": "BQT gửi bằng hữu phần quà tri ân S-VIP tháng 5.",
        "bac": 0,
        "knb": 0,
        "items": "20585_1_1_0",
    },
    {
        "roles": [
            1999867813,
            1999884409,
            1999892505,
            1999886273,
            278921,
        ],
        "title": "Phần quà tri ân V-VIP",
        "content": "BQT gửi bằng hữu phần quà tri ân V-VIP tháng 5.",
        "bac": 0,
        "knb": 0,
        "items": "20584_1_1_0",
    },
    {
        "roles": [
            245715,
            245419,
            277034,
            1800203651,
            202593,
            255802,
            1999854557,
            1999838726,
            283894,
            1999883146,
            1999873754,
            1999866648,
            1999806332,
            1999882896,
            1999888639,
            351565,
            1800203804,
            1999892240,
            1999864877,
            1999888562,
            254656,
            199816454,
        ],
        "title": "Phần quà tri ân VIP",
        "content": "BQT gửi bằng hữu phần quà tri ân VIP tháng 5.",
        "bac": 0,
        "knb": 0,
        "items": "20583_1_1_0",
    },
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
