using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlExport
{
    class TimeUse : IDisposable
    {
        readonly DateTime begin;
        readonly string actionName;
        public TimeUse(string action)
        {
            begin = DateTime.Now;
            actionName = action;
        }
        public void Dispose()
        {
            Console.WriteLine(string.Format("{0}执行完成，耗时：{1}", actionName, (DateTime.Now - begin).ToString()));
        }
    }
}
