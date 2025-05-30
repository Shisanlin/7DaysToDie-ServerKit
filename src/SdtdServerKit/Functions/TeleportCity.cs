﻿using SdtdServerKit.Data.Entities;
using SdtdServerKit.Data.IRepositories;
using SdtdServerKit.FunctionSettings;
using SdtdServerKit.Managers;
using SdtdServerKit.Variables;

namespace SdtdServerKit.Functions
{
    /// <summary>
    /// 城市传送
    /// </summary>
    public class TeleportCity : FunctionBase<TeleportCitySettings>
    {
        private readonly ICityLocationRepository _cityLocationRepository;
        private readonly IPointsInfoRepository _pointsInfoRepository;
        private readonly ITeleRecordRepository _teleRecordRepository;
        /// <inheritdoc/>
        public TeleportCity(
            ICityLocationRepository cityLocationRepository,
            IPointsInfoRepository pointsInfoRepository,
            ITeleRecordRepository teleRecordRepository)
        {
            _pointsInfoRepository = pointsInfoRepository;
            _teleRecordRepository = teleRecordRepository;
            _cityLocationRepository = cityLocationRepository;
        }
        /// <inheritdoc/>
        protected override async Task<bool> OnChatCmd(string message, ManagedPlayer managedPlayer)
        {
            if (string.Equals(message, Settings.QueryListCmd, StringComparison.OrdinalIgnoreCase))
            {
                string playerId = managedPlayer.PlayerId;
                var cityPositions = await _cityLocationRepository.GetAllAsync();

                if (cityPositions.Any() == false)
                {
                    SendMessageToPlayer(playerId, Settings.NoLocation);
                }
                else
                {
                    foreach (var item in cityPositions)
                    {
                        SendMessageToPlayer(playerId, FormatCmd(Settings.LocationItemTip, managedPlayer, item));
                    }
                }

                return true;
            }
            else if (message.StartsWith(Settings.TeleCmdPrefix + ConfigManager.GlobalSettings.ChatCommandSeparator, StringComparison.OrdinalIgnoreCase))
            {
                int cityId = message.Substring(Settings.TeleCmdPrefix.Length + ConfigManager.GlobalSettings.ChatCommandSeparator.Length).ToInt(-1);
                var cityPosition = await _cityLocationRepository.GetByIdAsync(cityId);
                if (cityPosition == null)
                {
                    return false;
                }
                else
                {
                    string playerId = managedPlayer.PlayerId;

                    var teleRecord = await _teleRecordRepository.GetNewestAsync(playerId, TeleTargetType.City);
                    if (teleRecord != null)
                    {
                        int timeSpan = (int)(DateTime.Now - teleRecord.CreatedAt).TotalSeconds;
                        if (timeSpan < Settings.TeleInterval) // 正在冷却
                        {
                            SendMessageToPlayer(playerId, FormatCmd(Settings.CoolingTip, managedPlayer, cityPosition, Settings.TeleInterval - timeSpan));

                            return true;
                        }
                    }

                    int pointsCount = await _pointsInfoRepository.GetPointsByIdAsync(playerId);
                    if (pointsCount < cityPosition.PointsRequired) // 积分不足
                    {
                        SendMessageToPlayer(playerId, FormatCmd(Settings.PointsNotEnoughTip, managedPlayer, cityPosition));
                    }
                    else
                    {
                        if(ConfigManager.GlobalSettings.TeleZombieCheck &&
                            GameManager.Instance.World.Players.dict.TryGetValue(managedPlayer.EntityId, out EntityPlayer player))
                        {
                            if (Utilities.Utils.ZombieCheck(player))
                            {
                                SendMessageToPlayer(playerId, ConfigManager.GlobalSettings.TeleDisableTip);
                                return true;
                            }
                        }

                        await _pointsInfoRepository.ChangePointsAsync(playerId, -cityPosition.PointsRequired);
                        Utilities.Utils.TeleportPlayer(managedPlayer.EntityId.ToString(), cityPosition.Position, cityPosition.ViewDirection);
                        SendMessageToPlayer(managedPlayer.PlayerId, FormatCmd(Settings.TeleSuccessTip, managedPlayer, cityPosition));
                        
                        await _teleRecordRepository.InsertAsync(new T_TeleRecord()
                        {
                            CreatedAt = DateTime.Now,
                            PlayerId = playerId,
                            PlayerName = managedPlayer.PlayerName,
                            OriginPosition = Utilities.Utils.GetPlayerPosition(managedPlayer.EntityId).ToString(),
                            TargetPosition = cityPosition.Position,
                            TargetType = TeleTargetType.City.ToString(),
                            TargetName = cityPosition.CityName
                        });

                        CustomLogger.Info("Player: {0}, entityId: {1}, teleported to: {2}", managedPlayer.PlayerName, managedPlayer.EntityId, cityPosition.CityName);
                    }
                }

                return true;
            }

            return false;
        }

        private string FormatCmd(string message, ManagedPlayer player, T_CityLocation position, int cooldownSeconds = 0)
        {
            return StringTemplate.Render(message, new TeleportCityVariables()
            {
                EntityId = player.EntityId,
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                CityId = position.Id,
                CityName = position.CityName,
                TeleInterval = Settings.TeleInterval,
                PointsRequired = position.PointsRequired,
                CooldownSeconds = cooldownSeconds,
            });
        }
    }
}