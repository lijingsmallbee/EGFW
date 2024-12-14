using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;
public class LoadExistLuaFail : IEvent
{
    private string _luaName;
    public const string NAME = "LOAD_EXIST_LUA_FAIL";
    public LoadExistLuaFail(string LuaName)
    {
        _luaName = LuaName;
    }
    public string GetEventType()
    {
        return NAME;
    }

    public string ErrorLuaName
    {
        get { return _luaName; }
    }
}
