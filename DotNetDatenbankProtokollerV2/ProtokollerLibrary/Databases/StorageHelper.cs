﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.CSVFile;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.Excel;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.Interfaces;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.MsSQL;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.MySQL;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.PLC;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.PostgreSQL;
using DotNetSimaticDatabaseProtokollerLibrary.Databases.SQLite;
using DotNetSimaticDatabaseProtokollerLibrary.SettingsClasses.Datasets;
using DotNetSimaticDatabaseProtokollerLibrary.SettingsClasses.Storage;

namespace DotNetSimaticDatabaseProtokollerLibrary.Databases
{
    public static class StorageHelper        
    {
        public static IDBInterface GetStorage(ProtokollerConfiguration config, StorageConfig cfg, Action<string> NewDataCallback)
        {
            if (cfg is SQLiteConfig)
                return new SQLLiteStorage(NewDataCallback);
            else if (cfg is CSVConfig)
                return new CSVStorage(NewDataCallback);
            else if (cfg is ExcelConfig)
                return new ExcelStorage(NewDataCallback);
            else if (cfg is PostgreSQLConfig)
                return new PostgreSQLStorage(NewDataCallback);
            else if (cfg is MySQLConfig)
                return new MySQLStorage(NewDataCallback);
            else if (cfg is Excel2007Config)
                return new Excel2007Storage(NewDataCallback);
            else if (cfg is MsSQLConfig)
                return new MsSQLStorage(NewDataCallback);
            else if (cfg is PLCConfig)
                return new PLCStorage(NewDataCallback);
            else if (cfg is MultiStorageConfig)
                return new MultiStorage.MultiStorage(config, cfg, NewDataCallback);
            return null;
        }        
    }
}
