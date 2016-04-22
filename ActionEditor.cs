using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

[CustomPropertyDrawer(typeof(Action))]
public class ActionEditor : PropertyDrawer  
{
    private bool open = true;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

//----zones---------------------------------
        Rect titleRect = new Rect(position.x, position.y, position.width, 17);
        Rect foldoutRect = new Rect(position.x + 16, position.y + 1, position.width - 12, 12);
        Rect regionRect = new Rect(position.x + 1, position.y + 17, position.width - 2, position.height - 20);
        
        Rect delButtonRect = new Rect(position.x + 6, position.y + 24, 12, 12);
        Rect delLabelRect = new Rect(position.x + 7, position.y + 22, 14, 14);
        Rect targetRect = new Rect(position.x + 20, position.y + 22, position.width - 26, 16);
        Rect methodRect = new Rect(position.x + 20, targetRect.y + 20, position.width - 26, 16);
        Rect splitterRect = new Rect(position.x + 6, methodRect.y + 6, position.width - 12, 16);
//----------------------------------------

        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.Toggle(titleRect, true, "dragtab");
        EditorGUI.EndDisabledGroup();

        open = EditorGUI.Toggle(titleRect, open, "dragtab");

        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontStyle = FontStyle.Bold;
        foldoutStyle.fontSize = 11;

        EditorGUI.Foldout(foldoutRect, open, label, foldoutStyle);
        
        if (!open)
        {
            EditorGUI.EndProperty();
            return;
        }

        // background
        EditorGUI.LabelField(regionRect, "", EditorStyles.textArea);
        
        List<MonoBehaviour> targets = Deserialize<MonoBehaviour>(property.FindPropertyRelative("targets"));
        List<string> methods = Deserialize<string>(property.FindPropertyRelative("methods"));
        
        MonoBehaviour monoBehTarget;

        for (int i = 0; i < targets.Count; i++)
        {
            if (GUI.Button(delButtonRect, ""))
            {
                targets.RemoveAt(i);
                methods.RemoveAt(i);
                Serialize<MonoBehaviour>(targets, property.FindPropertyRelative("targets"));
                Serialize<string>(methods, property.FindPropertyRelative("methods"));
                return;
            }
            EditorGUI.LabelField(delLabelRect, "-", EditorStyles.boldLabel);

            monoBehTarget = (MonoBehaviour)EditorGUI.ObjectField(targetRect, "Target Object", targets[i], typeof(MonoBehaviour), true);

            if (monoBehTarget != targets[i])
            {
                targets[i] = monoBehTarget;
                methods[i] = Action.METHOD_NONE;

                Serialize<MonoBehaviour>(targets, property.FindPropertyRelative("targets"));
                Serialize<string>(methods, property.FindPropertyRelative("methods"));
                return;
            }

            List<string> methodsList = GetMethodList(targets[i]);
            int selectedIndex = methodsList.FindIndex(x => x.StartsWith(methods[i]));

            int index = EditorGUI.Popup(methodRect, "Method Name:", selectedIndex, methodsList.ToArray() /*new string[] { "a/Rigidbody", "a/Box Collider", "b/Sphere Collider" }*/);

            if (index != selectedIndex)
            {
                methods[i] = methodsList[index];
                Serialize<string>(methods, property.FindPropertyRelative("methods"));
            }
            
            // Splitter
            EditorGUI.LabelField(splitterRect, "____________________________________________________________________________" +
            "_____________________________________________________________________________________________");


            delButtonRect = new Rect(position.x + 6, splitterRect.y + 21, 12, 12);
            delLabelRect = new Rect(position.x + 7, splitterRect.y + 19, 14, 14);
            targetRect = new Rect(position.x + 20, splitterRect.y + 19, position.width - 26, 16);
            methodRect = new Rect(position.x + 20, targetRect.y + 20, position.width - 26, 16);
            splitterRect = new Rect(position.x + 6, methodRect.y + 6, position.width - 12, 16);
        }

        monoBehTarget = (MonoBehaviour)EditorGUI.ObjectField(targetRect, "Target Object", null, typeof(MonoBehaviour), true);
        EditorGUI.Popup(methodRect, "Method Name:", 0, new string[] { Action.METHOD_NONE });

        if (monoBehTarget != null)
        {
            targets.Add(monoBehTarget);
            methods.Add(Action.METHOD_NONE);
            
            Serialize<MonoBehaviour>(targets, property.FindPropertyRelative("targets"));
            Serialize<string>(methods, property.FindPropertyRelative("methods"));
        }

