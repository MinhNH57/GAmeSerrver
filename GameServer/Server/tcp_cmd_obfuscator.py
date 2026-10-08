#!/usr/bin/python3

import re
import pathlib
import hashlib

type_set = set()
perfect_hash = True
padding = 746

def obs(match_object):
    global perfect_hash, type_set
    cmd = match_object.group(1)
    hashed = int(str(int(hashlib.md5(cmd.encode('utf-8')).hexdigest(), 16))[0:4])
    hashed = hashed ^ padding
    while hashed in type_set:
        # nếu hash trùng thì tăng hash đến khi nào không trùng
        hashed += 1
    type_set.add(hashed)
    repl = f'{cmd} = {hashed}'
    return repl

def obfuscate(filepath):
    # file = open(filepath, 'r')
    txt = pathlib.Path(filepath).read_text()
    # cmds = filter(None, [re.findall('(CMD_\S*)( = \d+)?.*?(?=,)', line) for line in file])
    # for cmd in cmds:
    #     print(cmd)

    newtext = re.sub(' (CMD_[^ ,]*)( = \d+)?.*?(?=,)', obs, txt)
    if perfect_hash:
        print(type_set.__len__())
        with open(f'{filepath}_bak', 'w') as output:
            output.write(newtext)
    else:
        print("ERROR! NOT A PERFECT HASH. CHANGE HASH METHOD")

def main():
    obfuscate('TCPCmdEnum.cs')

if __name__ == '__main__':
    main()
