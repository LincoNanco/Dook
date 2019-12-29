﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Dook
{

    /// <summary>
    /// This class represents a parameterized SQL query predicate. It stores a SQL query string and its parameters.
    /// </summary>
    public class SQLPredicate 
    {
        public  SQLPredicate()
        {

        }

        public string Sql { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        /// <summary>
        /// The key for this Dictionary is Type Name
        /// </summary>
        /// <returns></returns>
        public SortedDictionary<string, Dictionary<string,ColumnInfo>> TableMappings { get; set; } = new SortedDictionary<string, Dictionary<string,ColumnInfo>>();
        /// <summary>
        /// The key for this Dictionary is Type Name
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> Aliases { get; set; } = new Dictionary<string, string>();

        public void SetParameters<T>(T dbCommand) where T : IDbCommand
        {
            foreach (string parameter in Parameters.Keys)
            {
                var par = dbCommand.CreateParameter();
                par.ParameterName = parameter;
                par.Value = Parameters[parameter];
                dbCommand.Parameters.Add(par);
            }
        }
    }
}