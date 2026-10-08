using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MySQLDriverCS;
using Server.Tools;
using Tmsk.Tools;

namespace Tmsk.DbHelper
{
    public class MyDbConnection1
    {
        MySQLConnection DbConn = null;
        MySQLConnectionString ConnStr = null;

        public string DatabaseKey = null;
        private string DatabaseName = null;

        public MyDbConnection1(string connStr, string dbNames)
        {
            bool success = false;
            MySQLConnection dbConn = null;

            try
            {
                Dictionary<string, string> mapParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string[] strParams = connStr.Split(';');
                foreach (var param in strParams)
                {
                    string[] map = param.Split('=');
                    if (map.Length != 2)
                        continue;
                    map[0] = map[0].Trim();
                    map[1] = map[1].Trim();
                    mapParams[map[0]] = map[1];
                }
                
                string host = mapParams.ContainsKey("host") ? mapParams["host"] : "";
                string db = mapParams.ContainsKey("database") ? mapParams["database"] : "";
                string userId = mapParams.ContainsKey("user id") ? mapParams["user id"] : "";
                string password = mapParams.ContainsKey("password") ? mapParams["password"] : "";
                string port = mapParams.ContainsKey("port") ? mapParams["port"] : "3306";
                string charset = mapParams.ContainsKey("charset") ? mapParams["charset"] : "utf8";

                // Manually construct MySQLDriverCS connection string
                string connStrFinal = String.Format("Location={0};Data Source={1};User ID={2};Password={3};Port={4};CharSet={5};", 
                    host, db, userId, password, port, charset);

                //Hashtable hashLog = (Hashtable)MUJson.jsonDecode("{" + connectionLogString + "}");
                //Hashtable hashGame = (Hashtable)MUJson.jsonDecode("{" + connectionString + "}");
                /*
                ConnStr = new MySQLConnectionString(
                    mapParams["host"],
                    mapParams["database"],
                    mapParams["user id"],
                    mapParams["password"]
                    );

                string connStrFinal = ConnStr.AsString;
                if (mapParams.ContainsKey("port"))
                {
                    connStrFinal += ";Port=" + mapParams["port"];
                }
                */

                dbConn = new MySQLConnection(connStrFinal);
                dbConn.Open(); //打开数据库连接

                MySQLCommand cmd = null;

                //执行链接查询的字符集
                if (!string.IsNullOrEmpty(dbNames))
                {
                    cmd = new MySQLCommand(string.Format("SET names '{0}'", dbNames), dbConn);
                    cmd.ExecuteNonQuery();
                }

                DatabaseName = DbConn.Database;
                success = true;
                DbConn = dbConn;
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            if (!success && null != dbConn)
            {
                try
                {
                    dbConn.Close();
                }
                catch { }
            }
        }

        public bool IsConnected()
        {
            if (null != DbConn)
            {
                if (DbConn.State != ConnectionState.Closed && 0 == ((int)DbConn.State & (int)ConnectionState.Broken))
                {
                    return true;
                }
            }

            return false;
        }

        public void Close()
        {
            if (null != DbConn)
            {
                try
                {
                    DbConn.Close();
                }
                catch { }
            }
        }

        public void UseDatabase(string databaseKey, string databaseName)
        {
            MySQLCommand cmd = null;
            if (databaseKey != DatabaseKey)
            {
                try
                {
                    cmd = new MySQLCommand(string.Format("use '{0}'", databaseName), DbConn);
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();

                    DatabaseKey = databaseKey;
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteExceptionUseCache(ex.ToString());
                }
            }
        }
    }

}
