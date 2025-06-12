using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemWatcher : MonoBehaviour
{
    void Update()
    {
        var count = FindObjectsOfType<EventSystem>().Length;
        if (count != 1)
        {
            Debug.LogWarning($"🚨 Found {count} EventSystems in scene!");
        }
    }
}