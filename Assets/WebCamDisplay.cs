using UnityEngine;

public class WebCamDisplay : MonoBehaviour
{
    private WebCamTexture webcamTexture;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }
    void OnDisable()
    {
        if (webcamTexture != null) webcamTexture.Stop();
    }
}


