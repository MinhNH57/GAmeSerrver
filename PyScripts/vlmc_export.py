#!/usr/bin/env python

import sys
from pathlib import Path
import glob
import json
import xml.etree.ElementTree as ET
import subprocess
import re


def analysis(path):
    list_player_joined = []
    top_player = set()
    list_bet = []

    winner_rid = 0

    total_bet_win = 0
    total_bet_lose = 0

    with open(path, 'r') as input:
        lines = input.readlines()
        for line in lines:
            match = re.search(r'(\d+) đăng ký tham gia giải đấu thành công', line)
            if match is not None:
                rid = int(match.group(1))
                list_player_joined.append(rid)

            match = re.search(r'RoleID= (\d+) đạt top (\d+) Võ Lâm Minh Chủ lần này!', line)
            if match is not None:
                rid = int(match.group(1))
                rank = int(match.group(2))
                top_player.add((rid, rank))
                if rank == 1:
                    winner_rid = rid

            match = re.search(r'RoleID=(\d+) đã đặt cược (\d+) .*? vào.*? playerID=(\d+)', line)
            if match is not None:
                rid = int(match.group(1))
                bet_value = int(match.group(2))
                bet_rid = int(match.group(3))

                list_bet.append((rid, bet_value, bet_rid))

    for (_, bet_value, bet_rid) in list_bet:
        if bet_rid == winner_rid:
            total_bet_win += bet_value
        else:
            total_bet_lose += bet_value

    print('joined')
    print(list_player_joined)
    print('top 16')
    print([x[0] for x in top_player if x[1] == 16])
    print('top 8')
    print([x[0] for x in top_player if x[1] == 8])
    print('top 4')
    print([x[0] for x in top_player if x[1] == 4])
    print('top 2')
    print([x[0] for x in top_player if x[1] == 2])
    print('top 1')
    print([x[0] for x in top_player if x[1] == 1])
    # print(list_bet)
    print('total bet lose')
    print(total_bet_lose)
    print('total bet win')
    print(total_bet_win)

    for rid, bet_value, bet_rid in list_bet:
        if bet_rid != winner_rid:
            continue

        money_count = int(bet_value / 100000)
        coupon_count = int(bet_value * total_bet_lose / total_bet_win / 10000)

        print(f'{rid}: ({money_count}, {coupon_count})')



def main(args):
    analysis(args[0])


if __name__ == '__main__':
    args = sys.argv[1:]
    main(args)

