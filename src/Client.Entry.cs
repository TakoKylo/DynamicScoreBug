using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

public sealed partial class Scoreboard_ClientMod : global::IPuckMod
{
    private GameObject _host;
    private Harmony _harmony;
    
    // Static reference to host for callbacks
    private static GameObject _staticHost = null;
    
    // Track if mod is enabled
    public static bool IsModEnabled { get; private set; } = false;

    public bool OnEnable()
    {
        try
        {
            // Initialize Harmony for chat command patching
            _harmony = new Harmony("net.scoreboard.chatcommands");
            
            // Headless or no TMP? Skip UI.
            var hasTMP = HarmonyLib.AccessTools.TypeByName("TMPro.TMP_Text") != null;
            var isHeadless = Application.isBatchMode || !hasTMP;

            if (!isHeadless)
            {
                _host = new GameObject("[Scoreboard Host]");
                UnityEngine.Object.DontDestroyOnLoad(_host);
                _staticHost = _host;

                // Add custom broadcast scoreboard
                var scoreboard = _host.AddComponent<CustomScoreboard.UI.CustomBroadcastScoreboard>();
                if (scoreboard == null)
                {
                    Debug.LogError("[Scoreboard] Failed to add CustomBroadcastScoreboard component!");
                    return false;
                }
                Debug.Log("[Scoreboard] Custom broadcast scoreboard added.");

                // Add scoreboard UI manager
                var uiManager = _host.AddComponent<CustomScoreboard.UI.ScoreboardUIManager>();
                if (uiManager == null)
                {
                    Debug.LogError("[Scoreboard] Failed to add ScoreboardUIManager component!");
                    return false;
                }
                uiManager.SetScoreboardReference(scoreboard);
                Debug.Log("[Scoreboard] UI Manager added.");

                // Patch chat for /scoreboard command and message monitoring
                PatchChatCommands();

                // Register with ModMenuHub for unified "Modifications" button
                RegisterWithModMenuHub();

                // Suppress native scorebug show calls while custom scoreboard is enabled.
                PatchNativeScorebugSuppression();

                // Register for star data from oomtm450_stats mod
                RegisterStarDataListener(scoreboard);
            }

            IsModEnabled = true;
            Debug.Log("[Scoreboard] Enabled.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] Failed to enable: " + e);
            try { _harmony?.UnpatchSelf(); } catch { }
            try { if (_host != null) UnityEngine.Object.Destroy(_host); } catch { }
            _harmony = null;
            _host = null;
            return false;
        }
    }

    public bool OnDisable()
    {
        IsModEnabled = false;
        
        // Unregister from ModMenuHub
        try
        {
            PonceMods.Shared.ModMenuHub.UnregisterMod("Scoreboard");
            PonceMods.Shared.ModMenuHub.Cleanup("Scoreboard");
        }
        catch { }
        
        try { _harmony?.UnpatchSelf(); } catch { }
        try { if (_host != null) UnityEngine.Object.Destroy(_host); } catch { }
        _harmony = null;
        _host = null;
        Debug.Log("[Scoreboard] Disabled.");
        return true;
    }

    private void PatchChatCommands()
    {
        try
        {
            // Intercept slash commands on send.
            TryPatchChatSend();
            // Monitor incoming chat messages for game events (shot speed, shootout, stars, etc.)
            TryPatchChatReceive();
            Debug.Log("[Scoreboard] Chat command patch applied.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] Failed to patch chat commands: " + e);
        }
    }

