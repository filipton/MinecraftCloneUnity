using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
	[Header("Saves Menu")]
	public GameObject SavesParent;
	public GameObject SaveObject;

	// Start is called before the first frame update
	void Start()
    {
        DiscordController.ChangeDetails("In Main Menu", false);
        DiscordController.ChangeLargeImage("main", "MinecraftClone", false);
        DiscordController.ChangeTimeStamps(DateTimeOffset.Now.ToUnixTimeSeconds(), 0);

		GetAllWorlds();
    }

	public void GetAllWorlds()
	{
		foreach (string world in Directory.GetDirectories(SaveManager.singleton.SavesPath))
		{
			string name = Path.GetFileName(world);

			GameObject so = Instantiate(SaveObject, SavesParent.transform);
			so.name = name;
			so.GetComponentInChildren<Text>().text = name;
			so.GetComponent<Button>().onClick.AddListener(delegate { GoToSave(name); });
		}
	}

	public void GoToSave(string sName)
	{
		SaveManager.singleton.CurrentSave = sName;
		SaveManager.singleton.LoadWorld();

		SceneManager.LoadScene("GameScene");
	}

	private void Update()
	{
		
	}

	public void LoadWorld(InputField inpF)
	{
		SaveManager.singleton.CurrentSave = inpF.text;
		SaveManager.singleton.LoadWorld();

		SceneManager.LoadScene("GameScene");
	}
}