﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    #region Variables
    // General variables
    public static GameManager instance;
    [HideInInspector] public bool GameIsPaused = false;

    [Header("Tiles")]
    [Space][Space]
    // Tilemap variables
    private Tilemap interactiveTilemap = null;
    private BoundsInt playableArea = new BoundsInt();
    [SerializeField] private TileBase destructibleTile = null;
    [SerializeField] private TileBase marginTile = null;

    [Space][Space]
    // Player Variables
    [SerializeField] private GameObject[] participantPrefabs = null;

    [Space][Space]
    // Scene management variables
    private string currentlyLoadedScene = null;
    [SerializeField] private string[] multiplayerScenes = null;
    [SerializeField] private string multiplayerScenePrefix = "MP_";
    [SerializeField] private string menuScene = null;
    [SerializeField] private string menuScenePrefix = "MENU_";

    [Space][Space]
    // Menu variables
    [SerializeField][Range(0, 4)] private int MPPlayerCount = 2;
    [SerializeField][Range(0, 4)] private int MPAICount = 2;
    private int MPParticipantCount => MPPlayerCount + MPAICount;
    private int MPSelectedMapIndex = 2;
    private int MPMatchDurationMinutes = 3;

    [Space][Space]
    // Current multiplayer match variables
    private List<Transform> currentlyAliveParticipants = new List<Transform>();
    private float currentTimeRemaining = 0f;
    private float[] timeKeyMoments = { 0f, 10f, 30f, 60f * 1, 60f * 5, 60f * 15 };
    private int tKMIndex = 0;
    private bool gameOver = false;
    #endregion

    private void Awake()
    {
        // Set the gameManager static, with only one instance available per scene and unkillable when scenes change 
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        
        // Disable the UI elements who aren't supposed to be shown at this time
        // And update the required settings based on the system we're using
        switch(SystemInfo.deviceType)
        {
            case DeviceType.Desktop: case DeviceType.Console: InterfaceHolder.instance.areTouchControlsEnabled = false; break;
            case DeviceType.Handheld: InterfaceHolder.instance.areTouchControlsEnabled = true; break;
        }
    }

    private void Start()
    {
        UpdateInterfaces();
    }

    private void LoadMultiplayerMap()
    {
        interactiveTilemap = GameObject.FindGameObjectWithTag("Tilemap_interactible").GetComponent<Tilemap>();

        // Determine, based on the drawn tilemap, the bounds of the playable area
        for (int i = interactiveTilemap.cellBounds.min.x; i <= interactiveTilemap.cellBounds.max.x; i++)
            for (int j = interactiveTilemap.cellBounds.min.y; j <= interactiveTilemap.cellBounds.max.y; j++)
            {
                Vector3Int targetCell = new Vector3Int(i, j, 0);
                if (interactiveTilemap.GetTile(targetCell) == marginTile)
                {
                    if (
                        interactiveTilemap.GetTile(targetCell + new Vector3Int(-1, 0, 0)) != marginTile &&
                        interactiveTilemap.GetTile(targetCell + new Vector3Int(0, 1, 0)) != marginTile
                        )
                    {
                        playableArea.min = targetCell;
                    }
                    if (
                        interactiveTilemap.GetTile(targetCell + new Vector3Int(1, 0, 0)) != marginTile &&
                        interactiveTilemap.GetTile(targetCell + new Vector3Int(0, -1, 0)) != marginTile
                        )
                    {
                        playableArea.max = targetCell;
                    }
                }
            }

        // Generate the destructible tiles randomly within the playable area
        for (int i = playableArea.min.x + 1; i < playableArea.max.x; i++)
            for (int j = playableArea.min.y + 1; j < playableArea.max.y; j++)
            {
                Vector3Int targetCell = new Vector3Int(i, j, 0);
                if (interactiveTilemap.GetTile(targetCell) == null && Random.Range(0, 100) <= 80)
                    interactiveTilemap.SetTile(targetCell, destructibleTile);
            }

        // Clear the starting area for each player (the corners)
        // Participant 1 <Top left>
        if (MPParticipantCount >= 1)
        {
            interactiveTilemap.SetTile(new Vector3Int(playableArea.min.x + 1, playableArea.max.y - 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.min.x + 2, playableArea.max.y - 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.min.x + 1, playableArea.max.y - 2, 0), null);
        }

        // Participant 2 <Bottom right>
        if (MPParticipantCount >= 2)
        {
            interactiveTilemap.SetTile(new Vector3Int(playableArea.max.x - 1, playableArea.min.y + 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.max.x - 2, playableArea.min.y + 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.max.x - 1, playableArea.min.y + 2, 0), null);
        }

        // Participant 3 <Top right>
        if (MPParticipantCount >= 3)
        {
            interactiveTilemap.SetTile(new Vector3Int(playableArea.max.x - 1, playableArea.max.y - 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.max.x - 2, playableArea.max.y - 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.max.x - 1, playableArea.max.y - 2, 0), null);
        }

        // Participant 4 <Bottom Left>
        if (MPParticipantCount == 4)
        {
            interactiveTilemap.SetTile(new Vector3Int(playableArea.min.x + 1, playableArea.min.y + 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.min.x + 2, playableArea.min.y + 1, 0), null);
            interactiveTilemap.SetTile(new Vector3Int(playableArea.min.x + 1, playableArea.min.y + 2, 0), null);
        }



        // Instantiate each participant and grant them a reference to the interactive tilemap and the destructible tiles (so they can be passed to the spawned bombs)
        for (int i = 0; i < MPParticipantCount; i++)
        {
            Vector3 spawnPoint = new Vector3();
            Vector3 tileWorldDifference = new Vector3(0.5f, 0.5f);

            
            // Setting the spawn point for each participant, and adding them to the current alive participants
            switch (i)
            {
                case 0: spawnPoint = new Vector3(playableArea.min.x, playableArea.max.y) + Vector3.right + Vector3.down; break;
                case 1: spawnPoint = new Vector3(playableArea.max.x, playableArea.min.y) + Vector3.left + Vector3.up; break;
                case 2: spawnPoint = playableArea.max + Vector3.left + Vector3.down; break;
                case 3: spawnPoint = playableArea.min + Vector3.right + Vector3.up; break;
            }
            spawnPoint += tileWorldDifference;

            GameObject participantObject = Instantiate(participantPrefabs[i], spawnPoint, Quaternion.identity) as GameObject;

            participantObject.GetComponent<ParticipantStats>().participantNumber = i + 1;
            participantObject.GetComponent<ParticipantStats>().IsMainPlayer = i == 0 ? true : false;
            participantObject.GetComponent<ParticipantActionController>().Tilemap = interactiveTilemap;
            participantObject.GetComponent<ParticipantActionController>().DestructibleTile = destructibleTile;

            currentlyAliveParticipants.Add(participantObject.transform);
        }
    }


    private void Update()
    {
        // Check if the scene has been changed. If so, load the stuff that needs to be loaded in that scene
        if (currentlyLoadedScene != SceneManager.GetActiveScene().name)
        {
            currentlyLoadedScene = SceneManager.GetActiveScene().name;
            // If the active scene is a multiplayer map, load the map based on the multiplayer map loading function
            if(currentlyLoadedScene.Contains(multiplayerScenePrefix))
            {
                // Activate the HUD, and deactivate the rest.
                InterfaceHolder.instance.SetActiveInterface(InterfaceType.HUD);

                // Load the map
                LoadMultiplayerMap();

                // Starting the countdown timer. It is measured in minutes, and the timer in seconds, so *60
                currentTimeRemaining = MPMatchDurationMinutes * 60;
                // Calculate the time from which the announcer shoul start playing sounds
                tKMIndex = timeKeyMoments.Length - 1;
                while (timeKeyMoments[tKMIndex] >= currentTimeRemaining && tKMIndex > 0)
                    tKMIndex--;

                // Play the gamemode sound
                AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "Slayer");

                // Start the match theme, stop the others
                AudioManager.instance.StopAllGlobalSounds(SoundCategory.Environment);
                AudioManager.instance.StopAllPlayingSoundtracks();
                AudioManager.instance.soundtrackCategory = SoundCategory.MatchSoundtrack;
            }
            // If it is a menu scene, set the variables up and enable / disable what needs so
            else if(currentlyLoadedScene.Contains(menuScenePrefix))
            {
                // Switch to main menu
                InterfaceHolder.instance.SetActiveInterface(InterfaceType.MainMenu);
                AudioManager.instance.PlayGlobalSound(SoundCategory.Environment, "Wind");

                // Start the menu theme, stop the others
                AudioManager.instance.StopAllPlayingSoundtracks();
                AudioManager.instance.soundtrackCategory = SoundCategory.MenuSoundtrack;
            }
        }

        // Based on the currently loaded scene, find out what needs to be updated within it
        if(currentlyLoadedScene.Contains(multiplayerScenePrefix))
        {
            if (gameOver == false)
            {
                // Go to or exit the pause menu
                if (Input.GetButtonDown("Cancel"))
                    // If the game is paused, unfreeze the time, and hide the pause menu; else, the opposite.
                    ButtonPress(ButtonAction.TogglePause);

                // Checks if the players are still alive
                for(int i = 0; i < currentlyAliveParticipants.Count; i++)
                {
                    if (currentlyAliveParticipants[i] == null || currentlyAliveParticipants[i].GetComponent<UnitStats>().IsAlive == false)
                    {
                        currentlyAliveParticipants.Remove(currentlyAliveParticipants[i]);
                        if (currentlyAliveParticipants.Count <= 1)
                        {
                            string winner = currentlyAliveParticipants[0].name;
                            winner = winner.Substring(winner.IndexOf("Participant_") + "Participant_".Length);
                            winner = winner.Replace("(Clone)", "");

                            Debug.Log("Game winner: " + winner);
                            gameOver = true;
                        }
                    }
                }

                // Decreasing the time remaining and playing the required sound at key moments
                if (currentTimeRemaining - Time.deltaTime <= 0) currentTimeRemaining = 0;
                else currentTimeRemaining -= Time.deltaTime;

                // Update the interface clock value
                InterfaceHolder.instance.UpdateTimerValue(currentTimeRemaining);

                if (tKMIndex >= 0 && currentTimeRemaining <= timeKeyMoments[tKMIndex])
                {
                    switch (tKMIndex)
                    {
                        case 0: gameOver = true; Debug.Log("Game winner: Draw!"); break;
                        case 1: AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "10s"); break;
                        case 2: AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "30s"); break;
                        case 3: AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "1m"); break;
                        case 4: AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "5m"); break;
                        case 5: AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "15m"); break;
                    }
                    tKMIndex--;
                }

                // Checks if the game is over, either it be from the death of too many players or from time constraints
                if (gameOver)
                    StartCoroutine(FinishGameAnimation());
            }
        }

        else if(currentlyLoadedScene.Contains(menuScenePrefix))
        {
            // Nothing... yet!
        }
    }

    /// <summary> Update the menu button values, and go back to the main menu. </summary>
    private void UpdateInterfaces()
    {
        var holder_interface = InterfaceHolder.instance; var holder_audio = AudioManager.instance;
        holder_interface.SetActiveInterface(InterfaceType.MainMenu);
        holder_interface.SetActiveInterface(InterfaceType.Options, false);
        holder_interface.ModifyButtonText(InterfaceType.Options, ButtonAction.ToggleSoundtrack, "~" + (holder_audio.IsSoundtrackEnabled ? "On" : "Off"));
        holder_interface.ModifyButtonText(InterfaceType.Options, ButtonAction.ToggleTouchControls, "~" + (holder_interface.areTouchControlsEnabled ? "On" : "Off"));
        holder_interface.SetActiveInterface(InterfaceHolder.instance.PreviouslyActiveInterface);
        holder_interface.SetActiveInterface(InterfaceType.MatchLobby);
        holder_interface.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMap, $"~{multiplayerScenes[MPSelectedMapIndex].Substring(multiplayerScenePrefix.Length)}");
        holder_interface.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyPlayerNumber, $"~{MPPlayerCount}");
        holder_interface.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyAINumber, $"~{MPAICount}");
        holder_interface.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMatchDuration, $"~{MPMatchDurationMinutes}");
        holder_interface.SetActiveInterface(InterfaceHolder.instance.PreviouslyActiveInterface);
    }

    private IEnumerator FinishGameAnimation()
    {
        AudioManager.instance.StopAllPlayingSoundtracks();
        Time.timeScale = 0.3f;
        yield return new WaitForSeconds(1f);
        AudioManager.instance.PlayGlobalSound(SoundCategory.Announcer, "GameOver");
        yield return new WaitForSeconds(1.5f);
        Time.timeScale = 1f;

        currentlyAliveParticipants.Clear();

        gameOver = false;

        SceneManager.LoadScene(menuScene);
    }

    public void ButtonPress(ButtonAction buttonAction, InterfaceType menuToSwitchTo = InterfaceType.None)
    {
        switch(buttonAction)
        {
            case ButtonAction.Unknown:
                Debug.LogError("Unknown button action triggered! (Was this on purpose?)");
                break;
            case ButtonAction.SwitchInterface:
                // Disable the active menus and set the requested one to active
                InterfaceHolder.instance.SetActiveInterface(menuToSwitchTo);
                break;
            case ButtonAction.QuitGame:
                Application.Quit();
                break;
            case ButtonAction.StartGame:
                SceneManager.LoadScene(multiplayerScenes[MPSelectedMapIndex]);
                break;
            case ButtonAction.ModifyMap:
                MPSelectedMapIndex = MPSelectedMapIndex + 1 < multiplayerScenes.Length ? MPSelectedMapIndex + 1 : 0;
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMap, $"~{multiplayerScenes[MPSelectedMapIndex].Substring(multiplayerScenePrefix.Length)}");
                break;
            case ButtonAction.ModifyMatchDuration:
                MPMatchDurationMinutes = MPMatchDurationMinutes + 3 <= 20 ? MPMatchDurationMinutes + 3 : 3;
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMatchDuration, $"~{MPMatchDurationMinutes}");
                break;
            case ButtonAction.ModifyPlayerNumber: case ButtonAction.ModifyAINumber:
                if(buttonAction == ButtonAction.ModifyPlayerNumber) MPPlayerCount = MPPlayerCount < 4 ? MPPlayerCount + 1 : 0;
                else MPAICount = MPAICount < 4 ? MPAICount + 1 : 0;
                InterfaceHolder.instance.ModifyButtonInteraction(InterfaceType.MatchLobby, ButtonAction.StartGame, (2 <= MPParticipantCount && MPParticipantCount <= 4) ? true : false);
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.MatchLobby, buttonAction, $"~{(buttonAction == ButtonAction.ModifyPlayerNumber ? MPPlayerCount : MPAICount)}");
                break;
            case ButtonAction.ToggleSoundtrack:
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.Options, ButtonAction.ToggleSoundtrack, "~" + (AudioManager.instance.ToggleSoundtrack() == true ? "On" : "Off"));
                break;
            case ButtonAction.ToggleTouchControls:
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.Options, ButtonAction.ToggleTouchControls, "~" + (InterfaceHolder.instance.ToggleTouchControls() == true ? "On" : "Off"));
                break;
            case ButtonAction.TogglePause:
                GameIsPaused = !GameIsPaused;
                AudioManager.instance.PauseSoundtracks(GameIsPaused);
                Time.timeScale = GameIsPaused ? 0f : 1f;
                if(!GameIsPaused) /*Resume*/ InterfaceHolder.instance.SetActiveInterface(InterfaceType.HUD);
                else /*Pause*/ InterfaceHolder.instance.SetActiveInterface(InterfaceType.Pause, false);
                break;
            case ButtonAction.ReturnToMainMenu:
                ButtonPress(ButtonAction.TogglePause); // Resuming the game;
                SceneManager.LoadScene(menuScene);
                break;
            case ButtonAction.Back:
                ButtonPress(ButtonAction.SwitchInterface, InterfaceHolder.instance.PreviouslyActiveInterface);
                break;
        }
    }
}