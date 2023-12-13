using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class Checklist : MonoBehaviour
{
    public GameObject checkListItemPrefab;
    public string currentDoor;
    private Hashtable checkListTable;

    void Start()
    {
        checkListTable = new Hashtable
       {
           { "MT22", new List<string> { "MT22" } },
           { "MT23", new List<string> { "MT23" } }
       };
    }

    public void SetDoor(string doorName)
    {
        if (checkListTable.ContainsKey(doorName))
        {
            currentDoor = doorName;

            var verticalLayout = transform.Find("HorizontalLayout/ListVerticalLayout");
            var horizontalLayout = transform.Find("HorizontalLayout");

            // Remove all existing items
            foreach (Transform child in verticalLayout) if (child.name != "Title" && child.name != "ManipulationBar")
                {
                    Destroy(child.gameObject);
                }

            // Add new items
            foreach (string item in checkListTable[currentDoor] as List<string>)
            {
                var newEntry = Instantiate(checkListItemPrefab, verticalLayout);
                var textObject = newEntry.transform.Find("CheckListText").gameObject;
                textObject.GetComponent<TextMeshProUGUI>().SetText(item);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(verticalLayout.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(horizontalLayout.GetComponent<RectTransform>());
        }
    }
}
