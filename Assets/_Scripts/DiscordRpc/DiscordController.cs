using System;
using UnityEngine;

[System.Serializable]
public class DiscordJoinEvent : UnityEngine.Events.UnityEvent<string> { }

[System.Serializable]
public class DiscordSpectateEvent : UnityEngine.Events.UnityEvent<string> { }

[System.Serializable]
public class DiscordJoinRequestEvent : UnityEngine.Events.UnityEvent<DiscordRpc.DiscordUser> { }

public class DiscordController : MonoBehaviour
{
    public DiscordRpc.RichPresence presence = new DiscordRpc.RichPresence();
    public string applicationId;
    public DiscordRpc.DiscordUser joinRequest;
    public UnityEngine.Events.UnityEvent onConnect;
    public UnityEngine.Events.UnityEvent onDisconnect;
    public UnityEngine.Events.UnityEvent hasResponded;
    public DiscordJoinEvent onJoin;
    public DiscordJoinRequestEvent onJoinRequest;
    public static DiscordRpc.RichPresence RichPresence;

    DiscordRpc.EventHandlers handlers;

    public void RequestRespondYes()
    {
        Debug.Log("Discord: responding yes to Ask to Join request");
        DiscordRpc.Respond(joinRequest.userId, DiscordRpc.Reply.Yes);
        hasResponded.Invoke();
    }

    public void RequestRespondNo()
    {
        Debug.Log("Discord: responding no to Ask to Join request");
        DiscordRpc.Respond(joinRequest.userId, DiscordRpc.Reply.No);
        hasResponded.Invoke();
    }

    public void ReadyCallback(ref DiscordRpc.DiscordUser connectedUser)
    {
        Debug.Log(string.Format("Discord: connected to {0}#{1}: {2}", connectedUser.username, connectedUser.discriminator, connectedUser.userId));
        onConnect.Invoke();
    }

    public void DisconnectedCallback(int errorCode, string message)
    {
        Debug.Log(string.Format("Discord: disconnect {0}: {1}", errorCode, message));
        onDisconnect.Invoke();
    }

    public void ErrorCallback(int errorCode, string message)
    {
        Debug.Log(string.Format("Discord: error {0}: {1}", errorCode, message));
    }

    public void JoinCallback(string secret)
    {
        Debug.Log(string.Format("Discord: join ({0})", secret));
        onJoin.Invoke(secret);
    }

    public void RequestCallback(ref DiscordRpc.DiscordUser request)
    {
        Debug.Log(string.Format("Discord: join request {0}#{1}: {2}", request.username, request.discriminator, request.userId));
        joinRequest = request;
        onJoinRequest.Invoke(request);
    }

	private void Awake()
	{
        DontDestroyOnLoad(this);

        RichPresence = new DiscordRpc.RichPresence();
        ChangeDetails("In game");
    }

	void Start()
    {
        DiscordRpc.UpdatePresence(RichPresence);
    }

    void Update()
    {
        DiscordRpc.RunCallbacks();
    }

    public static void ChangeState(string state, bool UpdatePresence = true)
	{
        RichPresence.state = state;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
	}

    public static void ChangeDetails(string details, bool UpdatePresence = true)
    {
        RichPresence.details = details;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
    }

    public static void ChangeLargeImage(string key, string text, bool UpdatePresence = true)
    {
        RichPresence.largeImageKey = key;
        RichPresence.largeImageText = text;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
    }

    public static void ChangeSmallImage(string key, string text, bool UpdatePresence = true)
    {
        RichPresence.smallImageKey = key;
        RichPresence.smallImageText = text;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
    }

    public static void ChangeParty(int inParty, int maxParty, bool UpdatePresence = true)
    {
        RichPresence.partySize = inParty;
        RichPresence.partyMax = maxParty;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
    }

    public static void ChangeSecrets(string matchSecret, string joinSecret, string partyId, bool UpdatePresence = true)
    {
        RichPresence.matchSecret = matchSecret;
        RichPresence.joinSecret = joinSecret;
        RichPresence.partyId= partyId;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
    }

    public static void ChangeTimeStamps(long startTimeout, long endTimeout, bool UpdatePresence = true)
    {
        RichPresence.startTimestamp = startTimeout;
        RichPresence.endTimestamp = endTimeout;

        if (UpdatePresence) DiscordRpc.UpdatePresence(RichPresence);
    }

    void OnEnable()
    {
        Debug.Log("Discord: init");
        handlers = new DiscordRpc.EventHandlers();
        handlers.readyCallback += ReadyCallback;
        handlers.disconnectedCallback += DisconnectedCallback;
        handlers.errorCallback += ErrorCallback;
        handlers.joinCallback += JoinCallback;
        handlers.requestCallback += RequestCallback;
        DiscordRpc.Initialize(applicationId, ref handlers, true, "");
    }

    void OnApplicationQuit()
    {
        Debug.Log("Discord: shutdown");
        DiscordRpc.Shutdown();
    }

    void OnDestroy()
    {

    }
}