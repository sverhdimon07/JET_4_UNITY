using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        // Проверяем, запущен ли билд с флагом -batchmode (без графики)
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Headless mode detected. Starting Dedicated Server...");
            // Запускаем сервер через синглтон FishNet
            InstanceFinder.ServerManager.StartConnection();
        }
    }
}