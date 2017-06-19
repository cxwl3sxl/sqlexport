using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlExport
{
    class CommandArgument
    {
        public CommandArgument(string[] args)
        {
            BaseTables = new List<string>();
            if (args == null)
                throw new Exception("args不能为NULL");
            if (args.Length == 0)
                throw new Exception("没有输入任何参数");
            if (args.Length % 2 != 0)
                throw new Exception("参数个数必须为双数");
            for (var i = 0; i < args.Length; i += 2)
            {
                switch (args[i])
                {
                    case "-s":
                        Server = args[i + 1];
                        break;
                    case "-u":
                        UserName = args[i + 1];
                        break;
                    case "-p":
                        Password = args[i + 1];
                        break;
                    case "-d":
                        DataBase = args[i + 1];
                        break;
                    case "-t":
                        BaseTables.AddRange(args[i + 1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "-o":
                        Output = args[i + 1];
                        break;
                    case "-n":
                        NewDataBase = args[i + 1];
                        break;
                }
            }
        }

        public string Server { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string DataBase { get; private set; }
        public string NewDataBase { get; private set; }
        public string Output { get; private set; }
        public List<string> BaseTables { get; private set; }

        public static string ShowHelp()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("用法：");
            sb.AppendLine("DBSetup.exe -s localhost -u sa -p 123 -d TEMP -t T1,T2,T3 -o abc -n dbName");
            sb.AppendLine("\t-s：数据库服务器地址");
            sb.AppendLine("\t-u：登录账号");
            sb.AppendLine("\t-p：登录密码");
            sb.AppendLine("\t-d：数据库名称");
            sb.AppendLine("\t-t：需要导出基础数据的表名称清单，多个采用英文逗号分隔");
            sb.AppendLine("\t-o：输出文件夹名称");
            sb.AppendLine("\t-n：新的数据库名称，如果未设置则采用-d指定的名称");
            sb.AppendLine("如果要开启DEBUG模式，可以直接在程序名称中包含debug字样即可。");
            return sb.ToString();
        }
    }
}
