using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using sqlexport.Properties;

namespace SqlExport
{
    class Program
    {
        static int tableCount = 0, procCount = 0, viewCount = 0, funcCount = 0;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            CommandArgument ca = null;
            try
            {
                ca = new CommandArgument(args);
                CheckAgrument(ca.Server, "服务器地址必须设置");
                CheckAgrument(ca.UserName, "登录名称必须设置");
                CheckAgrument(ca.Password, "登录密码必须设置");
                CheckAgrument(ca.DataBase, "数据库名称必须设置");
            }
            catch (Exception ex)
            {
                Console.WriteLine("错误：" + ex.Message);
                Console.WriteLine(CommandArgument.ShowHelp());
                return;
            }


            var outputDir = "";
            if (string.IsNullOrWhiteSpace(ca.Output))
            {
                outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            else
            {
                if (!ca.Output.Contains(@":\"))
                {
                    outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ca.Output);
                }
            }
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var batFile = Path.Combine(outputDir, "setupdb.bat");
            if (File.Exists(batFile))
            {
                File.Delete(batFile);
            }
            File.WriteAllText(batFile, Resources.setupdb, Encoding.GetEncoding("GB2312"));

            var file = Path.Combine(outputDir, "Scripts.sql");
            if (File.Exists(file))
            {
                File.Move(file, Path.GetFileName(file) + "." + DateTime.Now.ToFileTimeUtc().ToString());
            }

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(file, false, Encoding.GetEncoding("GB2312"));
                var dbName = (string.IsNullOrWhiteSpace(ca.NewDataBase) ? ca.DataBase : ca.NewDataBase);
                sw.WriteLine("create database " + dbName);
                sw.WriteLine("GO");
                sw.WriteLine("use " + dbName);
                sw.WriteLine("GO");
                using (TimeUse tu = new TimeUse("数据库结构创建导出"))
                {
                    WriteSchemaInfo(ca, sw);
                    Console.WriteLine("数据结构导出完毕，共计{4}个(表：{0} 存储过程：{1} 视图：{2} 用户函数：{3})", tableCount, procCount, viewCount, funcCount, tableCount + procCount + viewCount + funcCount);
                }
                using (TimeUse tu = new TimeUse("基础数据导出"))
                {
                    ExportData(dbName, ca, sw);
                }
            }
            catch (Exception ex)
            {
                if (Process.GetCurrentProcess().ProcessName.Contains("debug"))
                {
                    Console.WriteLine("生成出错：\n" + ex.ToString());
                }
                else
                {
                    Console.WriteLine("生成出错：" + ex.Message);
                }
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
            }
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("ConnectionInfo"))
                return Assembly.Load(Resources.Microsoft_SqlServer_ConnectionInfo);
            else if (args.Name.Contains("Management.Sdk.Sfc"))
                return Assembly.Load(Resources.Microsoft_SqlServer_Management_Sdk_Sfc);
            else if (args.Name.Contains("Smo"))
                return Assembly.Load(Resources.Microsoft_SqlServer_Smo);
            else if (args.Name.Contains("SqlClrProvider"))
                return Assembly.Load(Resources.Microsoft_SqlServer_SqlClrProvider);
            else if (args.Name.Contains("SqlEnum"))
                return Assembly.Load(Resources.Microsoft_SqlServer_SqlEnum);
            else
                return null;
        }


