#!/usr/bin/env python

import hashlib, sys

        
def main(hashed: str):
    hashed = hashed.lower()
    for i in range(0, 100000000):
        raw = f'adnx_{i}_kv1_mobile'
        md5 = hashlib.md5(raw.encode('ascii')).hexdigest()
        if md5 == hashed:
            print(i)
            return


if __name__ == "__main__":
    args = sys.argv[1]
    main(args)