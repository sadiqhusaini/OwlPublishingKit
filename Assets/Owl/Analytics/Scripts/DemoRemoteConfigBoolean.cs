
using UnityEngine;

public class DemoRemoteConfigBoolean : MonoBehaviour
{
    public UnityEngine.UI.Text outputText;
    public UnityEngine.UI.InputField input;


    public void GetBoolean()
    {
#if USE_FIREBASEREMOTECONFIG

            outputText.text="Value: "+Owl.Analytics.FirebaseController.GetBoolean(input.text).ToString();
#endif

        
    }
}