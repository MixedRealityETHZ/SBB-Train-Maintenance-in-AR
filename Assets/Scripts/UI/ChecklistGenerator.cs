#region

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace UI
{
	/// <summary>
	/// Populates a checklist GameObject with the appropriate list items based on the scanned door label
	/// </summary>
	public class ChecklistGenerator : MonoBehaviour
	{
		public GameObject checkListItemPrefab;
		public string currentDoor;
		public List<Checklist> checklists;

		/// <summary>
		/// Set the door number we're currently working on. This should come from OCR recognition of the door label.
		/// </summary>
		/// <param name="doorName"></param>
		public void SetDoor(string doorName)
		{
			// Nothing to do if no such checklist
			if (!checklists.Exists(c => c.plaqueNr == doorName)) return;
			
			var checklist = checklists.Find(c => c.plaqueNr == doorName);

			currentDoor = doorName;

			var verticalLayout = transform.Find("HorizontalLayout/ListVerticalLayout");
			var horizontalLayout = transform.Find("HorizontalLayout");

			// Remove all existing items
			foreach (Transform child in verticalLayout)
				if (child.name != "Title" && child.name != "ManipulationBar")
					Destroy(child.gameObject);

			// Add new items
			foreach (var item in checklist.listItems)
			{
				var newEntry = Instantiate(checkListItemPrefab, verticalLayout);
				var textObject = newEntry.transform.Find("CheckListText").gameObject;
				textObject.GetComponent<TextMeshProUGUI>().SetText(item);
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(verticalLayout.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(horizontalLayout.GetComponent<RectTransform>());
		}

		[Serializable]
		public struct Checklist
		{
			public string plaqueNr;
			public List<string> listItems;
		}
	}
}