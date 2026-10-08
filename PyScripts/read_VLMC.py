def read_and_get_values(file_path):
    """
    Đọc file txt và lấy giá trị từng dòng.
    
    Args:
        file_path: Đường dẫn đến file txt
        
    Returns:
        list: Danh sách các giá trị từ mỗi dòng
    """
    values = []
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            for line_num, line in enumerate(file, 1):
                # Loại bỏ khoảng trắng và ký tự xuống dòng
                line = line.strip()
                # Bỏ qua dòng trống
                if line:
                    values.append({
                        'line_number': line_num,
                        'value': line,
                        'raw': line
                    })
        return values
    except FileNotFoundError:
        print(f"Lỗi: Không tìm thấy file '{file_path}'.")
        return None
    except Exception as e:
        print(f"Lỗi khi đọc file: {e}")
        return None


def read_lines_with_split(file_path, delimiter=None):
    """
    Đọc file và tách giá trị từng dòng theo delimiter (ví dụ: dấu phẩy, tab, khoảng trắng).
    
    Args:
        file_path: Đường dẫn đến file txt
        delimiter: Ký tự phân cách (None = dùng split() mặc định cho khoảng trắng)
        
    Returns:
        list: Danh sách các giá trị đã được tách từ mỗi dòng
    """
    values = []
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            for line_num, line in enumerate(file, 1):
                line = line.strip()
                if line:
                    if delimiter:
                        parts = line.split(delimiter)
                    else:
                        parts = line.split()  # Tách theo khoảng trắng
                    values.append({
                        'line_number': line_num,
                        'values': parts,
                        'raw': line
                    })
        return values
    except FileNotFoundError:
        print(f"Lỗi: Không tìm thấy file '{file_path}'.")
        return None
    except Exception as e:
        print(f"Lỗi khi đọc file: {e}")
        return None


def extract_between_brackets(file_path, required_substring=None, start_delimiter='[', end_delimiter=']'):
    """
    Đọc file và lấy tất cả giá trị nằm giữa cặp ký tự bắt đầu/kết thúc cho mỗi dòng.
    Mặc định là '[' và ']'. Có thể truyền vào 2 ký tự (hoặc chuỗi) bất kỳ.
    Chỉ xử lý các dòng có chứa 'required_substring' (nếu được cung cấp).

    Args:
        file_path (str): Đường dẫn file cần đọc
        required_substring (str | None): Chỉ lấy các dòng chứa chuỗi này
        start_delimiter (str): Ký tự/chuỗi bắt đầu (mặc định '[')
        end_delimiter (str): Ký tự/chuỗi kết thúc (mặc định ']')

    Returns:
        list[dict]: Mỗi phần tử gồm line_number và values (list các chuỗi)
    """
    import re

    # Escape để dùng trong regex an toàn cho mọi ký tự đặc biệt
    pattern = re.escape(start_delimiter) + r"(.*?)" + re.escape(end_delimiter)

    results = []
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            for line_num, line in enumerate(file, 1):
                if required_substring and required_substring not in line:
                    continue
                
                matches = re.findall(pattern, line)
                if matches:
                    results.append({
                        'line_number': line_num,
                        'values': [m.strip() for m in matches]
                    })
        return results
    except FileNotFoundError:
        print(f"Lỗi: Không tìm thấy file '{file_path}'.")
        return None
    except Exception as e:
        print(f"Lỗi khi đọc file: {e}")
        return None


def extract_ints_between_brackets(file_path, required_substring=None, start_delimiter='[', end_delimiter=']'):
    """
    Giống extract_between_brackets nhưng chỉ giữ các giá trị chuyển được sang int.

    Args:
        start_delimiter (str): Ký tự/chuỗi bắt đầu (mặc định '[')
        end_delimiter (str): Ký tự/chuỗi kết thúc (mặc định ']')

    Returns:
        list[dict]: line_number và values (list[int])
    """
    results = []
    raw = extract_between_brackets(
        file_path,
        required_substring=required_substring,
        start_delimiter=start_delimiter,
        end_delimiter=end_delimiter,
    )
    if not raw:
        return raw
    for item in raw:
        ints = []
        for v in item['values']:
            try:
                iv = int(v.strip())
                ints.append(iv)
            except (ValueError, TypeError):
                continue
        if ints:
            results.append({'line_number': item['line_number'], 'values': ints})
    return results


