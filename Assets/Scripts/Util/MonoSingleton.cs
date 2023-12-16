using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T: MonoSingleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<T>();
                if(instance == null)
                {
                    var obj = new GameObject();
                    instance = obj.AddComponent<T>();
                    obj.name = instance.GetType().Name;
                }
                DontDestroyOnLoad(instance);
                instance.Init();
            }

            return instance;
        }
    }

    /// <summary>
    /// 싱글턴이 생성된 후에 호출될 초기화 함수
    /// </summary>
    public virtual void Init()
    {

    }
}