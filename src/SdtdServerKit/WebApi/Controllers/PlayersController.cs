﻿using SdtdServerKit.Data.IRepositories;
using SdtdServerKit.Managers;
using SdtdServerKit.Models;
using Webserver.WebAPI.APIs.WorldState;

namespace SdtdServerKit.WebApi.Controllers
{
    /// <summary>
    /// Players
    /// </summary>
    [Authorize]
    [RoutePrefix("api")]
    public class PlayersController : ApiController
    {
        #region OnlinePlayers
        /// <summary>
        /// 获取所有在线玩家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("OnlinePlayers")]
        public async Task<IEnumerable<OnlinePlayer>> GetOnlinePlayers()
        {
            var onlinePlayers = OnlinePlayerManager.GetAll();

            var pointsInfoRepository = ModApi.ServiceContainer.Resolve<IPointsInfoRepository>();
            var pointsMap = await pointsInfoRepository.GetPointsByIdsAsync(onlinePlayers.Select(i => i.PlayerId));
            foreach (var item in onlinePlayers)
            {
                if (pointsMap.TryGetValue(item.PlayerId, out int count))
                {
                    item.PlayerDetails.PointsCount = count;
                }
            }
            return onlinePlayers;
        }

        /// <summary>
        /// 获取指定在线玩家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("OnlinePlayers/{entityId:int}")]
        [ResponseType(typeof(OnlinePlayer))]
        public async Task<IHttpActionResult> GetOnlinePlayer(int entityId)
        {
            if (OnlinePlayerManager.TryGetByEntityId(entityId, out var onlinePlayer))
            {
                onlinePlayer!.PlayerDetails.PointsCount = await ModApi.ServiceContainer.Resolve<IPointsInfoRepository>().GetPointsByIdAsync(onlinePlayer.PlayerId);
                return Ok(onlinePlayer);
            }

            return NotFound();
        }

        /// <summary>
        /// 获取指定在线玩家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("OnlinePlayers/{playerId}")]
        [ResponseType(typeof(OnlinePlayer))]
        public async Task<IHttpActionResult> GetOnlinePlayer(string playerId)
        {
            if (OnlinePlayerManager.TryGetByPlayerId(playerId, out var onlinePlayer))
            {
                onlinePlayer!.PlayerDetails.PointsCount = await ModApi.ServiceContainer.Resolve<IPointsInfoRepository>().GetPointsByIdAsync(playerId);
                return Ok(onlinePlayer);
            }

            return NotFound();
        }

        #endregion

        #region HistoryPlayers
        /// <summary>
        /// 分页获取历史玩家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("HistoryPlayers")]
        public async Task<Paged<HistoryPlayer>> GetHistoryPlayers([FromUri]PaginationQuery<HistoryPlayerQueryOrder> model)
        {
            var persistentPlayerMap = GameManager.Instance.persistentPlayers.Players;
            int total = persistentPlayerMap.Count;
            IEnumerable<PersistentPlayerData> persistentPlayers = persistentPlayerMap.Values;

            string? keyword = model.Keyword;
            if (string.IsNullOrEmpty(keyword) == false)
            {
                total = 0;
                var filterByKeyword = new List<PersistentPlayerData>();
                foreach (var item in persistentPlayerMap.Values)
                {
                    if (string.Equals(item.PrimaryId.CombinedString, keyword, StringComparison.OrdinalIgnoreCase) 
                        || string.Equals(item.NativeId.CombinedString, keyword, StringComparison.OrdinalIgnoreCase)
                        || item.PlayerName.DisplayName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        total += 1;
                        filterByKeyword.Add(item);
                    }
                }

                persistentPlayers = filterByKeyword;
            }

            IEnumerable<HistoryPlayer> historyPlayers = new List<HistoryPlayer>();
            foreach (var item in persistentPlayers)
            {
                var historyPlayer = new HistoryPlayer(item);
                ((List<HistoryPlayer>)historyPlayers).Add(historyPlayer);
            }

            switch (model.Order)
            {
                case HistoryPlayerQueryOrder.PlayerName:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerName) : historyPlayers.OrderBy(k => k.PlayerName);
                    break;
                case HistoryPlayerQueryOrder.Level:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.Progression.Level) : historyPlayers.OrderBy(k => k.PlayerDetails.Progression.Level);
                    break;
                case HistoryPlayerQueryOrder.IsOffline:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.IsOffline) : historyPlayers.OrderBy(k => k.IsOffline);
                    break;
                case HistoryPlayerQueryOrder.ZombieKills:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.ZombieKills) : historyPlayers.OrderBy(k => k.PlayerDetails.ZombieKills);
                    break;
                case HistoryPlayerQueryOrder.PlayerKills:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.PlayerKills) : historyPlayers.OrderBy(k => k.PlayerDetails.PlayerKills);
                    break;
                case HistoryPlayerQueryOrder.Deaths:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.Deaths) : historyPlayers.OrderBy(k => k.PlayerDetails.Deaths);
                    break;
                case HistoryPlayerQueryOrder.SkillPoints:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.Progression.SkillPoints) : historyPlayers.OrderBy(k => k.PlayerDetails.Progression.SkillPoints);
                    break;
                case HistoryPlayerQueryOrder.LastLogin:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.LastLogin) : historyPlayers.OrderBy(k => k.PlayerDetails.LastLogin);
                    break;
                case HistoryPlayerQueryOrder.TotalTimePlayed:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.TotalTimePlayed) : historyPlayers.OrderBy(k => k.PlayerDetails.TotalTimePlayed);
                    break;
                case HistoryPlayerQueryOrder.LongestLife:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.PlayerDetails.LongestLife) : historyPlayers.OrderBy(k => k.PlayerDetails.LongestLife);
                    break;
                case HistoryPlayerQueryOrder.EntityId:
                    historyPlayers = model.Desc ? historyPlayers.OrderByDescending(k => k.EntityId) : historyPlayers.OrderBy(k => k.EntityId);
                    break;
                default:
                    break;
            }
            
            int pageSize = model.PageSize;
            if(pageSize > 0)
            {
                historyPlayers = historyPlayers.Skip((model.PageNumber - 1) * pageSize).Take(pageSize);
            }

            var pointsInfoRepository = ModApi.ServiceContainer.Resolve<IPointsInfoRepository>();
            var pointsMap = await pointsInfoRepository.GetPointsByIdsAsync(historyPlayers.Select(i => i.PlayerId));
            foreach (var item in historyPlayers)
            {
                if(pointsMap.TryGetValue(item.PlayerId, out int count))
                {
                    item.PlayerDetails.PointsCount = count;
                }
            }

            return new Paged<HistoryPlayer>(historyPlayers, total);
        }

        /// <summary>
        /// 获取指定历史玩家
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("HistoryPlayers/{playerId}")]
        [ResponseType(typeof(HistoryPlayer))]
        public async Task<IHttpActionResult> GetHistoryPlayer(string playerId)
        {
            var userId = PlatformUserIdentifierAbs.FromCombinedString(playerId);
            if (userId == null)
            {
                return NotFound();
            }

            var persistentPlayerMap = GameManager.Instance.GetPersistentPlayerList().Players;
            if(persistentPlayerMap.TryGetValue(userId, out var persistentPlayerData) == false)
            {
                return NotFound();
            }

            var historyPlayer = new HistoryPlayer(persistentPlayerData);
            historyPlayer.PlayerDetails.PointsCount = await ModApi.ServiceContainer.Resolve<IPointsInfoRepository>().GetPointsByIdAsync(playerId);
            return Ok(historyPlayer);
        }

        #endregion
    }
}