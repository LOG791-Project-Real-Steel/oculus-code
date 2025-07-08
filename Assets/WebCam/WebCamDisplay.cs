using UnityEngine;

public class WebcamDisplay : MonoBehaviour
{
    private WebCamTexture webcamTexture;

    void Start()
    {
        // Get the default webcam
        webcamTexture = new WebCamTexture();

        // Apply it to the material of the object this script is attached to
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webcamTexture;

        // Start the webcam
        webcamTexture.Play();
    }

    void OnDestroy()
    {
        // Stop the webcam when the app closes or the object is destroyed
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}