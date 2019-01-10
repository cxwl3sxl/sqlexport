using System;

namespace SqlExport
{
    static class Extendtions
    {
        /// <summary>
        /// 在指定的字符串数组中寻找指定的原始，如果找到则返回索引位置，否则返回-1
        /// </summary>
        /// <param name="stringArray"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int IndexOf(this string[] stringArray, string target)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                if (stringArray[i].Equals(target, StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }
    }
}
