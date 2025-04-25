using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FieldOfViewController : MonoBehaviour
{
    [SerializeField]private Text scoreText;
    private Slider CurrentSlider;
    //public static float Save=90;
    // Start is called before the first frame update
    void Start()
    {
        CurrentSlider = GetComponent<Slider>();

        CurrentSlider.value = MyGameManeger.instance.FieldOfView;
    }

    public void Method()
    {
        FieldOfViewSetter.MyCamera.fieldOfView = CurrentSlider.value ;
        scoreText.text = CurrentSlider.value.ToString();
        MyGameManeger.instance.SettingChanged = true;
        MyGameManeger.instance.FieldOfView = CurrentSlider.value;
    }
}
