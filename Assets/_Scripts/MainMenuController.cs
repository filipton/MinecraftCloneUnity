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
        DiscordRpc.EventHandlers handlers = new DiscordRpc.EventHandlers();
        //DiscordRpc.Initialize("829656858402619392", ref handlers, true, "");
        DiscordRpc.UpdatePresence(new DiscordRpc.RichPresence()
        {
            state = "AMOGUSSS",
            details = "In Main Menu",
            largeImageKey = "amogus",
            largeImageText = "Tak o",
            startTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            joinSecret = "dsgfdh78ewbirewgf34789123",
            matchSecret = "h943hf394ohf895gh4965223",
            partyId = "fdh439fh890h84c3f=3-54fg43f43f43h9f34f43fgh43u9hrf97824vg798r421",
            partyMax = 420,
            partySize = 69
        });
    }

	private void Update()
	{
		
	}

	public void ToGameScene()
	{
        SceneManager.LoadScene("GameScene");
    }

    void OnDisable()
    {
        Debug.Log("Discord: shutdown");
        DiscordRpc.Shutdown();
    }
}