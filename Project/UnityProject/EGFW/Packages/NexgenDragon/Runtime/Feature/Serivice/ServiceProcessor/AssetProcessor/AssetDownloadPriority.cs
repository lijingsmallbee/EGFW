namespace NexgenDragon
{
    /// <summary>
    /// 资源下载优先级
    /// </summary>
    public readonly struct AssetDownloadPriority
    {
        /// <summary>
        /// 最高
        /// 用于插队 立即尽快下载
        /// 比如资源创建时发现需要下载的 用HIGH能尽快更新到
        /// </summary>
        public const int HIGH = 999;
        /// <summary>
        /// 普通
        /// 正常排进队列
        /// 默认值
        /// </summary>
        public const int NORMAL = 0;
        /// <summary>
        /// 低
        /// 可以随时被其他请求插队
        /// 用于后台静默下载
        /// </summary>
        public const int LOW = -1;
    }
}