using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Security.Cryptography;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

    public enum EventCode : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }

    public List<PlayerInfo> players = new List<PlayerInfo>();
    private int index;

    private List<LeaderboardPlayer> leaderboardPlayers = new List<LeaderboardPlayer>();

    public int killsToWin = 3;
    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterEnding = 5f;
    public bool perpetual;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);

            state = GameState.Playing;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
        {
            if (UIController.instance.leaderboard.activeInHierarchy)
            {
                UIController.instance.leaderboard.SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }
    }

    public void OnEvent(EventData eventData)
    {
        if (eventData.Code < 200)
        {
            EventCode code = (EventCode)eventData.Code;
            object[] data = (object[])eventData.CustomData;

            switch (code)
            {
                case EventCode.NewPlayer:
                    NewPlayerRecieve(data); break;
                case EventCode.ListPlayers:
                    ListPlayersRecieve(data); break;
                case EventCode.UpdateStat:
                    UpdateStatRecieve(data); break;
                case EventCode.NextMatch:
                    NextMatchReceive(); break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string name)
    {
        object[] package = new object[4];
        package[0] = name;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent((byte)EventCode.NewPlayer, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true });
    }

    public void NewPlayerRecieve(object[] data)
    {
        PlayerInfo player = new PlayerInfo((string)data[0], (int)data[1], (int)data[2], (int)data[3]);
        players.Add(player);

        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        object[] package = new object[players.Count + 1];

        package[0] = state;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerInfo player = players[i];
            object[] data = new object[4];
            data[0] = player.name;
            data[1] = player.actor;
            data[2] = player.kills;
            data[3] = player.deaths;

            package[i + 1] = data;
        }

        PhotonNetwork.RaiseEvent((byte)EventCode.ListPlayers, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void ListPlayersRecieve(object[] data)
    {
        players.Clear();

        state = (GameState)data[0];

        for (int i = 1; i < data.Length; i++)
        {
            object[] info = (object[])data[i];
            PlayerInfo player = new PlayerInfo((string)info[0], (int)info[1], (int)info[2], (int)info[3]);
            players.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

        StateCheck();
    }

    public void UpdateStatSend(int actorSending, int statIndex, int amount)
    {
        object[] package = new object[] { actorSending, statIndex, amount };
        PhotonNetwork.RaiseEvent((byte)EventCode.UpdateStat, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void UpdateStatRecieve(object[] data)
    {
        int actor = (int)data[0];
        int stat = (int)data[1];
        int amount = (int)data[2];

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: // kills
                        players[i].kills += amount;
                        break;
                    case 1: // deaths
                        players[i].deaths += amount;
                        break;
                }

                if (i == index)
                {
                    UpdateStatsDisplay();
                }

                if (UIController.instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderboard();
                }

                break;
            }
        }

        ScoreCheck();
    }

    public void UpdateStatsDisplay()
    {
        if (players.Count > index)
        {
            UIController.instance.getKillsText().text = "Kills: " + players[index].kills;
            UIController.instance.getDeathsText().text = "Deaths: " + players[index].deaths;
        }
        else
        {
            UIController.instance.getKillsText().text = "Kills: 0";
            UIController.instance.getDeathsText().text = "Deaths: 0";
        }
    }

    void ShowLeaderboard()
    {
        UIController.instance.leaderboard.SetActive(true);

        foreach (LeaderboardPlayer lp in leaderboardPlayers)
        {
            Destroy(lp.gameObject);
        }
        leaderboardPlayers.Clear();

        UIController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> list = SortPlayers(players);
        foreach (PlayerInfo player in list)
        {
            LeaderboardPlayer leaderboardPlayer = Instantiate(UIController.instance.leaderboardPlayerDisplay, 
                UIController.instance.leaderboardPlayerDisplay.transform.parent);
            leaderboardPlayer.gameObject.SetActive(true);
            leaderboardPlayer.SetDetails(player.name, player.kills, player.deaths);

            leaderboardPlayers.Add(leaderboardPlayer);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> list)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selection = players[0];

            foreach (PlayerInfo player in players)
            {
                if (!sorted.Contains(player))
                {
                    if (player.kills > highest)
                    {
                        highest = player.kills;
                        selection = player;
                    }
                }
            }

            sorted.Add(selection);
        }

        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in players)
        {
            if (player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            } 
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayersSend();
            }
        }
    }

    void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        state = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.endScreen.SetActive(true);
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;

        StartCoroutine(EndCoroutine());
    }

    IEnumerator EndCoroutine()
    {
        yield return new WaitForSeconds(waitAfterEnding);

        if (!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        } else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!Launcher.instance.changeMapsBetweenRounds)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, Launcher.instance.maps.Length);
                    if (Launcher.instance.maps[newLevel].Equals(SceneManager.GetActiveScene().name))
                    {
                        NextMatchSend();
                    } else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.maps[newLevel]);
                    }
                }
            }
        }
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent((byte)EventCode.NextMatch, null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;

        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);

        foreach (PlayerInfo player in players)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.instance.SpawnPlayer();
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string name, int actor, int kills, int deaths)
    {
        this.name = name;
        this.actor = actor;
        this.kills = kills;
        this.deaths = deaths;
    }
}
