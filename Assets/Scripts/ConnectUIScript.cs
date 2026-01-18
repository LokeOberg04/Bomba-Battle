using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUIScript : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hostButton.onClick.AddListener(startHost);
        clientButton.onClick.AddListener(startClient);
    }

    private void startHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void startClient()
    {
        NetworkManager.Singleton.StartClient();
    }

}
