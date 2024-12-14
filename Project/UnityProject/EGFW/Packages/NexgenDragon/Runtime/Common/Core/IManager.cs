namespace NexgenDragon
{
    /// <summary>
    /// IManager
    /// 所有管理器需要实现的接口
    /// TODO:希望通过Attribute来约束Manager之间的依赖关系（声明需要依赖的Manager及默认初始化参数，如果没有该Manager生成则创建并注册）
    /// </summary>
	public interface IManager : IObject
	{
		void Initialize(NexgenObject configParam);
		void Reset();
	}
}
