using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using NexgenDragon;

namespace FSM
{
	enum ParamType
	{
		PT_none,
		PT_char,
		PT_int,
		PT_double,
		PT_vec2,
		PT_vec3,
		PT_bool,
		PT_float,
        PT_string,
		PT_object,
	}
	[StructLayout(LayoutKind.Explicit)]
	struct ParamValue
	{
		[FieldOffset(0)]
		public char _cval;

		[FieldOffset(0)]
		public int _ival;

		[FieldOffset(0)]
		public double _dval;

		[FieldOffset(0)]
		public Vector2 _vec2val;

		[FieldOffset(0)]
		public Vector3 _vec3val;

		[FieldOffset(0)]
		public bool _bval;

		[FieldOffset(0)]
		public float _fval;
	}

	public class StateParam
	{
		ParamType ptType;
		ParamValue Value;
        string strValue;
		object objectValue;
		private bool isTrigger;

		public void SetTrigger(bool trigger)
		{
			isTrigger = trigger;
		}

		public bool IsTrigger => isTrigger;

		public void ResetValue()
		{
			Value._bval = false;
			Value._cval = (char)0;
			Value._dval = 0f;
			Value._fval = 0f;
			Value._ival = 0;
			Value._vec2val = Vector2.zero;
			Value._vec3val = Vector3.zero;
			strValue = string.Empty;
			objectValue = null;
		}

		public StateParam()
		{
			ptType = ParamType.PT_none;
		}
		public StateParam(object oValue)
		{
			ptType = ParamType.PT_object;
			objectValue = oValue;
		}

		public StateParam(char cValue)
		{
			ptType = ParamType.PT_char;
			Value._cval = cValue;
		}

		public StateParam(int iValue)
		{
			ptType = ParamType.PT_int;
			Value._ival = iValue;
		}

		public StateParam(double dValue)
		{
			ptType = ParamType.PT_double;
			Value._dval = dValue;
		}

		public StateParam(Vector2 vValue)
		{
			ptType = ParamType.PT_vec2;
			Value._vec2val = vValue;
		}

		public StateParam(Vector3 vValue)
		{
			ptType = ParamType.PT_vec3;
			Value._vec3val = vValue;
		}

		public StateParam(bool bValue)
		{
			ptType = ParamType.PT_bool;
			Value._bval = bValue;
		}

		public StateParam(float fValue)
		{
			ptType = ParamType.PT_float;
			Value._fval = fValue;
		}

        public StateParam(string sValue)
        {
            ptType = ParamType.PT_string;
            strValue = sValue;
        }

		public void SetValue(char value)
		{
			if(ptType == ParamType.PT_char)
			{
				Value._cval = value;
			}
		}

		public void SetValue(int value)
		{
			if(ptType == ParamType.PT_int)
			{
				Value._ival = value;
			}
		}

		public void SetValue(double value)
		{
			if(ptType == ParamType.PT_double)
			{
				Value._dval = value;
			}
		}

		public void SetValue(Vector2 value)
		{
			if(ptType == ParamType.PT_vec2)
			{
				Value._vec2val = value;
			}
		}

		public void SetValue(Vector3 value)
		{
			if(ptType == ParamType.PT_vec3)
			{
				Value._vec3val = value;
			}
		}

		public void SetValue(float value)
		{
			if(ptType == ParamType.PT_float)
			{
				Value._fval = value;
			}
		}

		public void SetValue(bool value)
		{
			if(ptType == ParamType.PT_bool)
			{
				Value._bval = value;
			}
		}

		public void SetValue(object value)
		{
			if (ptType == ParamType.PT_object)
			{
				objectValue = value;
			}
		}

        public void SetValue(string value)
        {
            if (ptType == ParamType.PT_string)
            {
                strValue = value;
            }
        }
		public void GetValue(out char value)
		{
			value = Value._cval;
		}

		public void GetValue(out int value)
		{
			value = Value._ival;
		}

		public void GetValue(out Vector2 value)
		{
			value = Value._vec2val;
		}

		public void GetValue(out Vector3 value)
		{
			value = Value._vec3val;
		}

		public void GetValue(out float value)
		{
			value = Value._fval;
		}

