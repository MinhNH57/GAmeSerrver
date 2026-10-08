using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using Server.Tools;

public static class EventTimeManager
{
    private const string CONFIG_PATH = "Config/KT_Interactive/EventsTimeConfig.xml";
    
    [XmlRoot(ElementName = "Event")]
    public class Event
    {
        [XmlAttribute(AttributeName = "Key")] 
        public string Key;
        
        [XmlAttribute(AttributeName = "StartDate")]
        public string StartDate_str;

        [XmlAttribute(AttributeName = "EndDate")]
        public string EndDate_str;
        
        [XmlAttribute(AttributeName = "Enable")]
        public bool Enable;
        
        [XmlAttribute(AttributeName = "NewServerEvent")]
        public bool NewServerEvent;
        
        public DateTime StartDate
        {
            get
            {
                if (DateTime.TryParse(StartDate_str, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    return dt;
                return DateTime.MinValue;
            }
        }
        
        public DateTime EndDate
        {
            get
            {
                if (DateTime.TryParse(EndDate_str, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    return dt;
                return DateTime.MinValue;
            }
        }
    }

    [XmlRoot(ElementName = "Server")]
    public class ServerConfig
    {
        [XmlAttribute(AttributeName = "Id")] 
        public string Id;
        
        [XmlAttribute(AttributeName = "IsNewServer")]
        public bool IsNewServer;

        [XmlElement(ElementName = "Event")] 
        public List<Event> Events;
        
    }

    [XmlRoot(ElementName = "EventsTimeConfig")]
    public class EventsTimeConfig
    {
        [XmlElement(ElementName = "Server")] 
        public List<ServerConfig> Servers;

    }
    
    private static ServerConfig serverConfig;

    public static void Init()
    {
        var path = Global.GameResPath(CONFIG_PATH);
        if (!File.Exists(path))
        {
            return;
        }
        
        EventsTimeConfig Config = new EventsTimeConfig();

        using (var fs = File.OpenRead(path))
        {
            var serializer = new XmlSerializer(typeof(EventsTimeConfig));
            Config = serializer.Deserialize(fs) as EventsTimeConfig;
        }
        
        serverConfig = new ServerConfig();
    
        if (Config?.Servers != null)
        {
            var exactMatch = Config.Servers.FirstOrDefault(s => IsServerMatch(s.Id, exactOnly: true));
            var rangeMatch = Config.Servers.FirstOrDefault(s => IsServerMatch(s.Id, exactOnly: false));
            var fallbackMatch = Config.Servers.FirstOrDefault(s => s.Id == "-1");

            serverConfig = exactMatch ?? rangeMatch ?? fallbackMatch;
        }
    }
    
    public static bool IsEventEnabled(string timeKey)
    {
        if (serverConfig == null)
            return false;
        
        var evt = GetEvent(timeKey);
        if (evt == null || !evt.Enable)
            return false;

        return true;
    }

    public static Event GetEventConfig(string timeKey)
    {
        if (serverConfig?.Events == null)
            return null;

        return serverConfig.Events.FirstOrDefault(evt => evt.Key == timeKey);
    }

    public static Event GetEvent(string timeKey)
    {
        if (serverConfig?.Events == null)
            return null;

        foreach (var evt in serverConfig.Events)
        {
            if (evt.Key == timeKey && KTLuaLib_System.IsDateTimeInRange(evt.StartDate_str, evt.EndDate_str))
                return evt;
        }        

        return null;
    }
    
    public static bool IsNewServer() => serverConfig?.IsNewServer ?? false;
    
    private static bool IsServerMatch(string configId, bool exactOnly)
    {
        if (string.IsNullOrEmpty(configId) || configId == "-1")
            return false;

        int serverId = GameManager.ServerId;

        string[] parts = configId.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            string p = part.Trim();
            if (!exactOnly && p.Contains("-"))
            {
                string[] rangeParts = p.Split('-');
                if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out int start) && int.TryParse(rangeParts[1], out int end))
                {
                    if (serverId >= start && serverId <= end)
                        return true;
                }
            }
            else if (int.TryParse(p, out int exactId))
            {
                if (serverId == exactId)
                    return true;
            }
        }
        return false;
    }
}