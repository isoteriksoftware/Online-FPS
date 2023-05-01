using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

    [SerializeField] GameObject loadingScreen;
    [SerializeField] GameObject menuButtons;
    [SerializeField] GameObject createRoomScreen;
    [SerializeField] GameObject roomScreen;
    [SerializeField] GameObject errorScreen;
    [SerializeField] GameObject roomBrowserScreen;
    [SerializeField] GameObject nameInputScreen;
    [SerializeField] GameObject startGameButton;
    [SerializeField] GameObject roomTestButton;

    [SerializeField] TMP_Text loadingText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text playerNameLabel;

    [SerializeField] TMP_InputField roomNameInput;
    [SerializeField] TMP_InputField nameInput;

    [SerializeField] RoomButton roomButton;

    [SerializeField] string level;

    List<RoomButton> roomButtons = new List<RoomButton>();
    List<TMP_Text> players = new List<TMP_Text>();

    public string[] maps;
    public bool changeMapsBetweenRounds = true;

    public static bool hasSetNick;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();

        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network...";

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        CloseMenus();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if (!hasSetNick)
        {
            CloseMenus();
            nameInputScreen.SetActive(true);

            if (PlayerPrefs.HasKey("playerName"))
            {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
    }

    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(roomNameInput.text))
        {
            CloseMenus();
            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        CloseMenus();
        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.SetActive(true);
        }
        else
        {
            startGameButton.SetActive(false);
        }
    }

    void ListAllPlayers()
    {
        foreach (TMP_Text player in players)
        {
            Destroy(player.gameObject);
        }
        players.Clear();

        var networkPlayers = PhotonNetwork.CurrentRoom.Players;
        foreach (var player in networkPlayers)
        {
            TMP_Text playerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            playerLabel.text = player.Value.NickName;
            playerLabel.gameObject.SetActive(true);

            players.Add(playerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        TMP_Text playerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        playerLabel.text = newPlayer.NickName;
        playerLabel.gameObject.SetActive(true);

        players.Add(playerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);

        CloseMenus();
        errorText.text = "Failed To Create Room: " + message;
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
    }

    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);

        foreach (RoomButton roomButton in roomButtons)
        {
            Destroy(roomButton.gameObject);
        }

        roomButtons.Clear();

        roomButton.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];

            if (info.PlayerCount != info.MaxPlayers && !info.RemovedFromList)
            {
                RoomButton rbtn = Instantiate(roomButton, roomButton.transform.parent);
                rbtn.SetButtonDetails(info);
                rbtn.gameObject.SetActive(true);

                roomButtons.Add(rbtn);
            }
        }
    }

    public void JoinRoom(RoomInfo roomInfo)
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);

        CloseMenus();
        loadingText.text = "Joining Room...";
        loadingScreen.SetActive(true);
    }

    public void SetNickname()
    {
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName = nameInput.text;

            PlayerPrefs.SetString("playerName", nameInput.text);

            CloseMenus();
            menuButtons.SetActive(true);

            hasSetNick = true;
        }
    }

    public void StartGame()
    {
        //PhotonNetwork.LoadLevel(level);

        PhotonNetwork.LoadLevel(maps[Random.Range(0, maps.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void QuickJoin()
    {
        PhotonNetwork.CreateRoom("Test");
        CloseMenus();
        loadingText.text = "Creating Test Room";
        loadingScreen.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