        EditorGUI.EndProperty();
    }

    //override this function to add space below the new property drawer
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float extraHeight = 0;
        if(open)
            extraHeight = 6 + 45 * (Deserialize<MonoBehaviour>(property.FindPropertyRelative("targets")).Count + 1);
        return base.GetPropertyHeight(property, label) + extraHeight;
    }

    private List<string> GetMethodList(MonoBehaviour monoBeh)
    {
        List<string> result = new List<string>();
        result.Add(Action.METHOD_NONE);

        if (monoBeh == null)
            return result;

        GameObject go = monoBeh.gameObject;
        MonoBehaviour[] monoBehList = go.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour beh in monoBehList)
        {
            MethodInfo[] methods = beh.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (MethodInfo methodInfo in methods)
            {
                // Check return type void.
                if (methodInfo.ReturnType != typeof(void))
                    continue;

                // Check default method.
                if (isDefaultMethod(methodInfo.Name) == true)
                    continue;

                ParameterInfo[] pars = methodInfo.GetParameters();
                if (pars.Length > 0)
                    continue;

                result.Add(beh.GetType().ToString() + "/" + methodInfo.Name);
            }
        }
        return result;
    }

    private void Serialize<T>(List<T> targets, SerializedProperty sp)
    {
        if (!sp.isArray)
            return;

        sp.ClearArray();

        sp.Next(true); // skip generic field
        sp.Next(true); // advance to array size field

        sp.intValue = targets.Count;  // Set the array size

        sp.Next(true); // advance to first array index

        // Write values to array
        int lastIndex = targets.Count - 1;

        for (int i = 0; i < targets.Count; i++) 
        {
            if (!SetSerialzedProperty(sp, targets[i])) return; // write the value to the property
            if(i < lastIndex) sp.Next(false); // advance without drilling into children            
        }
    }

    private List<T> Deserialize<T>(SerializedProperty sp)
    {
        if (!sp.isArray)
            return null;

        int arrayLength = 0;

        sp.Next(true); // skip generic field
        sp.Next(true); // advance to array size field

        // Get the array size
        arrayLength = sp.intValue;

        if (arrayLength == 0)
            return new List<T>();

        sp.Next(true); // advance to first array index

        // Write values to list
        List<T> result = new List<T>(arrayLength);
        int lastIndex = arrayLength - 1;
        for (int i = 0; i < arrayLength; i++)
        {
            result.Add((T)GetSerialzedProperty(sp));
            if (i < lastIndex) sp.Next(false); // advance without drilling into children
        }
        return result;
    }

    private object GetSerialzedProperty(SerializedProperty sp)
    {
        SerializedPropertyType type = sp.propertyType; // get the property type
        switch (type)
        {
            case SerializedPropertyType.Integer:
                return sp.intValue;
            case SerializedPropertyType.String:
                return sp.stringValue;
            case SerializedPropertyType.ObjectReference:
                return sp.objectReferenceValue;
        }
        return null;
    }

    private bool SetSerialzedProperty(SerializedProperty sp, object variableValue)
    {
        SerializedPropertyType type = sp.propertyType; // get the property type

        switch (type)
        {
            case SerializedPropertyType.Integer:
                int intValue = (int)variableValue;
                if (sp.intValue != intValue)
                    sp.intValue = intValue;
                break;
            case SerializedPropertyType.String:
                string strValue = (string)variableValue;
                if (sp.stringValue != strValue)
                    sp.stringValue = strValue;
                break;
            case SerializedPropertyType.ObjectReference:
                Object objValue = (Object)variableValue;
                if (sp.objectReferenceValue != objValue)
                    sp.objectReferenceValue = objValue;
                break;
            default:
                return false;
        }
        return true;
    }

    private bool isDefaultMethod(string name)
    {
        if (name == "Invoke") return true;
        if (name == "InvokeRepeating") return true;
        if (name == "CancelInvoke") return true;
        if (name == "StopCoroutine") return true;
        if (name == "StopAllCoroutines") return true;
        if (name == "BroadcastMessage") return true;
        if (name == "GetComponentsInChildren") return true;
        if (name == "GetComponentsInParent") return true;
        if (name == "GetComponents") return true;
        if (name.StartsWith("SendMessage")) return true;
        if (name.StartsWith("set_")) return true;
        return false;
    }    
}
