using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
    {
        DiscordController.ChangeDetails("In Main Menu", false);
        DiscordController.ChangeLargeImage("main", "MinecraftClone", false);
        DiscordController.ChangeTimeStamps(DateTimeOffset.Now.ToUnixTimeSeconds(), 0);
    }

	private void Update()
	{
		
	}

	public void ToGameScene()
	{
        SceneManager.LoadScene("GameScene");
    }
}