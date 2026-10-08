#!/usr/bin/env python

# script quản lý việc tự động tắt và khởi chạy game server

import time
import psutil
import subprocess
import xml.etree.ElementTree as ET
import urllib.request
import socket
import datetime
import signal
import shutil
import os
import wget
import zipfile
import traceback

# lịch bảo trì (ngày trong tuần [0-6][monday-sunday], giờ, phút)
scheduled_time = [
    (0, 5, 0), # 2
    (1, 5, 0), # 3
    (2, 5, 0), # 4
    (3, 5, 0), # 5
    (4, 5, 0), # 6 -- hẹn giờ bảo trì 
    (5, 5, 0), # 7
    (6, 5, 0), # CN
    (7, 6, 2024, 4, 0)
]

def is_server_running() -> bool:
    for proc in psutil.process_iter():
        with proc.oneshot():
            if proc.name() == 'GameServer.exe':
                return True
    return False

def exit_server(ip, port, server_id):
    counter = 0
    # tắt server bằng cách gọi request exitServer đến webapi
    urllib.request.urlopen(f'http://{ip}:{port}/exitServer?serverId={server_id}')

    # lặp vô hạn 1s/ lần đến khi server tắt xong
    while True:

        # nếu chưa tắt xong thì tiếp tục chờ
        if is_server_running():
            counter += 1
            print(f'server is shuting down... {counter}')
            time.sleep(1)
        else:
            time.sleep(5)
            # restart_db_server()
            return

def start_server(port):
    counter = 0
    # mở server ở console mới để không ảnh hưởng tới tiến trình watcher
    subprocess.Popen('GameServer.exe', creationflags=subprocess.CREATE_NEW_CONSOLE)

    # lặp vô hạn 1s/lần đến khi server khởi chạy xong
    # đánh dấu bằng sự kiện port webapi được mở
    while True:
        # khởi tạo socket đến port webapi
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        result = sock.connect_ex(('127.0.0.1', int(port)))
        if result == 0:
            print("server started successfully")
            time.sleep(5)
            return
        else:
            counter += 1
            print(f"server is starting... {counter}")
        sock.close()
        time.sleep(1)

def get_maintainance():
    try:
        wget.download('http://res.kiemvuong1.adnxgame.com/maintain/maintainance.zip')
        print('[maintain] get update zip file')
        time.sleep(60)
    except:
        # ignore
        traceback.print_exc()
    finally:
        return


# copy file mới nếu có
def copy_maintainance():
    try:
        with zipfile.ZipFile("maintainance.zip","r") as zip_ref:
            zip_ref.printdir()
            zip_ref.extractall()
        print('[maintain] extracted update from zip')
    except:
        # ignore
        print('[maintain] no patch updated')
        traceback.print_exc()
    finally:
        if os.path.exists("maintainance.zip"):
            os.remove("maintainance.zip")

    # if os.path.isdir('maintainance'):
    #     shutil.copytree('maintainance', '.', dirs_exist_ok=True, ignore=shutil.ignore_patterns('*.DS_Store'))
    #     shutil.rmtree('maintainance')

# handler bắt sự kiện ấn Ctrl C
def break_handler(signum, frame):
    res = input("^C was pressed. Do you really want to exit? y/n ")
    if res == 'y':
        exit(1)

def main():
    # xử lý sự kiện ấn Ctrl-C
    signal.signal(signal.SIGINT, break_handler)

    # đọc cấu hình server từ file AppConfig.xml
    app_config = ET.parse('AppConfig.xml').getroot()
    web_address = app_config.find('APIWebServer').attrib['IP']
    web_port = app_config.find('APIWebServer').attrib['Port']
    local_port = app_config.find('Socket').attrib['port']
    server_id = app_config.find('Server').attrib['LineID']
    
    # lặp vô hạn
    while True:
        # nếu server đang không chạy thì khởi chạy server
        if (not is_server_running()):
            # return
            copy_maintainance()
            start_server(local_port)
            continue

        current = datetime.datetime.now()

        if (current.weekday(), current.hour + 1, current.minute) in scheduled_time:
            get_maintainance()
        elif (current.day, current.month, current.year, current.hour + 1, current.minute) in scheduled_time:
            get_maintainance()

        # server đang chạy
        # nếu đến giờ đã được hẹn trước thì tắt server
        if (current.weekday(), current.hour, current.minute) in scheduled_time or (current.day, current.month, current.year, current.hour, current.minute) in scheduled_time:
            exit_server(web_address, web_port, server_id)

            # sau khi tắt thì chờ sang phút tiếp theo mới tiếp tục lặp
            new_min = datetime.datetime.now().minute
            new_sec = datetime.datetime.now().second
            if new_min != current.minute:
                time.sleep(5)
                continue
            else:
                time.sleep(60 - new_sec + 5)
        else:
            # thông báo server đang chạy kèm thời gian
            print(f'server is running... {current.time()}')
            time.sleep(10) 

DB_ROOT = 'D:/DBServer'

def start_db_server():
    subprocess.Popen(f'{DB_ROOT}/GameDBServer.exe', creationflags=subprocess.CREATE_NEW_CONSOLE, cwd=DB_ROOT)

def restart_db_server():
    for proc in psutil.process_iter():
        with proc.oneshot():
            if proc.name() == 'GameDBServer.exe':
                proc.kill()

    time.sleep(2)
    if os.path.isdir(f'{DB_ROOT}/maintainance'):
        shutil.copytree(f'{DB_ROOT}/maintainance', DB_ROOT, dirs_exist_ok=True, ignore=shutil.ignore_patterns('*.DS_Store'))
        shutil.rmtree(f'{DB_ROOT}/maintainance')

    time.sleep(2)
    start_db_server()
    counter = 0
    # lặp vô hạn 1s/lần đến khi server khởi chạy xong
    # đánh dấu bằng sự kiện port webapi được mở
    while True:
        # khởi tạo socket đến port webapi
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        result = sock.connect_ex(('127.0.0.1', 23001))
        if result == 0:
            print("db started successfully")
            time.sleep(5)
            return
        else:
            counter += 1
            print(f"db is starting... {counter}")
        sock.close()
        time.sleep(1)
    
if __name__ == '__main__':
    main()