def extract_value_after_phrase(file_path, phrase="Tổng tiền cược thắng: "):
    """
    Tìm dòng chứa 'phrase', lấy chính xác phần ngay sau chuỗi đó (token đầu tiên),
    rồi cố gắng chuyển thành số nguyên. Trả về int nếu chuyển được, nếu không
    trả về chuỗi token (đã strip). Nếu không tìm thấy thì trả về None.
    """
    import re

    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            for line in file:
                if phrase in line:
                    tail = line.split(phrase, 1)[1].strip()
                    if not tail:
                        return None
                    # Lấy token đầu tiên ngay sau phrase
                    first_token = tail.split()[0]
                    # Chuẩn hoá: bỏ dấu phân tách ngàn thường gặp (',' hoặc '.')
                    normalized = re.sub(r"[,.]", "", first_token)
                    try:
                        return int(normalized)
                    except ValueError:
                        return first_token
        return None
    except FileNotFoundError:
        print(f"Lỗi: Không tìm thấy file '{file_path}'.")
        return None
    except Exception as e:
        print(f"Lỗi khi đọc file: {e}")
        return None


def wait_until_time(target_time: str):
    """
    Chờ đến đúng mốc thời gian rồi trả về.
    - Hỗ trợ hai dạng:
      + 'HH:MM' hoặc 'HH:MM:SS' (mốc giờ trong ngày, nếu đã qua sẽ đợi tới ngày hôm sau)
      + 'YYYY-MM-DD HH:MM' hoặc 'YYYY-MM-DD HH:MM:SS' (mốc thời gian tuyệt đối)
    """
    from datetime import datetime, timedelta
    import time as _time

    now = datetime.now()

    # Nếu có dấu '-' và khoảng trắng, coi như chuỗi ngày-giờ tuyệt đối
    if '-' in target_time and ' ' in target_time:
        # Chuẩn hoá có giây
        if len(target_time.split(':')) == 2:
            target_time = target_time + ":00"
        try:
            target_dt = datetime.strptime(target_time, "%Y-%m-%d %H:%M:%S")
        except ValueError:
            # Sai định dạng thì không chờ
            return
        if target_dt <= now:
            # Nếu thời điểm đã qua, không chờ (hoặc có thể chọn đợi tới tương lai tuỳ nhu cầu)
            return
        seconds = (target_dt - now).total_seconds()
        if seconds > 0:
            _time.sleep(seconds)
        return

    # Ngược lại, coi là mốc giờ trong ngày
    fmt = "%H:%M:%S" if len(target_time.split(':')) == 3 else "%H:%M"
    today_str = now.strftime("%Y-%m-%d")
    dt_str = f"{today_str} {target_time if fmt=='%H:%M:%S' else target_time+':00'}"
    try:
        target_dt = datetime.strptime(dt_str, "%Y-%m-%d %H:%M:%S")
    except ValueError:
        return

    if target_dt <= now:
        target_dt = target_dt + timedelta(days=1)

    seconds = (target_dt - now).total_seconds()
    if seconds > 0:
        _time.sleep(seconds)


def last_friday_of_month(year: int, month: int):
    """
    Trả về datetime.date của ngày Thứ 6 cuối cùng trong tháng (year, month).
    """
    from datetime import date, timedelta
    import calendar

    last_day = calendar.monthrange(year, month)[1]
    d = date(year, month, last_day)
    # weekday(): Mon=0 .. Sun=6. Friday=4
    offset = (d.weekday() - 4) % 7
    return d - timedelta(days=offset)


def build_target_time_last_friday(hour_min_sec: str = "07:30:00", year: int | None = None, month: int | None = None) -> str:
    """
    Tạo chuỗi TARGET_TIME dạng "YYYY-MM-DD HH:MM:SS" ứng với Thứ 6 cuối cùng
    của tháng chỉ định. Nếu không truyền year/month, dùng tháng hiện tại.
    """
    from datetime import datetime

    now = datetime.now()
    y = year if year is not None else now.year
    m = month if month is not None else now.month
    last_fri = last_friday_of_month(y, m)
    # Bổ sung giây nếu chỉ có HH:MM
    if len(hour_min_sec.split(":")) == 2:
        hour_min_sec = hour_min_sec + ":00"
    return f"{last_fri.strftime('%Y-%m-%d')} {hour_min_sec}"