		public void GetDouble(out double value)
		{
			value = Value._dval;
		}
		public void GetValue(out bool value)
		{
			value = Value._bval;
		}

        public void GetValue(out string value)
        {
            value = strValue;
        }

		public void GetValue(out object value)
		{
			value = objectValue;
		}

		public int CompareTo(StateParam stateParam)
		{
			if (ptType == ParamType.PT_bool)
			{
				return Value._bval.CompareTo(stateParam.Value._bval);
			}

			if (ptType == ParamType.PT_char)
			{
				return Value._cval.CompareTo(stateParam.Value._cval);
			}

			if (ptType == ParamType.PT_double)
			{
				return Value._dval.CompareTo(stateParam.Value._dval);
			}

			if (ptType == ParamType.PT_float)
			{
				return Value._fval.CompareTo(stateParam.Value._fval);
			}

			if (ptType == ParamType.PT_int)
			{
				return Value._ival.CompareTo(stateParam.Value._ival);
			}

			if (ptType == ParamType.PT_string)
			{
				return strValue.CompareTo(stateParam.strValue);
			}

			NLogger.Error("type {0} compare is not support",ptType.ToString());
			return 0;
		}
	}

    public class StateParamDic
    {
        private Dictionary<string, StateParam> m_StateParam = new Dictionary<string, StateParam>();

        public void ResetTrigger()
        {
	        foreach (var param in m_StateParam)
	        {
		        if (param.Value.IsTrigger)
		        {
			        param.Value.ResetValue();
		        }
	        }
        }
		public void Remove(string name)
		{
			m_StateParam.Remove (name);
		}

		public void SetObject(string name, object value)
		{
			StateParam param = null;
			if (m_StateParam.TryGetValue(name, out param))  //find the param
			{
				param.SetValue(value);
			}
			else
			{
				m_StateParam.Add(name, new StateParam(value));
			}
		}

		public void SetTrigger(string name, bool trigger)
		{
			if (m_StateParam.TryGetValue(name, out var param))  //find the param
			{
				param.SetTrigger(trigger);
			}
		}

		public void GetObject(string name, out object value)
		{
			value = string.Empty;
			StateParam param = null;
			if (m_StateParam.TryGetValue(name, out param))  //find the param
			{
				param.GetValue(out value);
			}
		}

        public void SetBool(string name, bool value)
        {
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.SetValue(value);
            }
            else
            {
                m_StateParam.Add(name, new StateParam(value));
            }
        }

        public void GetBool(string name, out bool value)
        {
            value = false;
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.GetValue(out value);
            }
        }

        public void SetVector2(string name, Vector2 value)
        {
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.SetValue(value);
            }
            else
            {
                m_StateParam.Add(name, new StateParam(value));
            }
        }

        public void GetVector2(string name, out Vector2 value)
        {
            value = Vector2.zero;
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.GetValue(out value);
            }
        }

        public void SetVector3(string name, Vector3 value)
        {
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.SetValue(value);
            }
            else
            {
                m_StateParam.Add(name, new StateParam(value));
            }
        }

        public void GetVector3(string name, out Vector3 value)
        {
            value = Vector3.zero;
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.GetValue(out value);
            }
        }

        public void SetInt(string name, int value)
        {
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.SetValue(value);
            }
            else
            {
                m_StateParam.Add(name, new StateParam(value));
            }
        }

        public void GetInt(string name, out int value)
        {
            value = -1;
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.GetValue(out value);
            }
        }

        public void SetFloat(string name, float value)
        {
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.SetValue(value);
            }
            else
            {
                m_StateParam.Add(name, new StateParam(value));
            }
        }

        public void GetFloat(string name, out float value)
        {
            value = 0f;
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.GetValue(out value);
            }
        }

        public void SetString(string name, string value)
        {
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.SetValue(value);
            }
            else
            {
                m_StateParam.Add(name, new StateParam(value));
            }
        }

        public void GetString(string name, out string value)
        {
            value = string.Empty;
            StateParam param = null;
            if (m_StateParam.TryGetValue(name, out param))  //find the param
            {
                param.GetValue(out value);
            }
        }

        public StateParam GetByName(string name)
        {
	        m_StateParam.TryGetValue(name, out var param);
	        return param;
        }
    }
}
