import json
import xml.etree.ElementTree as ET
from xml.dom import minidom

def read_json_file(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        return json.load(f)

def create_item_xml(data, item_id):
    item = ET.Element("ItemData", attrib={
        "ItemID": str(item_id),
        "Name": data["Text"],
        "IconBundleDir": "Icon/EquipIcon2.unity3d",
        "IconAtlasName": "EquipIcon2",
        "MapSpriteBundleDir": "Resources/Item/MapSprite.unity3d",
        "MapSpriteAtlasName": "MapSprite",
        "Kind": "Ngoại Trang",
        "QualityPrefix": "0",
        "Category": "1",
        "Genre": "1",
        "Stack": "1",
        "DetailType": "36",
        "ParticularType": "19",
        "Level": "3",
        "Icon": "quest_0009_s",
        "View": "quest_0009_s",
        "Intro": data["Description"].split(".")[0],
        "BindType": "1",
        "Series": "0",
        "ReqLevel": "0",
        "CDType": "0",
        "Price": "0",
        "ItemValue": "10000000",
        "MakeCost": "-1",
        "ResMale": str(data["ID"]),
        "ResFemale": str(data["ID"]),
        "FightPower": "2",
        "ScriptID": "-1",
        "DeductOnUse": "false",
        "BuffID": "-1"
    })

    for idx, prop in enumerate(data.get("MagicProps", []), start=1):
        name = prop["Name"]
        if name == "lucky_p":
            name = "lucky_v"
            
        value = prop["Value"][0]
        ET.SubElement(item, "ListBasicProp", attrib={
            "BasicPropType": name,
            "BasicPropPA1Min": str(value),
            "BasicPropPA1Max": str(value),
            "Index": str(idx)
        })

    return item

def prettify(elem):
    rough_string = ET.tostring(elem, 'utf-8')
    reparsed = minidom.parseString(rough_string)
    return reparsed.toprettyxml(indent="    ")

def main():
    json_file_path = "KT_Title/title.json"
    output_xml_path = "KT_Item/BasicItem/title.xml"
    mapping_xml_path = "KT_Interactive/TitleToItemMap.xml"

    data_list = read_json_file(json_file_path)

    root = ET.Element("ArrayOfItemData", attrib={
        "xmlns:xsi": "http://www.w3.org/2001/XMLSchema-instance",
        "xmlns:xsd": "http://www.w3.org/2001/XMLSchema"
    })

    mapping_root = ET.Element("TitleItemMapping")

    starting_id = 16000

    for i, data in enumerate(data_list):
        item_id = starting_id + i

        item_xml = create_item_xml(data, item_id)
        root.append(item_xml)

        ET.SubElement(mapping_root, "Title", attrib={
            "id": str(data["ID"]),
            "itemid": str(item_id)
        })

    xml_str = prettify(root)
    with open(output_xml_path, "w", encoding="utf-8") as f:
        f.write(xml_str)

    mapping_xml_str = prettify(mapping_root)
    with open(mapping_xml_path, "w", encoding="utf-8") as f:
        f.write(mapping_xml_str)

    print(f"XML generated for {len(data_list)} items and saved to {output_xml_path}")
    print(f"Title-item mapping XML saved to {mapping_xml_path}")



if __name__ == "__main__":
    main()
