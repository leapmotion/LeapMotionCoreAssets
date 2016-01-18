using System;
using System.Reflection;


namespace LeapInternal{
    public static class Logger
    {
        //
        // Summary:
        //     Logs message to the a Console.
        public static void Log(object message)
        {
            #if DEBUG
                #if UNITY_EDITOR
                    UnityEngine.Debug.Log(message);
                #else
                    Console.WriteLine(message);
                #endif
            #endif
        }

        public static void LogObject(object thisObject)
        {
            try
            {
                MemberInfo[] memberInfos;
                memberInfos = thisObject.GetType().GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic // Get public and non-public
                    | BindingFlags.Static | BindingFlags.Instance  // Get instance + static
                    | BindingFlags.FlattenHierarchy); // Search up the hierarchy
                // sort members by name
                Array.Sort(memberInfos,
                           (memberInfo1, memberInfo2) => memberInfo1.Name.CompareTo(memberInfo2.Name));
                
                // write member names
                foreach (MemberInfo memberInfo in memberInfos)
                {
                    if(memberInfo.MemberType == MemberTypes.Property){
                        string value = thisObject.GetType().GetProperty(memberInfo.Name).GetValue(thisObject, null).ToString();
                        Logger.Log("Type: " + memberInfo.MemberType + ", Name: " + memberInfo.Name + ", Value = " + value);
                    } else {
                        Logger.Log("Type: " + memberInfo.MemberType + ", Name: " + memberInfo.Name);
                    }
                   
                }
            }
            catch (Exception exception)
            {
                Logger.Log (exception.Message);
            }
            
        }
        public static void LogStruct(object thisObject, string title = "")
        {
            try
            {
                if(!thisObject.GetType().IsValueType){
                    Logger.Log (title + " ---- Trying to log non-struct with struct logger");
                    return;
                }
                Logger.Log (title + " ---- " + thisObject.GetType().ToString());
                FieldInfo[] fieldInfos;
                fieldInfos = thisObject.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic // Get public and non-public
                    | BindingFlags.Static | BindingFlags.Instance  // Get instance + static
                    | BindingFlags.FlattenHierarchy); // Search up the hierarchy

                // write member names
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    string value = fieldInfo.GetValue(thisObject).ToString();
                    Logger.Log(" -------- Name: " + fieldInfo.Name + ", Value = " + value);
                }
            }
            catch (Exception exception)
            {
                Logger.Log (exception.Message);
            }
            
        }
    }
}
