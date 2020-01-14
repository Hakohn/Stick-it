using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
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
        //pauseMenu.SetActive(false);
    }
    

    private void LoadMultiplayerMap()
    {
        interactiveTilemap = GameObject.FindGameObjectWithTag("Tilemap_interactible").GetComponent<Tilemap>();

        // Determine, based on the drawn tilemap, the bounds of the playable area
        for (int i = interactiveTilemap.cellBounds.min.x; i <= interactiveTilemap.cellBounds.max.x; i++)
        {
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
        }

        // Generate the destructible tiles randomly within the playable area
        for (int i = playableArea.min.x + 1; i < playableArea.max.x; i++)
        {
            for (int j = playableArea.min.y + 1; j < playableArea.max.y; j++)
            {
                Vector3Int targetCell = new Vector3Int(i, j, 0);
                if (interactiveTilemap.GetTile(targetCell) == null && Random.Range(0, 100) <= 80)
                {
                    interactiveTilemap.SetTile(targetCell, destructibleTile);
                }
            }
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
                case 0:
                    spawnPoint = new Vector3(playableArea.min.x + 1, playableArea.max.y - 1, 0) + tileWorldDifference;
                    break;
                case 1:
                    spawnPoint = new Vector3(playableArea.max.x - 1, playableArea.min.y + 1, 0) + tileWorldDifference;
                    break;
                case 2:
                    spawnPoint = new Vector3(playableArea.max.x - 1, playableArea.max.y - 1, 0) + tileWorldDifference;
                    break;
                case 3:
                    spawnPoint = new Vector3(playableArea.min.x + 1, playableArea.min.y + 1, 0) + tileWorldDifference;
                    break;
            }

            GameObject participantObject = Instantiate(participantPrefabs[i], spawnPoint, Quaternion.identity) as GameObject;

            participantObject.GetComponent<ParticipantStats>().participantNumber = i + 1;
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
                // Disable all the active menus
                InterfaceHolder.instance.DisableAllActiveMenus();
                
                // Enable in-game interface elements
                InterfaceHolder.instance.SetInGameInterfaceActive(true);

                // Load the map
                LoadMultiplayerMap();

                // Starting the countdown timer. It is measured in minutes, and the timer in seconds, so *60
                currentTimeRemaining = MPMatchDurationMinutes * 60;
                // Calculate the time from which the announcer shoul start playing sounds
                tKMIndex = timeKeyMoments.Length - 1;
                while (timeKeyMoments[tKMIndex] >= currentTimeRemaining && tKMIndex > 0)
                    tKMIndex--;

                // Play the gamemode sound
                AudioManager.instance.PlayGlobalSound("MPIGGameMode");

                // Start the multiplayer theme, stop the others
                AudioManager.instance.StopGlobalSoundsContaining("Loop");
                AudioManager.instance.StopAllPlayingSoundtracks();
                AudioManager.instance.themeType = AudioManager.ThemeType.MULTIPLAYER;
            }
            // If it is a menu scene, set the variables up and enable / disable what needs so
            else if(currentlyLoadedScene.Contains(menuScenePrefix))
            {
                // Disable in-game interface elements
                InterfaceHolder.instance.SetInGameInterfaceActive(false);

                // Update the texts within the Menu->MultiplayerMenu
                InterfaceHolder.instance.UpdateMenuButtonTextValue("Map", multiplayerScenes[MPSelectedMapIndex].Substring(multiplayerScenePrefix.Length));
                InterfaceHolder.instance.UpdateMenuButtonTextValue("Duration", MPMatchDurationMinutes.ToString());
                InterfaceHolder.instance.UpdateMenuButtonTextValue("Players", MPPlayerCount.ToString());
                InterfaceHolder.instance.UpdateMenuButtonTextValue("AIs", MPAICount.ToString());
                if (AudioManager.instance.isSoundtrackEnabled == true)
                    InterfaceHolder.instance.UpdateMenuButtonTextValue("Soundtrack", "Enabled");
                else
                    InterfaceHolder.instance.UpdateMenuButtonTextValue("Soundtrack", "Disabled");

                // Disable all menus besides the main one
                InterfaceHolder.instance.SetActiveMenu("Main", "Main");
                AudioManager.instance.PlayGlobalSound("WindLoop");

                // Start the menu theme, stop the others
                AudioManager.instance.StopAllPlayingSoundtracks();
                AudioManager.instance.themeType = AudioManager.ThemeType.MENU;
            }
        }

        // Based on the currently loaded scene, find out what needs to be updated within it
        if(currentlyLoadedScene.Contains(multiplayerScenePrefix))
        {
            if (gameOver == false)
            {
                // Go to or exit the pause menu
                if (Input.GetButtonDown("Cancel"))
                {
                    // If the game is paused, unfreeze the time, and hide the pause menu
                    if (GameIsPaused)
                    {
                        MenuIGResumeGame();
                    }
                    else
                    {
                        MenuIGPauseGame();
                    }
                }

                // Checks if the players are still alive
                for(int i = 0; i < currentlyAliveParticipants.Count; i++)
                {
                    if (currentlyAliveParticipants[i] == null || currentlyAliveParticipants[i].GetComponent<UnitStats>().IsAlive == false)
                    {
                        currentlyAliveParticipants.Remove(currentlyAliveParticipants[i]);
                        if (currentlyAliveParticipants.Count <= 1)
                        {
                            string winner = currentlyAliveParticipants[0].name;
                            winner = winner.Substring(winner.IndexOf("Player_") + "Player_".Length);
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
                        case 1: AudioManager.instance.PlayGlobalSound("MPIG10Secs"); break;
                        case 2: AudioManager.instance.PlayGlobalSound("MPIG30Secs"); break;
                        case 3: AudioManager.instance.PlayGlobalSound("MPIG1Min"); break;
                        case 4: AudioManager.instance.PlayGlobalSound("MPIG5Mins"); break;
                        case 5: AudioManager.instance.PlayGlobalSound("MPIG15Mins"); break;
                    }
                    tKMIndex--;
                }

                // Checks if the game is over, either it be from the death of too many players or from time constraints
                if (gameOver)
                {
                    StartCoroutine(FinishGameAnimation());
                }
            }
        }

        else if(currentlyLoadedScene.Contains(menuScenePrefix))
        {
            // Nothing... yet!
        }
    }

    private IEnumerator FinishGameAnimation()
    {
        AudioManager.instance.StopAllPlayingSoundtracks();
        Time.timeScale = 0.3f;
        yield return new WaitForSeconds(1f);
        AudioManager.instance.PlayGlobalSound("MPIGGameOver");
        yield return new WaitForSeconds(1.5f);
        Time.timeScale = 1f;

        currentlyAliveParticipants.Clear();

        gameOver = false;

        SceneManager.LoadScene(menuScene);
    }

    /// <summary>
    /// Main menu quit button action
    /// </summary>
    public void MenuMainQuit()
    {
        Application.Quit();
    }

    /// <summary>
    /// Multiplayer menu play game button action
    /// </summary>
    public void MenuMPPlayGame()
    {
        SceneManager.LoadScene(multiplayerScenes[MPSelectedMapIndex]);
    }

    /// <summary>
    /// Multiplayer menu select map button action
    /// </summary>
    public void MenuMPSelectMap()
    {
        if (MPSelectedMapIndex + 1 < multiplayerScenes.Length)
            MPSelectedMapIndex++;
        else
            MPSelectedMapIndex = 0;

        InterfaceHolder.instance.UpdateMenuButtonTextValue("Map", multiplayerScenes[MPSelectedMapIndex].Substring(multiplayerScenePrefix.Length));
    }

    /// <summary>
    /// Multiplayer menu select match duration button action
    /// </summary>
    public void MenuMPSelectMatchDuration()
    {
        if (MPMatchDurationMinutes + 3 <= 20)
            MPMatchDurationMinutes += 3;
        else
            MPMatchDurationMinutes = 1;

        InterfaceHolder.instance.UpdateMenuButtonTextValue("Duration", MPMatchDurationMinutes.ToString());
    }

    /// <summary>
    /// Multiplayer menu select player number button action
    /// </summary>
    public void MenuMPSelectPlayerNumber()
    {
        if (MPPlayerCount < 4)
            MPPlayerCount++;
        else
            MPPlayerCount = 0;

        InterfaceHolder.instance.SetButtonInteractible("StartGame", (2 <= MPParticipantCount && MPParticipantCount <= 4) ? true : false);
        InterfaceHolder.instance.UpdateMenuButtonTextValue("Players", MPPlayerCount.ToString());
    }

    /// <summary>
    /// Multiplayer menu select AI number button action
    /// </summary>
    public void MenuMPSelectAINumber()
    {
        if (MPAICount < 4)
            MPAICount++;
        else
            MPAICount = 0;

        InterfaceHolder.instance.SetButtonInteractible("StartGame", (2 <= MPParticipantCount && MPParticipantCount <= 4) ? true : false);
        InterfaceHolder.instance.UpdateMenuButtonTextValue("AIs", MPAICount.ToString());
    }

    /// <summary>
    /// Menu toggle soundtrack button action
    /// </summary>
    public void MenuOptionsToggleSoundtrack()
    {
        string updateToString = null;
        if (AudioManager.instance.ToggleSoundtrack() == true)
            updateToString = "Enabled";
        else
            updateToString = "Disabled";


        InterfaceHolder.instance.UpdateMenuButtonTextValue("Soundtrack", updateToString);
    }

    /// <summary>
    /// In game menu pause button action
    /// </summary>
    public void MenuIGPauseGame()
    {
        Time.timeScale = 0f;
        InterfaceHolder.instance.SetActiveMenu("Pause", "Main");
        GameIsPaused = true;
    }

    /// <summary>
    /// In game menu resume button action
    /// </summary>
    public void MenuIGResumeGame()
    {
        Time.timeScale = 1f;
        InterfaceHolder.instance.DisableAllActiveMenus();
        GameIsPaused = false;
    }

    /// <summary>
    /// In game menu back to main menu button action
    /// </summary>
    public void MenuIGBackToMenu()
    {
        // Resuming the game, so that the time won't remain frozen
        MenuIGResumeGame();
        SceneManager.LoadScene(menuScene);
    }
}