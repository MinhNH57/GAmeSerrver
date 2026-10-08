# kv1 deploy

## setup mysql server

- cài đặt MySQLServer
- tạo 2 database mới: 1 database RUNTIME dùng để lưu data từ game server, 1 database LOG dùng để ghi log (tên có thể thay đổi tùy ý)
- chạy lần lượt 2 script SQL: gamedb_empty.sql và t_config.sql

## setup database server và log server

> IP ở môi trường local thường set là `127.0.0.1`

- trong file `DBServer/AppConfig.xml`, thẻ `Database` sửa lại các attribute `ip`, `port`, `dname` (tên của RUNTIME database), `uname`, `upassd` theo như cấu hình đã cài đặt từ MySQL Server
  > `uname` và `upassd` được lấy bằng cách encrypt username/password của database, sử dụng hàm `String.Encrypt` trong file `StringEncrypt.cs` với key dùng để encrypt là `eabcix675u49,/` và salt là `3&3i4x4^+-0`

- cũng trong file `DBServer/AppConfig.xml`, thẻ `GameServer/Line`, thay đổi các attribute `ID`, `ip`, `port`. Đây là ID, IP và PORT của Game Server.

- cũng trong file `DBServer/AppConfig.xml`, thay đổi attribute `Socket/port`. Đây là PORT của database server.

- trong file `LogServer/AppConfig.xml`, thẻ `Database` sửa lại các attribute `ip`, `port`, `dname` (tên của LOG database), `uname`, `upassd` theo như cấu hình đã cài đặt từ MySQL Server, tương tự như cách làm với database server

- sau khi cấu hình xong, chạy lần lượt `DBServer/GameDBServer.exe` và `LogServer/LogServer.exe`. Tiến trình chạy không có exception tức là setup đã thành công.

## setup game server

- trong file `GameServer/AppConfig.xml`, thay đổi các thẻ và attribute như sau
  - `Server/LineID`: ID của Game Server
  - `Socket/Port`: PORT của Game Server
  - `DBServer/ip`, `DBServer/port`: IP và PORT của database server
  - `LogDBServer/ip`, `LogDBServer/port`: IP và PORT của log server

- chạy file `GameServer/GameServer.exe`. Tiến trình chạy không có exception tức là thành công.

## setup client

- client sử dụng Unity 2021.3.22f, platform là Android
- mở project, vào menu `Utils/Open Data Folder` và copy toàn bộ nội dung của folder `Resources` vào folder `Android`.
- thay đổi file `ServerList.json`, điều chỉnh lại `nServerID`, `strURL` (ip), `nServerPort` tương ứng với game server đã cấu hình. Tên của game server có thể đặt tùy ý.
- chạy game từ Unity, scene MainGame.
