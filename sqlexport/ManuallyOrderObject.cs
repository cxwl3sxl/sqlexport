namespace SqlExport
{
    /// <summary>
    /// 需要手动控制生成顺序的对象
    /// </summary>
    class ManuallyOrderObject
    {
        public ManuallyOrderObject(string name, int order, string script)
        {
            Order = order;
            Script = script;
            Name = name;
        }
        /// <summary>
        /// 对象名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 目标对象的顺序
        /// </summary>
        public int Order { get; }
        /// <summary>
        /// 脚本
        /// </summary>
        public string Script { get; }
    }
}