        static void WriteSchemaInfo(CommandArgument ca, StreamWriter sw)
        {
            Console.WriteLine("正在创建数据库结构脚本...");
            var conn = new ServerConnection(ca.Server, ca.UserName, ca.Password);
            var svr = new Server(conn);
            var db = svr.Databases[ca.DataBase];

            Console.WriteLine("正在生成表结构...");
            foreach (Table tb in db.Tables)
            {
                if (tb.IsSystemObject)
                    continue;
                Console.WriteLine("正在生成表：" + tb.Name);
                sw.WriteLine("PRINT '正在生成表：" + tb.Name + "'");
                sw.WriteLine("GO");
                var scripts = tb.Script(new ScriptingOptions()
                {
                    DriDefaults = true
                });
                foreach (var a in scripts)
                {
                    sw.WriteLine(a);
                    sw.WriteLine("GO");
                }

                foreach (Index index in tb.Indexes)
                {
                    foreach (var str in index.Script())
                    {
                        sw.WriteLine(str);
                        sw.WriteLine("GO");
                    }
                }

                foreach (ForeignKey fk in tb.ForeignKeys)
                {
                    foreach (var str in fk.Script())
                    {
                        sw.WriteLine(str);
                        sw.WriteLine("GO");
                    }
                }

                foreach (ExtendedProperty ep in tb.ExtendedProperties)
                {
                    foreach (var str in ep.Script())
                    {
                        sw.WriteLine(str);
                        sw.WriteLine("GO");
                    }
                }

                foreach (Column col in tb.Columns)
                {
                    foreach (ExtendedProperty ep in col.ExtendedProperties)
                    {
                        foreach (var ext in ep.Script())
                        {
                            sw.WriteLine(ext);
                            sw.WriteLine("GO");
                        }
                    }
                }

                tableCount++;
            }
            Console.WriteLine("表导出完成，共{0}个", tableCount);

            Console.WriteLine("正在生成视图...");
            foreach (View tb in db.Views)
            {
                if (tb.IsSystemObject)
                    continue;
                Console.WriteLine("正在生成视图：" + tb.Name);
                sw.WriteLine("PRINT '正在生成视图：" + tb.Name + "'");
                sw.WriteLine("GO");
                var scripts = tb.Script();
                foreach (var a in scripts)
                {
                    sw.WriteLine(a);
                    sw.WriteLine("GO");
                }

                viewCount++;
            }
            Console.WriteLine("视图导出完成，共{0}个", viewCount);

            Console.WriteLine("正在生成存储过程...");
            foreach (StoredProcedure tb in db.StoredProcedures)
            {
                if (tb.IsSystemObject)
                    continue;
                Console.WriteLine("正在生成存储过程：" + tb.Name);
                sw.WriteLine("PRINT '正在生成存储过程：" + tb.Name + "'");
                sw.WriteLine("GO");
                var scripts = tb.Script();
                foreach (var a in scripts)
                {
                    sw.WriteLine(a);
                    sw.WriteLine("GO");
                }

                procCount++;
            }
            Console.WriteLine("存储过程导出完成，共{0}个", procCount);

            Console.WriteLine("正在生成自定义函数...");
            foreach (UserDefinedFunction tb in db.UserDefinedFunctions)
            {
                if (tb.IsSystemObject)
                    continue;
                Console.WriteLine("正在生成用户函数：" + tb.Name);
                sw.WriteLine("PRINT '正在生成用户函数：" + tb.Name + "'");
                sw.WriteLine("GO");
                var scripts = tb.Script();
                foreach (var a in scripts)
                {
                    sw.WriteLine(a);
                    sw.WriteLine("GO");
                }

                funcCount++;
            }
            Console.WriteLine("自定义函数导出完成，共{0}个", funcCount);
        }

        static void CheckAgrument(string arg, string message)
        {
            if (string.IsNullOrWhiteSpace(arg))
                throw new Exception(message);
        }

        static void ExportData(string dbName, CommandArgument ca, StreamWriter sw)
        {
            Console.WriteLine("正在导出基础数据...");
            sw.WriteLine("use " + dbName);
            sw.WriteLine("SET NOCOUNT ON;");
            sw.WriteLine("SET XACT_ABORT ON;");
            sw.WriteLine("GO");
            using (SqlConnection conn = new SqlConnection(string.Format("server={0};database={1};uid={2};pwd={3}", ca.Server, ca.DataBase, ca.UserName, ca.Password)))
            {
                foreach (string table in ca.BaseTables)
                {
                    Console.Write("正在导出表：" + table + "...");
                    int count = ExportTable(conn, sw, table);
                    Console.WriteLine("共{0}条记录", count);
                }
            }
        }

        static int ExportTable(SqlConnection conn, StreamWriter sw, string tableName)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            var sql = "select * from [" + tableName + "]";
            SqlCommand cmd = new SqlCommand(sql + " where 1=2", conn);
            List<string> columns = new List<string>();
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            sda.Fill(dt);
            sda.Dispose();
            cmd.Dispose();
            foreach (DataColumn col in dt.Columns)
            {
                columns.Add("[" + col.ColumnName + "]");
            }
            return ExportTableData(sql, columns, tableName, conn, sw);
        }

        static int ExportTableData(string sql, List<string> cols, string tableName, SqlConnection conn, StreamWriter sw)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            SqlCommand cmd = new SqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            List<string> select = new List<string>();
            int batch = 1;
            int dataCount = 0;
            while (reader.Read())
            {
                dataCount++;
                List<string> colData = new List<string>();
                foreach (var col in cols)
                {
                    var v = reader[col.Replace("[", "").Replace("]", "")];
                    if (DBNull.Value.Equals(v) || v == null)
                    {
                        colData.Add("NULL");
                    }
                    else
                    {
                        colData.Add("N'" + v.ToString() + "'");
                    }
                }
                select.Add("SELECT " + string.Join(", ", colData) + " ");
                if (select.Count >= 20)
                {
                    WriteInsertBacth(tableName, cols, batch, select, sw);
                    batch++;
                    select.Clear();
                }
            }
            if (select.Count > 0)
            {
                WriteInsertBacth(tableName, cols, batch, select, sw);
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
            return dataCount;
        }

        static void WriteInsertBacth(string tableName, List<string> cols, int batch, List<string> select, StreamWriter sw)
        {
            sw.WriteLine("BEGIN TRANSACTION;");
            sw.WriteLine("INSERT INTO [dbo].[" + tableName + "](" + string.Join(",", cols) + ")");
            sw.WriteLine(string.Join("UNION ALL\n", select));
            sw.WriteLine("COMMIT;");
            sw.WriteLine("RAISERROR (N'[dbo].[" + tableName + "]: Insert Batch: " + batch + ".....Done!', 10, 1) WITH NOWAIT;");
            sw.WriteLine("GO");
        }
    }
}
