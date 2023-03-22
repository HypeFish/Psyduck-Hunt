using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GUIManager : MonoBehaviour
{

    private GroupBox timerGb;
    private Label time;

    private LevelManager levelManager;

    private GroupBox reportGB;
    private Label titleLabel;
    private Label line1Label;
    private Label line2Label;
    private Button returnButton;
    
    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
        
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        
        timerGb = root.Q<GroupBox>("timer");
        time = root.Q<Label>("time");

        reportGB = root.Q<GroupBox>("report");
        titleLabel = root.Q<Label>("title");
        line1Label = root.Q<Label>("line1");
        line2Label = root.Q<Label>("line2");
        returnButton = root.Q<Button>("return");
        returnButton.clicked += returnButtonPressed;
        
        setDisplay(reportGB, false);
        setDisplay(timerGb, true);
    }

    // Update is called once per frame
    void Update()
    {
        if (getDisplay(timerGb))
        {
            time.text = (levelManager.endTime - levelManager.currentTime).ToString("0");

        }
    }
    
    public void ReportToPlayer(string title, string line1, float timer)
    {
        if (getDisplay(reportGB))
        {
            CancelInvoke(nameof(HideReport));
        }
        setDisplay(timerGb, false);
        setDisplay(reportGB, true);
        titleLabel.text = title;
        line1Label.text = line1;
        titleLabel.visible = true;
        line1Label.visible = true;
        line2Label.visible = false;
        returnButton.visible = false;
        Invoke(nameof(HideReport), timer);
    }
    
    public void ReportToPlayer(string title, string line1, string line2)
    {
        if (getDisplay(reportGB))
        {
            CancelInvoke(nameof(HideReport));
        }
        setDisplay(timerGb, false);
        setDisplay(reportGB, true);

        titleLabel.text = title;
        line1Label.text = line1;
        line2Label.text = line2;

        titleLabel.visible = true;
        line1Label.visible = true;
        line2Label.visible = true;
        returnButton.visible = true;
        returnButton.text = "Return to Start";
    }

    private void HideReport()
    {
        setDisplay(timerGb, true);
        setDisplay(reportGB, false);
    }

    private void returnButtonPressed()
    {
        SceneManager.LoadScene("Scenes/Start");
    }

    private void setDisplay(GroupBox gb, bool display)
    {
        gb.style.display =!display ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private bool getDisplay(GroupBox gb)
    {
        if (gb.style.display == DisplayStyle.Flex)
        {
            return true;
        }

        return false;

    }
}
