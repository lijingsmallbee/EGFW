using NexgenDragon;

namespace FSM
{
	public class ParamCondition : BaseCondition
	{
		public void SetCompareParam(StateParam param)
		{
			_paramToCompare = param;
		}
		public ParamCondition(string name,int result)
		{
			this.name = name;
			this.result = result;
		}
		protected StateParam _paramToCompare;
		protected int result = 0;
		protected string name;


		protected StateParam _cacheParam;
		public override bool IsSatisfied()
		{
			if (_cacheParam == null)
			{
				_cacheParam = _sm.StateParams.GetByName(name);
			}

			if (_cacheParam != null && _paramToCompare != null)
			{
				return result == _cacheParam.CompareTo(_paramToCompare);
			}
			return false;
		}
	}
//下边这些类用lua来显示构造，防止参数重载问题
	public class BoolCondition:ParamCondition
	{
		public BoolCondition(string name,bool value,int result = 0):base(name,result)
		{
			_paramToCompare = new StateParam(value);
		}
	}
    
	public class StringCondition:ParamCondition
	{
		public StringCondition(string name,string value,int result = 0):base(name,result)
		{
			_paramToCompare = new StateParam(value);
		}
	}
    
	public class FloatCondition:ParamCondition
	{
		public FloatCondition(string name,float value,int result = 0):base(name,result)
		{
			_paramToCompare = new StateParam(value);
		}
	}
    
	public class IntCondition:ParamCondition
	{
		public IntCondition(string name,int value,int result = 0):base(name,result)
		{
			_paramToCompare = new StateParam(value);
		}
	}

	public class DoubleCondition:ParamCondition
	{
		public DoubleCondition(string name,double value,int result = 0):base(name,result)
		{
			_paramToCompare = new StateParam(value);
		}
	}
}