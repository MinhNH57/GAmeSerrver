using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
//using MySql.Data.MySqlClient;
using MySQLDriverCS;
using System.Configuration;
using System.Data.Common;
using System.Collections.Generic;
using MILogin;
namespace Maticsoft.DBUtility
{
    /// <summary>
    /// 数据访问抽象基础类
    /// Copyright (C) Maticsoft
    /// </summary>
    public abstract class DbHelperMySQL2
    {
        //数据库连接字符串(web.config来配置)，可以动态更改connectionString支持多数据库.		
        public static string connectionString = PubConstant.ConnectionString;
        public static string connectionLogString = PubConstant.ConnectionLogString;
        public static bool loadConnectStr = false;
        public static string realConnStrGame = "";
        public static string realConnStrLog = "";
        public DbHelperMySQL2()
        {
        }

        public static  void LoadConnectStr()
        {
            Dictionary<string, string> mapParams = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            string logStr = connectionLogString ?? "";
            if (!string.IsNullOrEmpty(logStr))
            {
                string[] strParams = logStr.Split(';');
                foreach (var param in strParams)
                {
                    string[] map = param.Split('=');
                    if (map.Length != 2)
                        continue;
                    map[0] = map[0].Trim();
                    map[1] = map[1].Trim();
                    mapParams[map[0]] = map[1];
                }
                
                string hostLog = mapParams.ContainsKey("host") ? mapParams["host"] : "";
                string dbLog = mapParams.ContainsKey("database") ? mapParams["database"] : "";
                string userLog = mapParams.ContainsKey("user id") ? mapParams["user id"] : "";
                string passLog = mapParams.ContainsKey("password") ? mapParams["password"] : "";
                string portLog = mapParams.ContainsKey("port") ? mapParams["port"] : "3306";
                string charsetLog = mapParams.ContainsKey("charset") ? mapParams["charset"] : "utf8";
                realConnStrLog = String.Format("Location={0};Data Source={1};User ID={2};Password={3};Port={4};CharSet={5};", 
                    hostLog, dbLog, userLog, passLog, portLog, charsetLog);
            }
            else
            {
                realConnStrLog = "";
            }

            mapParams.Clear();
            string gameStr = connectionString ?? "";
            if (!string.IsNullOrEmpty(gameStr))
            {
                string[] strParams = gameStr.Split(';');
                foreach (var param in strParams)
                {
                    string[] map = param.Split('=');
                    if (map.Length != 2)
                        continue;
                    map[0] = map[0].Trim();
                    map[1] = map[1].Trim();
                    mapParams[map[0]] = map[1];
                }

                string hostGame = mapParams.ContainsKey("host") ? mapParams["host"] : "";
                string dbGame = mapParams.ContainsKey("database") ? mapParams["database"] : "";
                string userGame = mapParams.ContainsKey("user id") ? mapParams["user id"] : "";
                string passGame = mapParams.ContainsKey("password") ? mapParams["password"] : "";
                string portGame = mapParams.ContainsKey("port") ? mapParams["port"] : "3306";
                string charsetGame = mapParams.ContainsKey("charset") ? mapParams["charset"] : "utf8";
                realConnStrGame = String.Format("Location={0};Data Source={1};User ID={2};Password={3};Port={4};CharSet={5};", 
                    hostGame, dbGame, userGame, passGame, portGame, charsetGame);
            }
            else
            {
                realConnStrGame = "";
            }
                
            loadConnectStr = true;
        }

        /// <summary>
        /// 执行查询语句，返回MySQLDataReader ( 注意：调用该方法后，一定要对MySQLDataReader进行Close )
        /// </summary>
        /// <param name="strSQL">查询语句</param>
        /// <returns>MySQLDataReader</returns>
        public static MySQLDataReader ExecuteReader(string strSQL,bool islog = false)
        {
            if (!loadConnectStr)
            {
                LoadConnectStr();
            }

            MySQLConnection connection = null;
            if (islog)
            {
                connection = new MySQLConnection(string.IsNullOrEmpty(realConnStrLog) ? realConnStrGame : realConnStrLog);
            }
            else
            {
                connection = new MySQLConnection(realConnStrGame);
            }

            MySQLCommand cmd = new MySQLCommand(strSQL, connection);
            MySQLCommand cmdname = new MySQLCommand("SET NAMES 'utf8mb4'", connection);
            try
            {
                connection.Open();
                int res = cmdname.ExecuteNonQuery();
                MySQLDataReader myReader = cmd.ExecuteReaderEx();
                return myReader;
            }
            catch (MySql.Data.MySqlClient.MySqlException e)
            {
                throw e;
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (cmdname != null)
                {
                    cmdname.Dispose();
                }
                if (connection != null)
                {
                    connection.Dispose();
                    connection.Close();
                }
            }

        }
    }

}
