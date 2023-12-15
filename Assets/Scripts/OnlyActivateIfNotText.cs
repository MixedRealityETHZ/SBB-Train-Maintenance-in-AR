using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OnlyActivateIfNotText : MonoBehaviour
{
    private TMP_Text _text;

    private string _initialText;
    // Start is called before the first frame update
    void Start()
    {
        _text = GetComponentInChildren<TMP_Text>();
        _initialText = _text.text;
    }

    public void SetActiveConditional(bool state)
    {
        if (!state)
        {
            gameObject.SetActive(false);
        }

        gameObject.SetActive(_text.text != _initialText);
    }
}
