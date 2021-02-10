using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using Mirror;

public class GameManager : NetworkBehaviour
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

    [Header("Player")]
    [Space][Space]
    // Player Variables
    [SerializeField] private GameObject participantPrefab = null;
    [System.Serializable] public struct ParticipantProperties
	{
#pragma warning disable IDE1006 // Naming Styles
		public string name => teamTag;
#pragma warning restore IDE1006 // Naming Styles
		[TagSelector] public string teamTag;
        public RuntimeAnimatorController controller;
	}
    [Tooltip("The properties of each player.")]
    [SerializeField] private ParticipantProperties[] participantProperties = null;
    [HideInInspector] private Vector3Int[,] spawnPointZones = new Vector3Int[4, 3];

    [Header("Scenes")]
    [Space][Space]
    // Scene management variables
    [Scene]
    private string currentlyLoadedScene = default;
    [Scene] [SerializeField]
    private string[] multiplayerScenes = null;
    private readonly string multiplayerScenePrefix = "MP_";
    [Scene] [SerializeField]
    private string menuScene = default;
    private readonly string menuScenePrefix = "MENU_";

    [Header("Menu Vars")]
    [Space][Space]
    [SerializeField][Range(0, 4)] private int MPPlayerCount = 2;
    [SerializeField][Range(0, 4)] private int MPAICount = 2;
    private int MPParticipantCount => MPPlayerCount + MPAICount;
    private int MPSelectedMapIndex = 2;
    private int MPMatchDurationMinutes = 3;

    [Header("Current Match Vars")]
    [Space][Space]
    private bool enoughPlayersJoined = false;
    public int LocalPlayerCount { get; private set; } = 0;
    public int OnlinePlayerCount => currentlyAliveParticipants.Count - LocalPlayerCount;
    private readonly List<Transform> currentlyAliveParticipants = new List<Transform>();
    private float currentTimeRemaining = 0f;
    private readonly float[] timeKeyMoments = { 0f, 10f, 30f, 60f * 1, 60f * 5, 60f * 15 };
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
        if(InterfaceHolder.instance != null)
		{
            switch(SystemInfo.deviceType)
            {
                case DeviceType.Desktop: case DeviceType.Console: InterfaceHolder.instance.areTouchControlsEnabled = false; break;
                case DeviceType.Handheld: InterfaceHolder.instance.areTouchControlsEnabled = true; break;
            }
		}
    }

    private void Start()
    {
        if(InterfaceHolder.instance != null)
		{
            UpdateInterfaces();
		}
    }

    private void LoadMultiplayerMap()
    {
		#region Map generation
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

		#region Loading spawn points
		// Clear the starting area for each player (the corners)
		// Participant 1 <Top left>
		spawnPointZones[0, 0] = new Vector3Int(playableArea.min.x + 1, playableArea.max.y - 1, 0);
        spawnPointZones[0, 1] = new Vector3Int(playableArea.min.x + 2, playableArea.max.y - 1, 0);
        spawnPointZones[0, 2] = new Vector3Int(playableArea.min.x + 1, playableArea.max.y - 2, 0);

        // Participant 2 <Bottom right>
        spawnPointZones[1, 0] = new Vector3Int(playableArea.max.x - 1, playableArea.min.y + 1, 0);
        spawnPointZones[1, 1] = new Vector3Int(playableArea.max.x - 2, playableArea.min.y + 1, 0);
        spawnPointZones[1, 2] = new Vector3Int(playableArea.max.x - 1, playableArea.min.y + 2, 0);

        // Participant 3 <Top right>
        spawnPointZones[2, 0] = new Vector3Int(playableArea.max.x - 1, playableArea.max.y - 1, 0);
        spawnPointZones[2, 1] = new Vector3Int(playableArea.max.x - 2, playableArea.max.y - 1, 0);
        spawnPointZones[2, 2] = new Vector3Int(playableArea.max.x - 1, playableArea.max.y - 2, 0);

        // Participant 4 <Bottom Left>
        spawnPointZones[3, 0] = new Vector3Int(playableArea.min.x + 1, playableArea.min.y + 1, 0);
        spawnPointZones[3, 1] = new Vector3Int(playableArea.min.x + 2, playableArea.min.y + 1, 0);
        spawnPointZones[3, 2] = new Vector3Int(playableArea.min.x + 1, playableArea.min.y + 2, 0);
		#endregion
		#endregion

		#region Player Spawning
		currentlyAliveParticipants.Clear();
        LocalPlayerCount = 0;
        for(int i = 0; i < MPPlayerCount; i++)
            AddParticipant(Instantiate(participantPrefab), true);
		#endregion
	}

	public void AddParticipant(GameObject participantObject, bool localPlayer)
    {
        // Check if the current player isn't already existing in the game.
        if(!participantObject.transform.IsFoundIn(currentlyAliveParticipants))
		{
            // Check if it has the stats component. If not, then we'll ignore this guy and return/
            if (participantObject.TryGetComponent(out ParticipantStats stats))
            {
                // Updating his stats
                int i = currentlyAliveParticipants.Count;
                if (i >= 4)
                {
                    Debug.LogError("Too many players have joined the match!" + $"Removing {participantObject}, AKA player {i}...");
                    Destroy(participantObject);
                    return;
			    }
                stats.participantNumber = i;
                if (localPlayer)
                {
                    stats.localParticipantNumber = LocalPlayerCount;
                    LocalPlayerCount++;
                }

                // Updating his action controller
                if (participantObject.TryGetComponent(out ParticipantActionController actionController))
                {
                    actionController.Tilemap = interactiveTilemap;
                    actionController.DestructibleTile = destructibleTile;
                }
                // Updating his name, tag and look.
                if (participantObject.TryGetComponent(out Animator animator))
                {
                    animator.runtimeAnimatorController = participantProperties[i].controller;
                    animator.tag = participantProperties[i].teamTag;
                    animator.name = $"Participant_{animator.tag}";
                }

                // Clearing his spawn area.
                // Time to clean up the area around the player
                for(int j = 0; j < 3; j++)
			    {
                    interactiveTilemap.SetTile(spawnPointZones[i, j], null);
			    }

                // Position the player correctly.
                participantObject.transform.SetPositionAndRotation(spawnPointZones[i, 0] + Vector3.one * 0.5f, Quaternion.identity);
                if(participantObject.TryGetComponent(out ParticipantMovementController movement))
				{
                    movement.DestinationTilePosition = participantObject.transform.position;
                }

                // Adding him to the bunch.
                currentlyAliveParticipants.Add(participantObject.transform);

                // Making sure that the match is ready to start.
                if(currentlyAliveParticipants.Count >= 2)
				{
                    enoughPlayersJoined = true;
                }
            }
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
                // Calculate the time from which the announcer should start playing sounds
                tKMIndex = timeKeyMoments.Length - 1;
                while (timeKeyMoments[tKMIndex] >= currentTimeRemaining && tKMIndex > 0)
                    tKMIndex--;

                // Play the game mode sound
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

                if (enoughPlayersJoined)
				{
                    // Checks if the players are still alive
                    for (int i = 0; i < currentlyAliveParticipants.Count; i++)
                    {
                        if (currentlyAliveParticipants[i] == null || currentlyAliveParticipants[i].GetComponent<UnitStats>().IsAlive == false)
                        {
                            currentlyAliveParticipants.Remove(currentlyAliveParticipants[i]);
                            if (currentlyAliveParticipants.Count <= 1)
                            {
                                string winner = currentlyAliveParticipants[0]?.name ?? "Unknown";
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
        }

        else if(currentlyLoadedScene.Contains(menuScenePrefix))
        {
            // Nothing... yet!
        }
    }

    /// <summary> Update the menu button values, and go back to the main menu. </summary>
    private void UpdateInterfaces()
    {
        InterfaceHolder holder_interface = InterfaceHolder.instance; AudioManager holder_audio = AudioManager.instance;
        holder_interface.SetActiveInterface(InterfaceType.MainMenu);
        holder_interface.SetActiveInterface(InterfaceType.Options, false);
        holder_interface.ModifyButtonText(InterfaceType.Options, ButtonAction.ToggleSoundtrack, "~" + (holder_audio.IsSoundtrackEnabled ? "On" : "Off"));
        holder_interface.ModifyButtonText(InterfaceType.Options, ButtonAction.ToggleTouchControls, "~" + (holder_interface.areTouchControlsEnabled ? "On" : "Off"));
        holder_interface.SetActiveInterface(InterfaceHolder.instance.PreviouslyActiveInterface);
        holder_interface.SetActiveInterface(InterfaceType.MatchLobby);
        holder_interface.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMap, $"~{multiplayerScenes[MPSelectedMapIndex].Split('/').Last().Substring(multiplayerScenePrefix.Length).Split('.').First()}");
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
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMap, $"~{multiplayerScenes[MPSelectedMapIndex].Split('/').Last().Substring(multiplayerScenePrefix.Length).Split('.').First()}");
                break;
            case ButtonAction.ModifyMatchDuration:
                MPMatchDurationMinutes = MPMatchDurationMinutes + 3 <= 20 ? MPMatchDurationMinutes + 3 : 3;
                InterfaceHolder.instance.ModifyButtonText(InterfaceType.MatchLobby, ButtonAction.ModifyMatchDuration, $"~{MPMatchDurationMinutes}");
                break;
            case ButtonAction.ModifyPlayerNumber: case ButtonAction.ModifyAINumber:
                if(multiplayerScenes[MPSelectedMapIndex].Contains("Network"))
				{
                    MPAICount = 0;
                    MPPlayerCount = 0;
				}
                else
				{
                    if(buttonAction == ButtonAction.ModifyPlayerNumber) MPPlayerCount = MPPlayerCount < 4 ? MPPlayerCount + 1 : 0;
                    else MPAICount = MPAICount < 4 ? MPAICount + 1 : 0;
				}
                InterfaceHolder.instance.ModifyButtonInteraction(InterfaceType.MatchLobby, ButtonAction.StartGame, ((2 <= MPParticipantCount && MPParticipantCount <= 4) || multiplayerScenes[MPSelectedMapIndex].ToLower().Contains("network")));
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