using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quaver.Server.Client;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Select.UI.Leaderboard;
using Quaver.Shared.Screens.Selection.UI.Leaderboard.Components;
using Quaver.Shared.Screens.Selection.UI.Leaderboard.Rankings;
using WebSocketSharp;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Scheduling;
using Logger = Wobble.Logging.Logger;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard
{
    public class LeaderboardContainer : Sprite, ILoadable
    {
        /// <summary>
        ///     Displays "LEADERBOARD"
        /// </summary>
        private SpriteTextPlus Header { get; set; }

        /// <summary>
        ///     Allows the user to select between different leaderboard types
        /// </summary>
        private LeaderboardTypeDropdown TypeDropdown { get; set; }

        /// <summary>
        ///     The background for <see cref="ScoresContainer"/>
        /// </summary>
        public Sprite ScoresContainerBackground { get; private set; }

        /// <summary>
        ///     Displays the scores of the leaderboard
        /// </summary>
        public LeaderboardScoresContainer ScoresContainer { get; set; }

        /// <summary>
        ///     A header above the user's personal best score
        /// </summary>
        private SpriteTextPlus PersonalBestHeader { get; set; }

        /// <summary>
        ///     Displays the user's personal best score for the leaderboard section
        /// </summary>
        private LeaderboardPersonalBestScore PersonalBestScore { get; set; }

        /// <summary>
        ///     Task that's ran when fetching for leaderboard scores
        /// </summary>
        public TaskHandler<Map, FetchedScoreStore> FetchScoreTask { get; }

        /// <summary>
        /// </summary>
        public LeaderboardContainer()
        {
            Size = new ScalableVector2(564, 838);
            Alpha = 0f;

            FetchScoreTask = new TaskHandler<Map, FetchedScoreStore>(FetchScores);
            FetchScoreTask.OnCompleted += OnFetchedScores;

            CreateHeaderText();
            CreateRankingDropdown();
            CreateScoresContainer();
            CreatePersonalBestHeader();
            CreatePersonalBestScore();

            ListHelper.Swap(Children, Children.IndexOf(TypeDropdown), Children.IndexOf(ScoresContainerBackground));

            MapManager.Selected.ValueChanged += OnMapChanged;

            if (ConfigManager.LeaderboardSection != null)
                ConfigManager.LeaderboardSection.ValueChanged += OnLeaderboardSectionChanged;

            ModManager.ModsChanged += OnModsChanged;
            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;

            FetchScores();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            FetchScoreTask?.Dispose();

            // ReSharper disable once DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;

            if (ConfigManager.LeaderboardSection != null)
            {
                // ReSharper disable once DelegateSubtraction
                ConfigManager.LeaderboardSection.ValueChanged -= OnLeaderboardSectionChanged;
            }

            ModManager.ModsChanged -= OnModsChanged;

            // ReSharper disable once DelegateSubtraction
            OnlineManager.Status.ValueChanged -= OnConnectionStatusChanged;

            base.Destroy();
        }

        /// <summary>
        ///    Creates <see cref="Header"/>
        /// </summary>
        private void CreateHeaderText()
        {
            Header = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.LatoHeavy), "LEADERBOARD", 30)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
            };
        }

        /// <summary>
        ///     Creates <see cref="TypeDropdown"/>
        /// </summary>
        private void CreateRankingDropdown()
        {
            TypeDropdown = new LeaderboardTypeDropdown
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = Header.Y / 2f
            };
        }

        /// <summary>
        ///     Creates <see cref="ScoresContainer"/>
        /// </summary>
        private void CreateScoresContainer()
        {
            ScoresContainerBackground = new Sprite()
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Y = Header.Y + Header.Height + 8,
                Size = new ScalableVector2(Width,664),
                Image = UserInterface.LeaderboardScoresPanel
            };

            ScoresContainer = new LeaderboardScoresContainer(this)
            {
                Parent = ScoresContainerBackground,
                Alignment = Alignment.MidCenter
            };
        }

        /// <summary>
        ///     Creates <see cref="PersonalBestHeader"/>
        /// </summary>
        private void CreatePersonalBestHeader()
        {
            PersonalBestHeader = new SpriteTextPlus(Header.Font, "PERSONAL BEST", Header.FontSize)
            {
                Parent = this,
                Y = ScoresContainerBackground.Y + ScoresContainerBackground.Height + 28
            };
        }

        /// <summary>
        ///     Creates <see cref="PersonalBestScore"/>
        /// </summary>
        private void CreatePersonalBestScore()
        {
            PersonalBestScore = new LeaderboardPersonalBestScore(this)
            {
                Parent = this,
                Y = PersonalBestHeader.Y + PersonalBestHeader.Height + 6
            };
        }

        /// <summary>
        ///     Initiates a task to fetch scores for the selected map
        /// </summary>
        public void FetchScores()
        {
            StopLoading();
            FetchScoreTask.Run(MapManager.Selected.Value, 400);
            StartLoading();
        }

        /// <summary>
        ///     Fetches scores for the passed in map
        /// </summary>
        /// <returns></returns>
        private FetchedScoreStore FetchScores(Map map, CancellationToken token)
        {
            switch (ConfigManager.LeaderboardSection.Value)
            {
                case LeaderboardType.Local:
                    return new ScoreFetcherLocal().Fetch(map);
                case LeaderboardType.Global:
                    return new ScoreFetcherGlobal().Fetch(map);
                case LeaderboardType.Mods:
                    return new ScoreFetcherMods().Fetch(map);
                case LeaderboardType.Country:
                    return new ScoreFetcherCountry().Fetch(map);
                default:
                    return new FetchedScoreStore();
            }
        }

        /// <summary>
        ///     Called when having successfully fetched scores
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFetchedScores(object sender, TaskCompleteEventArgs<Map, FetchedScoreStore> e)
        {
            Logger.Important($"Fetched {e.Result.Scores.Count} {ConfigManager.LeaderboardSection.Value} scores for map: {e.Input} | " +
                             $"Has PB: {e.Result.PersonalBest != null}", LogType.Runtime);

            StopLoading();

            Children.ForEach(x =>
            {
                if (x is IFetchedScoreHandler handler)
                    handler.HandleFetchedScores(e.Input, e.Result);
            });

            ScoresContainer.HandleFetchedScores(e.Input, e.Result);
        }

        /// <summary>
        ///     Called when the selected map has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapChanged(object sender, BindableValueChangedEventArgs<Map> e) => FetchScores();

        /// <summary>
        ///     Called when the user changes the selected leaderboard section
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLeaderboardSectionChanged(object sender, BindableValueChangedEventArgs<LeaderboardType> e) => FetchScores();

        /// <summary>
        ///     Called when the user selects new mods while their leaderboard section is selected mods
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object sender, ModsChangedEventArgs e)
        {
            if (ConfigManager.LeaderboardSection == null || ConfigManager.LeaderboardSection.Value != LeaderboardType.Mods)
                return;

            FetchScores();
        }

        /// <summary>
        /// </summary>
        public void StartLoading() => Children.ForEach(x =>
        {
            if (x is ILoadable loadable)
                loadable.StartLoading();

            ScoresContainer.StartLoading();
        });

        /// <summary>
        /// </summary>
        public void StopLoading() => Children.ForEach(x =>
        {
            if (x is ILoadable loadable)
                loadable.StopLoading();

            ScoresContainer.StopLoading();
        });

        /// <summary>
        ///     Whenever the user connects to the server in song select, it will automatically
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnectionStatusChanged(object sender, BindableValueChangedEventArgs<ConnectionStatus> e)
        {
            if (e.Value != ConnectionStatus.Connected || ConfigManager.LeaderboardSection.Value == LeaderboardType.Local)
                return;

            FetchScores();
        }
    }
}