def build_playerid_playername (listPlayerIDs: list, listPlayerNames: list):
    results = []
    if listPlayerIDs or listPlayerNames:
        money_by_line = {it['line_number']: it['values'] for it in (listPlayerNames or [])}
        for it in (listPlayerIDs or []):
            line_no = it['line_number']
            ids = it['values']
            monies = money_by_line.get(line_no, [])
            max_len = max(len(ids), len(monies)) if max(len(ids), len(monies)) > 0 else 0
            if max_len == 0:
                results.append(' '.join(map(str, ids)) + "\n")
                continue
            for i in range(max_len):
                id_str = ids[i] if i < len(ids) else ''
                money_str = monies[i] if i < len(monies) else ''
                results.append(f"{id_str} -- {money_str} \n")
    else:
        return None
    return results
def main():
    from datetime import datetime, timedelta

    now = datetime.today()
    friday = now - timedelta(days=1)
    y = friday.year
    m = friday.month
    d = friday.day
    #file_path = rf"E:\\Log\\LeagueBattle_{y}{m}{d}.log"  # Thay đổi đường dẫn file của bạn
    file_path = r"D:\Projects\kv1\kv1-game-server\PyScripts\LeagueBattle_20260529.log"
    phrase = "đăng ký tham gia giải đấu thành công"
    listJoinPlayers = extract_ints_between_brackets(file_path, required_substring=phrase, start_delimiter='[', end_delimiter=']')
    if not listJoinPlayers:
        listJoinPlayers = []

    phrase = "đạt top 16 Võ Lâm Minh Chủ lần này!"
    listTop16Players = extract_ints_between_brackets(file_path, required_substring=phrase, start_delimiter='RoleID= ', end_delimiter=' đạt')
    if not listTop16Players:
        listTop16Players = []
    listTop16Names = extract_between_brackets(file_path, required_substring=phrase, start_delimiter='</color> ', end_delimiter=',')
    if not listTop16Names:
        listTop16Names = []

    phrase = "đạt top 8 Võ Lâm Minh Chủ lần này!"
    listTop8Players = extract_ints_between_brackets(file_path, required_substring=phrase, start_delimiter='RoleID= ', end_delimiter=' đạt')
    if not listTop8Players:
        listTop8Players = []
    listTop8Names = extract_between_brackets(file_path, required_substring=phrase, start_delimiter='</color> ', end_delimiter=',')
    if not listTop8Names:
        listTop8Names = []

    phrase = "đạt top 4 Võ Lâm Minh Chủ lần này!"
    listTop4Players = extract_ints_between_brackets(file_path, required_substring=phrase, start_delimiter='RoleID= ', end_delimiter=' đạt')
    if not listTop4Players:
        listTop4Players = []
    listTop4Names = extract_between_brackets(file_path, required_substring=phrase, start_delimiter='</color> ', end_delimiter=',')
    if not listTop4Names:
        listTop4Names = []

    phrase = "đạt top 2 Võ Lâm Minh Chủ lần này!"
    listTop2Players = extract_ints_between_brackets(file_path, required_substring=phrase, start_delimiter='RoleID= ', end_delimiter=' đạt')
    if not listTop2Players:
        listTop2Players = []
    listTop2Names = extract_between_brackets(file_path, required_substring=phrase, start_delimiter='</color> ', end_delimiter=',')
    if not listTop2Names:
        listTop2Names = []

    phrase = "đạt top 1 Võ Lâm Minh Chủ lần này!"
    listTop1Players = extract_ints_between_brackets(file_path, required_substring=phrase, start_delimiter='RoleID= ', end_delimiter=' đạt')
    if not listTop1Players:
        listTop1Players = []
    listTop1Names = extract_between_brackets(file_path, required_substring=phrase, start_delimiter='</color> ', end_delimiter=',')
    if not listTop1Names:
        listTop1Names = []

    # Ghi kết quả ra file
    from datetime import datetime
    read_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    out_path = r"E:\\result.txt"
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(f"Thời gian đọc: {read_time}\n")
        f.write(f"Nguồn: {file_path}\n")
        
        f.write("\nTitle tham gia : Quà tham gia giải đấu Võ Lâm Minh Chủ")
        f.write("\nContent tham gia : BQT gửi bạn phần quà tham dự giải đấu Võ Lâm Minh Chủ")
        f.write("\nQuà tham gia : 25197_5_1_0\n")
        f.write("\nDanh sách tham gia :\n")
        if listJoinPlayers:
            for item in listJoinPlayers:
                f.write(' '.join(map(str, item['values'])) + "\n")
        else:
            f.write("Không có giá trị số nào khớp điều kiện.\n")

        f.write("\nTitle Top 16: Trao thưởng quà Top 16 Võ Lâm Minh Chủ")
        f.write("\nContent Top 16 : Trong giải đấu Võ Lâm Minh Chủ, bạn đã thể hiện xuất sắc và đạt Top 16. BQT gửi bạn phần thưởng ở thư đính kèm.")
        f.write("\nQuà Top 16 : 191_1_1_0|506_400_1_0|20344_20_1_0|20529_50_1_0|25192_50_1_0")
        f.write("\nDanh sách Top 16 : \n")
        if listTop16Players and listTop16Names:
            result = build_playerid_playername(listPlayerIDs= listTop16Players, listPlayerNames= listTop16Names)
            if result:
                count = 0;
                for item in result:
                    count += 1;
                    if count > 16:
                        break;
                    f.write(item)
        else:
            f.write("Không có giá trị số nào khớp điều kiện.\n")


        f.write("\nTitle Top 8: Trao thưởng quà Top 8 Võ Lâm Minh Chủ")
        f.write("\nContent Top 8 : Trong giải đấu Võ Lâm Minh Chủ, bạn đã thể hiện xuất sắc và đạt Top 8. BQT gửi bạn phần thưởng ở thư đính kèm.")
        f.write("\nQuà Top 8 : 191_2_1_0|506_700_1_0|20344_30_1_0|20529_80_1_0|25192_80_1_0")
        f.write("\nDanh sách Top 8 :\n")
        if listTop8Players and listTop8Names:
            result = build_playerid_playername(listPlayerIDs= listTop8Players, listPlayerNames= listTop8Names)
            if result:
                count = 0;
                for item in result:
                    count += 1;
                    if count > 8:
                        break;
                    f.write(item)
        else:
            f.write("Không có giá trị số nào khớp điều kiện.\n")
    
        f.write("\nTitle Top 4: Trao thưởng quà Top 4 Võ Lâm Minh Chủ")
        f.write("\nContent Top 4 : Trong giải đấu Võ Lâm Minh Chủ, bạn đã thể hiện xuất sắc và đạt Top 4. BQT gửi bạn phần thưởng ở thư đính kèm.")
        f.write("\nQuà Top 4 : 191_3_1_0|506_1000_1_0|20344_50_1_0|20529_150_1_0|25192_150_1_0")
        f.write("\nDanh sách Top 4 :\n")
        if listTop4Players and listTop4Names:
            result = build_playerid_playername(listPlayerIDs= listTop4Players, listPlayerNames= listTop4Names)
            if result:
                count = 0;
                for item in result:
                    count += 1;
                    if count > 4:
                        break;
                    f.write(item)
        else:
            f.write("Không có giá trị số nào khớp điều kiện.\n")

        f.write("\nTitle Top 2: Trao thưởng quà Top 2 Võ Lâm Minh Chủ")
        f.write("\nContent Top 2 : Trong giải đấu Võ Lâm Minh Chủ, bạn đã thể hiện xuất sắc và đạt Top 2. BQT gửi bạn phần thưởng ở thư đính kèm.")
        f.write("\nQuà Top 2 : 192_1_1_0|506_2000_1_0|20344_70_1_0|20529_200_1_0|25192_200_1_0\n")
        f.write("\nDanh sách Top 2 :\n")
        if listTop2Players and listTop2Names:
            result = build_playerid_playername(listPlayerIDs= listTop2Players, listPlayerNames= listTop2Names)
            if result:
                count = 0;
                for item in result:
                    count += 1;
                    if count > 2:
                        break;
                    f.write(item)
        else:
            f.write("Không có giá trị số nào khớp điều kiện.\n")

        f.write("\nTitle Top 1: Trao thưởng quà Top 1 Võ Lâm Minh Chủ")
        f.write("\nContent Top 1 : Trong giải đấu Võ Lâm Minh Chủ, bạn đã thể hiện xuất sắc và đạt Top 1. BQT gửi bạn phần thưởng ở thư đính kèm.")
        f.write("\nQuà Top 1 : 192_2_1_0|506_3000_1_0|20344_100_1_0|20529_300_1_0|25192_300_1_0|20449_1_1_0")
        f.write("\nDanh sách Top 1 :\n")
        if listTop1Players and listTop1Names:
            result = build_playerid_playername(listPlayerIDs= listTop1Players, listPlayerNames= listTop1Names)
            if result:
                count = 0;
                for item in result:
                    count += 1;
                    if count > 1:
                        break;
                    f.write(item)
        else:
            f.write("Không có giá trị số nào khớp điều kiện.\n")

        
        total_bet_win = extract_value_after_phrase(file_path, phrase="Tổng tiền cược thắng: ")
        f.write(f"\nTổng tiền cược thắng : {total_bet_win}\n")

        total_bet_lose = extract_value_after_phrase(file_path, phrase="Tổng tiền cược thua: ")
        f.write(f"\nTổng tiền cược thua : {total_bet_lose}\n")

        # Danh sách đặt cược cho từng người thuộc Top 1
        count = 0;
        for item in listTop1Players:
            count += 1;
            if count > 1:
                break;
            player_id_str = ' '.join(map(str, item['values']))
            f.write(f"\nTitle đặt cược {player_id_str}: Quà dự đoán Võ Lâm Minh Chủ")
            f.write(f"\nContent đặt cược {player_id_str} : Bạn tham gia dự đoán Võ Lâm Minh Chủ và dự đoán đúng. Xin chúc mừng!")
            f.write(f"\nDanh sách đặt cược {player_id_str}:\n")
            phrase = f"playerID={player_id_str}"
            listBetPlayers = extract_ints_between_brackets(
                file_path,
                required_substring=phrase,
                start_delimiter='RoleID=',
                end_delimiter=' đã'
            )
            money_values = extract_ints_between_brackets(
                file_path,
                required_substring=phrase,
                start_delimiter='cược ',
                end_delimiter=' bạc'
            )
            if listBetPlayers or money_values:
                money_by_line = {it['line_number']: it['values'] for it in (money_values or [])}
                for it in (listBetPlayers or []):
                    line_no = it['line_number']
                    ids = it['values']
                    monies = money_by_line.get(line_no, [])
                    max_len = max(len(ids), len(monies)) if max(len(ids), len(monies)) > 0 else 0
                    if max_len == 0:
                        f.write(' '.join(map(str, ids)) + "\n")
                        continue
                    for i in range(max_len):
                        id_str = ids[i] if i < len(ids) else ''
                        money_str = monies[i] if i < len(monies) else ''
                        # tính toán số vé thưởng
                        number_item = round(money_str * total_bet_lose / total_bet_win / 15000)
                        if number_item > 1000:
                            number_item = 1000
                        elif number_item <=0:
                            number_item = 1
                        # tính toán số bạc hoàn trả
                        money_refund = round(money_str /100000)
                        # chuỗi thuỏng
                        reward_str = f"943_{money_refund}_1_0|25192_{number_item}_1_0"
                        f.write(f"{id_str} --- Quà: {reward_str}\n")


            else:
                f.write("Không có giá trị số nào khớp điều kiện.\n")

if __name__ == "__main__":
    # Ví dụ: đặt TARGET_TIME là Thứ 6 cuối cùng của tháng hiện tại lúc 07:30:00
    TARGET_TIME = build_target_time_last_friday("07:30:00")
    print(TARGET_TIME)
    # Hoặc: TARGET_TIME = build_target_time_last_friday("07:30:00", year=2025, month=11)
    #wait_until_time(TARGET_TIME)
    main()