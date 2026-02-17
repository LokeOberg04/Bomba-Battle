using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUIScript : MonoBehaviour
{

    public Canvas canvas;
    public Button hostButton;
    public Button clientButton;
    public GameObject controls;
    public Button controlsButton;
    public Button controlsX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hostButton.onClick.AddListener(startHost);
        clientButton.onClick.AddListener(startClient);
        controlsButton.onClick.AddListener(toggleControls);
        controlsX.onClick.AddListener(toggleControls);
    }

    private void toggleControls()
    {
        controls.SetActive(!controls.activeSelf);
    }

    private void startHost()
    {
        NetworkManager.Singleton.StartHost();
        canvas.enabled = false;
    }

    private void startClient()
    {
        NetworkManager.Singleton.StartClient();
        canvas.enabled = false;
    }



}
