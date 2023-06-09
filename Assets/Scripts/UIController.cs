using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    [SerializeField] TMP_Text overheatedText;
    [SerializeField] TMP_Text deathText;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text killsText;
    [SerializeField] TMP_Text deathsText;
    [SerializeField] Slider weaponTempSlider;
    [SerializeField] Slider healthSlider;
    [SerializeField] GameObject deathScreen;
    public GameObject leaderboard;
    public LeaderboardPlayer leaderboardPlayerDisplay;
    public GameObject endScreen;
    public GameObject pauseScreen;
    public GameObject mapScreen;
    public TMP_Text playersText;

    public TMP_Text getOverheatedText()
    {
        return overheatedText;
    }

    public GameObject getDeathScreen()
    {
        return deathScreen;
    }

    public TMP_Text getDeathText() {  return deathText; }

    public Slider getWeaponTempSlider()
    {
        return weaponTempSlider;
    }

    public TMP_Text getHealthText() { return healthText; }

    public Slider getHealthSlider() { return healthSlider; }

    public TMP_Text getKillsText() { return killsText; }

    public TMP_Text getDeathsText() {  return deathsText; }

    void Awake()
    {
        instance = this;
    }

    public void ExitRoom()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
    }
}
