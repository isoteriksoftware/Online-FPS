using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomButton : MonoBehaviour
{
    [SerializeField] TMP_Text buttonText;

    RoomInfo roomInfo;

    public void SetButtonDetails(RoomInfo inputInfo)
    {
        roomInfo = inputInfo;
        buttonText.text = inputInfo.Name;
    }

    public void OpenRoom()
    {
        Launcher.instance.JoinRoom(roomInfo);
    }
}
