using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using WukongMp.Api;
using WukongMp.Coop.Common;
using WukongMp.Coop.UI;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;
using WukongMp.Sdk.SDK;

namespace WukongMp.Coop.Services;

[Service]
public sealed partial class WidgetUpdates(IEntities entities, IGameEvents gameEvents)
{
    private readonly Lazy<CoopStatusWidget> _coopStatusWidget = new();

    private void Start()
    {
        gameEvents.OnJoinedArea += OnJoinedArea;
        gameEvents.OnLeftArea += OnLeftArea;
        gameEvents.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        gameEvents.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;

        gameEvents.OnLevelLoaded += OnLevelLoaded;
        gameEvents.OnExitLevel += OnExitLevel;
        gameEvents.OnLoadingScreenClose += OnLoadingScreenClose;

        gameEvents.OnPlayerChangedTeam += UpdatePlayerTeam;
        gameEvents.OnLocalPlayerBeforeRebirth += OnLocalPlayerBeforeRebirth;
    }

    private void Stop()
    {
        gameEvents.OnJoinedArea -= OnJoinedArea;
        gameEvents.OnLeftArea -= OnLeftArea;
        gameEvents.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
        gameEvents.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;

        gameEvents.OnLevelLoaded -= OnLevelLoaded;
        gameEvents.OnExitLevel -= OnExitLevel;
        gameEvents.OnLoadingScreenClose -= OnLoadingScreenClose;

        gameEvents.OnPlayerChangedTeam -= UpdatePlayerTeam;
        gameEvents.OnLocalPlayerBeforeRebirth -= OnLocalPlayerBeforeRebirth;
    }

    private void UpdatePlayerTeam(MainCharacter mainCharacter)
    {
        _coopStatusWidget.Value.RemovePlayer(mainCharacter.Nickname.ToString());
        _coopStatusWidget.Value.AddPlayer(mainCharacter.Nickname.ToString());
        RefreshWidgets();
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing co-op widgets");
        _coopStatusWidget.Value.Initialize();
    }

    private void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing co-op widgets");
        _coopStatusWidget.Value.Deinitialize();
    }

    private void OnLoadingScreenClose()
    {
        var isOnGameplayLevel = WukongApi.Entities.InArea;
        WukongApi.Widgets.ShowInGameWidgets(isOnGameplayLevel);

        if (isOnGameplayLevel)
        {
            _coopStatusWidget.Value.SetVisibility(true);
            _coopStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
        }
    }

    private void RefreshWidgets()
    {
        _coopStatusWidget.Value.SetConnectedCount(WukongApi.Entities.AreaPlayers.Count);
        _coopStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
    }

    private void OnLocalPlayerBeforeRebirth()
    {
        WukongApi.Widgets.HideInfoMessage();
    }

    private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area)
    {
        if (entities.TryLookup(playerId, out Player player))
        {
            _coopStatusWidget.Value.AddPlayer(player.Nickname.ToString());
            RefreshWidgets();
        }
        else
        {
            Logging.LogWarning("Player entity for player {PlayerId} not found when they entered area {AreaId}, cannot add to co-op widget", playerId, area);
        }
    }

    private void OnOtherPlayerOutsideArea(PlayerId playerId, AreaId area)
    {
        if (entities.TryLookup(playerId, out Player player))
        {
            _coopStatusWidget.Value.RemovePlayer(player.Nickname.ToString());
            RefreshWidgets();
        }
    }

    private void OnJoinedArea(AreaId area)
    {
        if (WukongApi.Entities.LocalPlayer is {} player)
        {
            _coopStatusWidget.Value.AddPlayer(player.Nickname.ToString());
            RefreshWidgets();
        }
        else
        {
            Logging.LogWarning("Local player entity not found when joining area {AreaId}, cannot add to co-op widget", area);
        }
    }

    private void OnLeftArea(AreaId area)
    {
        if (WukongApi.Entities.LocalPlayer is {} player)
        {
            _coopStatusWidget.Value.RemovePlayer(player.Nickname.ToString());
            RefreshWidgets();
        }
    }
}