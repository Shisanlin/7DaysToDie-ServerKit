﻿using SdtdServerKit.FunctionSettings;
using SdtdServerKit.Managers;
using System.IO.Compression;

namespace SdtdServerKit.Functions
{
    /// <summary>
    /// AutoBackup
    /// </summary>
    public class AutoBackup : FunctionBase<AutoBackupSettings>
    {
        private readonly SubTimer _timer;
        private DateTime _lastServerStateChange = DateTime.Now;

        /// <inheritdoc/>
        public AutoBackup()
        {
            _timer = new SubTimer(TryBackup);
        }

        /// <inheritdoc/>
        protected override void OnDisableFunction()
        {
            GlobalTimer.UnregisterSubTimer(_timer);
            ModEventHub.PlayerSpawnedInWorld -= OnPlayerSpawnedInWorld;
            ModEventHub.PlayerDisconnected -= OnPlayerDisconnected;
            ModEventHub.GameStartDone -= OnGameStartDone;
        }

        private void OnGameStartDone()
        {
            if (Settings.IsEnabled && Settings.AutoBackupOnServerStartup)
            {
                ExecuteInternal();
            }
        }

        /// <inheritdoc/>
        protected override void OnEnableFunction()
        {
            GlobalTimer.RegisterSubTimer(_timer);
            ModEventHub.PlayerSpawnedInWorld += OnPlayerSpawnedInWorld;
            ModEventHub.PlayerDisconnected += OnPlayerDisconnected;
            ModEventHub.GameStartDone += OnGameStartDone;
        }

        /// <inheritdoc/>
        protected override void OnSettingsChanged()
        {
            _timer.Interval = Settings.Interval;
            _timer.IsEnabled = Settings.IsEnabled;
        }
        private void OnPlayerDisconnected(ManagedPlayer player)
        {
            _lastServerStateChange = DateTime.Now;
        }

        private void OnPlayerSpawnedInWorld(SpawnedPlayer spawnedPlayer)
        {
            try
            {
                switch (spawnedPlayer.RespawnType)
                {
                    // New player spawning
                    case Models.RespawnType.EnterMultiplayer:
                    // Old player spawning
                    case Models.RespawnType.JoinMultiplayer:
                        _lastServerStateChange = DateTime.Now;
                        break;
                }
            }
            catch (Exception ex)
            {
                CustomLogger.Warn(ex, "Error in AutoBackup.PlayerSpawnedInWorld");
            }
        }

        private void TryBackup()
        {
            try
            {
                if(ModApi.IsGameStartDone == false)
                {
                    return;
                }

                DateTime now = DateTime.Now;
                if(Settings.SkipIfThereAreNoPlayers && LivePlayerManager.Count == 0 && (now - _lastServerStateChange).TotalSeconds > Settings.Interval)
                {
                    CustomLogger.Info("AutoBackup: Skipped because there are no players.");
                    return;
                }

                ExecuteInternal();
            }
            catch (Exception ex)
            {
                CustomLogger.Warn(ex, "Error in AutoBackup.TryBackup");
            }
        }

        private void ExecuteInternal()
        {
            try
            {
                string backupSrcPath = GameIO.GetSaveGameDir();
                string backupDestPath = Settings.ArchiveFolder;
                if (Path.IsPathRooted(backupDestPath) == false)
                {
                    backupDestPath = Path.Combine(AppContext.BaseDirectory, Settings.ArchiveFolder);
                }
                
                Directory.CreateDirectory(backupDestPath);

                // 服务端版本、游戏世界、游戏名称、游戏时间
                string serverVersion = global::Constants.cVersionInformation.LongString.Replace('_', ' ');
                string gameWorld = GamePrefs.GetString(EnumGamePrefs.GameWorld).Replace('_', ' ');
                string gameName = GamePrefs.GetString(EnumGamePrefs.GameName).Replace('_', ' ');

                var worldTime = GameManager.Instance.World.GetWorldTime();
                int days = GameUtils.WorldTimeToDays(worldTime);
                int hours = GameUtils.WorldTimeToHours(worldTime);
                int minutes = GameUtils.WorldTimeToMinutes(worldTime);

                string title = $"{serverVersion}_{gameWorld}_{gameName}_Day{days}_Hour{hours}";
                string archiveFileName = Path.Combine(backupDestPath, $"{title}.zip");

                if (File.Exists(archiveFileName))
                {
                    CustomLogger.Info("AutoBackup: Backup already exists: {0}", archiveFileName);
                    return;
                }

                ZipFile.CreateFromDirectory(backupSrcPath, archiveFileName, System.IO.Compression.CompressionLevel.Optimal, true);
                CustomLogger.Info("AutoBackup: Backup created: {0}", archiveFileName);

                if (Settings.RetainedFileCountLimit > 0)
                {
                    string[] files = Directory.GetFiles(backupDestPath, "*.zip");
                    int count = files.Length - Settings.RetainedFileCountLimit;
                    if (count > 0)
                    {
                        // 根据文件的创建日期对文件进行排序
                        var oldestFiles = files.Select(i => new FileInfo(i)).OrderBy(f => f.CreationTime).Take(count);
                        foreach (var oldestFile in oldestFiles)
                        {
                            CustomLogger.Info("AutoBackup: Deleting file: {0}, CreatedAt: {1}", oldestFile.Name, oldestFile.CreationTime);
                            // 删除最旧的文件
                            oldestFile.Delete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomLogger.Warn(ex, "Error in AutoBackup.ExecuteInternal");
            }
        }

        /// <summary>
        /// 手动备份
        /// </summary>
        public void ManualBackup()
        {
            ExecuteInternal();
            if(Settings.ResetIntervalAfterManualBackup)
            {
                _timer.IsEnabled = false;
                _timer.IsEnabled = Settings.IsEnabled;
            }
        }

    }
}