using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StartScreenManagerController : MonoBehaviour
{
    // ReSharper disable once InconsistentNaming
    private VisualElement root;

    private Label callToAction;
    // Start is called before the first frame update
    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        callToAction = root.Q<Label>("CallToAction");
        Time.timeScale = 1;
        InvokeRepeating(nameof(blinkCallToAction), 0.25f, 0.25f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            SceneManager.LoadScene("Scenes/SampleScene");
        }
    }

    void blinkCallToAction()
    {
        callToAction.visible = !callToAction.visible;
    }
}
