-- danh sách user vip (nạp trên 10m)
select * from (select sum(realmoney) as total, left(userid, 32) as accountid from t_money group by accountid) as t where t.total > 100000;

-- danh sách user đăng nhập gần đây
select * from (select userid, max(lasttime) as mlast, max(logofftime) as mlogoff from t_roles group by userid) as t where mlogoff > '2024-08-15 00:00:00' or mlast > '2024-08-15 00:00:00';

-- danh sách user đã từng nạp
select * from (select sum(realmoney) as total, left(userid, 32) as accountid from t_money group by accountid) as t where t.total >= 0;