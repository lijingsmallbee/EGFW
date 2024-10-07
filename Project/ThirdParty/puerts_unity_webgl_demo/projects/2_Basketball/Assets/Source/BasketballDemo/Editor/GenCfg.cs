using System.Collections.Generic;
using Puerts;
using System;
using UnityEngine;

[Configure]
public class ExamplesCfg
{
    [Binding]
    static IEnumerable<Type> Bindings
    {
        get
        {
            return new List<Type>()
            {
                typeof(JsMonoBehaviour),
                typeof(Type),
                typeof(UnityEngine.Input),
                typeof(UnityEngine.Object),
                typeof(UnityEngine.GameObject),
                typeof(UnityEngine.Transform),
                typeof(UnityEngine.Rigidbody),
                typeof(UnityEngine.Vector3),
                typeof(UnityEngine.BoxCollider),
                typeof(UnityEngine.Collider),
                typeof(UnityEngine.Debug),
                typeof(GameManager),
            };
        }
    }
    
    [Filter]
    static bool FilterMethods(System.Reflection.MemberInfo mb)
    {
        if (mb.Name == "IsJoystickPreconfigured") {
            return true;
        }
        if (mb.Name == "MakeGenericSignatureType" || mb.Name == "IsCollectible") {
            return true;
        }
        return false;
    } 
}