    private void RegisterWithModMenuHub()
    {
        try
        {
            // Register our button entry with ModMenuHub
            PonceMods.Shared.ModMenuHub.RegisterMod(
                "Scoreboard",
                "SCOREBUG",
                OnScoreboardButtonClicked,
                30 // Priority - lower = higher in list
            );
            
            // Initialize ModMenuHub (first mod to call this becomes owner)
            PonceMods.Shared.ModMenuHub.Initialize("Scoreboard");
            
            Debug.Log("[Scoreboard] Registered with ModMenuHub");
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] ModMenuHub registration failed: " + e);
        }
    }


    
    private static void OnScoreboardButtonClicked()
    {
        try
        {
            Debug.Log("[Scoreboard] Scoreboard Settings button clicked");
            
            var uiManager = UnityEngine.Object.FindFirstObjectByType<CustomScoreboard.UI.ScoreboardUIManager>();
            if (uiManager != null)
            {
                uiManager.ToggleUI();
                Debug.Log("[Scoreboard] Toggled scoreboard settings UI");
            }
            else
            {
                Debug.LogError("[Scoreboard] ScoreboardUIManager not found!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Scoreboard] Failed to open scoreboard settings: {e}");
        }
    }


    private void TryPatchChatSend()
    {
        try
        {
            // B310 sends chat through ChatManager rather than UIChat.
            var method = AccessTools.Method(typeof(ChatManager), "Client_SendChatMessage", new[] { typeof(string), typeof(bool), typeof(bool) });
            
            if (method != null)
            {
                _harmony.Patch(method, prefix: new HarmonyMethod(typeof(Scoreboard_ClientMod), nameof(Chat_ScoreboardCommand_Prefix)));
                Debug.Log("[Scoreboard] Patched ChatManager.Client_SendChatMessage for /scoreboard command");
            }
            else
            {
                Debug.LogError("[Scoreboard] Could not find ChatManager.Client_SendChatMessage method");
            }
        }
        catch (Exception e) 
        { 
            Debug.LogError("[Scoreboard] Chat patch failed: " + e); 
        }
    }

    private void TryPatchChatReceive()
    {
        try
        {
            // Patch ChatManager.AddChatMessage for shootout/star/reset/shot-speed chat detection.
            var chatMethod = AccessTools.Method(typeof(ChatManager), "AddChatMessage", new[] { typeof(ChatMessage) });
            if (chatMethod != null)
            {
                _harmony.Patch(chatMethod, postfix: new HarmonyMethod(typeof(Scoreboard_ClientMod), nameof(Chat_MessageReceived_Postfix)));
                Debug.Log("[Scoreboard] Patched ChatManager.AddChatMessage for message monitoring");
            }
            else
            {
                Debug.LogError("[Scoreboard] Could not find ChatManager.AddChatMessage method");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] Chat receive patch failed: " + e);
        }
    }

    private void RegisterStarDataListener(CustomScoreboard.UI.CustomBroadcastScoreboard scoreboard)
    {
        try
        {
            // Use Unity's CustomMessagingManager to listen for star data
            var messagingManager = Unity.Netcode.NetworkManager.Singleton?.CustomMessagingManager;
            if (messagingManager != null)
            {
                messagingManager.RegisterNamedMessageHandler("oomtm450_statsSTAR", (senderId, reader) =>
                {
                    try
                    {
                        // Read the data: SteamID;StarNumber
                        var bytes = new byte[reader.Length];
                        reader.ReadBytes(ref bytes, (int)reader.Length);
                        string data = System.Text.Encoding.UTF8.GetString(bytes);
                        
                        Debug.Log($"[Scoreboard] Star data received: {data}");
                        
                        var parts = data.Split(';');
                        if (parts.Length >= 2)
                        {
                            string steamId = parts[0];
                            int starNumber = int.Parse(parts[1]);
                            
                            // Get player name from Steam ID
                            var playerManager = MonoBehaviourSingleton<PlayerManager>.Instance;
                            if (playerManager != null)
                            {
                                var player = playerManager.GetPlayerBySteamId(steamId);
                                if (player != null)
                                {
                                    string playerName = player.Username.Value.ToString();
                                    Debug.Log($"[Scoreboard] Star {starNumber} = {playerName} (Steam ID: {steamId})");
                                    
                                    // Set the star
                                    if (starNumber == 1)
                                        scoreboard.OnFirstStarAnnounced(playerName);
                                    else if (starNumber == 2)
                                        scoreboard.OnSecondStarAnnounced(playerName);
                                    else if (starNumber == 3)
                                        scoreboard.OnThirdStarAnnounced(playerName);
                                }
                                else
                                {
                                    Debug.LogWarning($"[Scoreboard] Could not find player with Steam ID: {steamId}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Scoreboard] Error processing star data: {ex}");
                    }
                });
                
                Debug.Log("[Scoreboard] Registered listener for oomtm450_statsSTAR messages");
                
                // Register listener for shot data (BATCHSOG)
                messagingManager.RegisterNamedMessageHandler("oomtm450_statsBATCHSOG", (senderId, reader) =>
                {
                    try
                    {
                        // Read the data: SteamID;ShotCount;SteamID;ShotCount;...
                        var bytes = new byte[reader.Length];
                        reader.ReadBytes(ref bytes, (int)reader.Length);
                        string data = System.Text.Encoding.UTF8.GetString(bytes);
                        
                        Debug.Log($"[Scoreboard] Received BATCHSOG data: {data}");
                        
                        // Parse the batch data
                        string[] entries = data.Split(';');
                        for (int i = 0; i < entries.Length - 1; i += 2)
                        {
                            if (ulong.TryParse(entries[i], out ulong steamId) && 
                                int.TryParse(entries[i + 1], out int shotCount))
                            {
                                // Update the scoreboard with shot data
                                scoreboard.UpdatePlayerShots(steamId, shotCount);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Scoreboard] Error processing shot data: {ex}");
                    }
                });
                
                Debug.Log("[Scoreboard] Registered listener for oomtm450_statsBATCHSOG messages");
                
                // Register listener for save percentage data (BATCHSAVEPERC)
                messagingManager.RegisterNamedMessageHandler("oomtm450_statsBATCHSAVEPERC", (senderId, reader) =>
                {
                    try
                    {
                        // Read the data: SteamID;(shots, saves);SteamID;(shots, saves);...
                        var bytes = new byte[reader.Length];
                        reader.ReadBytes(ref bytes, (int)reader.Length);
                        string data = System.Text.Encoding.UTF8.GetString(bytes);
                        
                        Debug.Log($"[Scoreboard] Received BATCHSAVEPERC data: {data}");
                        
                        // Parse the batch data
                        string[] entries = data.Split(';');
                        for (int i = 0; i < entries.Length - 1; i += 2)
                        {
                            if (ulong.TryParse(entries[i], out ulong steamId))
                            {
                                // Parse tuple format: (shots, saves)
                                string tuple = entries[i + 1].Trim('(', ')');
                                string[] values = tuple.Split(',');
                                if (values.Length == 2 && 
                                    int.TryParse(values[0].Trim(), out int shots) &&
                                    int.TryParse(values[1].Trim(), out int saves))
                                {
                                    // Update the scoreboard with save data
                                    scoreboard.UpdateGoalieSaves(steamId, shots, saves);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Scoreboard] Error processing save percentage data: {ex}");
                    }
                });
                
                Debug.Log("[Scoreboard] Registered listener for oomtm450_statsBATCHSAVEPERC messages");
            }
            else
            {
                Debug.LogWarning("[Scoreboard] CustomMessagingManager not available - stars will not be detected");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Scoreboard] Failed to register star data listener: {e}");
        }
    }



}
