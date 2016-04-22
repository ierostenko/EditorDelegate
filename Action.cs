using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

[System.Serializable]
public class Action
{
    public const string METHOD_NONE = "None";

    public List<MonoBehaviour> targets;
    public List<string> methods;
    
    public void Execute()
    {
        for (int i = 0; i < targets.Count; i++ )
        {
            if (targets[i] == null)
                continue;

            if(methods.Count > i && methods[i] != METHOD_NONE)
            {
                string[] strArr = methods[i].Split('/');

                MonoBehaviour monoBeh = targets[i];
                Component component = monoBeh.gameObject.GetComponent(strArr[0]);
                
                component.GetType().InvokeMember(strArr[strArr.Length - 1], BindingFlags.Public | BindingFlags.InvokeMethod, null, component, null);
            }
        }
    }
}