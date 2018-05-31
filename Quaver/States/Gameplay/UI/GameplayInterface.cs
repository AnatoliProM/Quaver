﻿using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Quaver.API.Maps;
using Quaver.Audio;
using Quaver.Config;
using Quaver.GameState;
using Quaver.Graphics;
using Quaver.Graphics.Base;
using Quaver.Graphics.Sprites;
using Quaver.Graphics.Text;
using Quaver.Graphics.UserInterface;
using Quaver.Helpers;
using Quaver.Main;
using Quaver.States.Gameplay.UI.Components;
using Quaver.States.Gameplay.UI.Components.Judgements;
using Quaver.States.Gameplay.UI.Components.Pause;

namespace Quaver.States.Gameplay.UI
{
    internal class GameplayInterface : IGameStateComponent
    {
        /// <summary>
        ///     Reference to the gameplay screen itself.
        /// </summary>
        private GameplayScreen Screen { get; }
        
        /// <summary>
        ///     Contains general purpose stuff for this screen such as the following:
        ///         - Score/Accuracy Display
        ///         - Leaderboard display
        ///         - Song progress time display
        /// </summary>
        private Container Container { get; }

        /// <summary>
        ///     The progress bar for the song time.
        /// </summary>
        private SongTimeProgressBar SongTimeProgressBar { get; set;  }

        /// <summary>
        ///     The display for score.
        /// </summary>
        private NumberDisplay ScoreDisplay { get; set; }

        /// <summary>
        ///     The display for accuracy.
        /// </summary>
        private NumberDisplay AccuracyDisplay { get; set; }

        /// <summary>
        ///     Displays the judgements and KPS if specified.
        /// </summary>
        private JudgementStatusDisplay JudgementStatusDisplay { get; set; }

        /// <summary>
        ///     The sprite used solely to fade the screen with transitions.
        /// </summary>
        internal Sprite ScreenTransitioner { get; set; }

        /// <summary>
        ///     The keys per second display.
        /// </summary>
        internal KeysPerSecond KpsDisplay { get; set; }

        /// <summary>
        ///     Sprite that displays the grade next to the accuracy display
        /// </summary>
        internal GradeDisplay GradeDisplay { get; set;  }

        /// <summary>
        ///     Overlay that's shown when paused.
        /// </summary>
        internal PauseOverlay PauseOverlay { get; set; }

        /// <summary>
        ///     If the volume has already been set to fade out.
        /// </summary>
        private bool VolumeFadedOut { get; set; }

        /// <summary>
        ///     The time counter where the game should start fading out.
        /// </summary>
        private double GameShouldFadeOutTime { get; set; }

        /// <summary>
        ///     The amount of time for the overlay to fade in/out
        /// </summary>
        internal int PauseFadeTimeScale { get; } = 120;

        /// <summary>
        ///     Ctor -
        /// </summary>
        internal GameplayInterface(GameplayScreen screen)
        {
            Screen = screen;
            Container = new Container();
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void Initialize(IGameState state)
        {
            // Initialize the progress bar if the user has it set in config.
            if (ConfigManager.DisplaySongTimeProgress.Value)
                SongTimeProgressBar = new SongTimeProgressBar(Qua.FindSongLength(GameBase.SelectedMap.Qua), 0, new Vector2(GameBase.WindowRectangle.Width, 6),
                                                            Container, Alignment.BotLeft);

            // Create score display
            ScoreDisplay = new NumberDisplay(NumberDisplayType.Score, StringHelper.ScoreToString(0), new Vector2(1, 1))
            {
                Parent = Container,
                Alignment = Alignment.TopRight
            };

            // Put the display in the top right corner.
            ScoreDisplay.PosX = -ScoreDisplay.TotalWidth + GameBase.LoadedSkin.ScoreDisplayPosX;
            ScoreDisplay.PosY = GameBase.LoadedSkin.ScoreDisplayPosY;
            
            // Create acc display
            AccuracyDisplay = new NumberDisplay(NumberDisplayType.Accuracy, StringHelper.AccuracyToString(0), new Vector2(1.5f, 1.5f))
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
            };
            
            // Set the position of the accuracy display.
            AccuracyDisplay.PosX = -AccuracyDisplay.TotalWidth + GameBase.LoadedSkin.AccuracyDisplayPosX;
            AccuracyDisplay.PosY = ScoreDisplay.Digits[0].SizeY + GameBase.LoadedSkin.AccuracyDisplayPosY;
            
            // Create judgement status display
            JudgementStatusDisplay = new JudgementStatusDisplay(Screen) { Parent = Container };
           
            // Create KPS display
            KpsDisplay = new KeysPerSecond(NumberDisplayType.Score, "0", new Vector2(1.75f, 1.75f))
            {
                Parent = Container,
                Alignment = Alignment.TopRight
            };
            
            // Set the position of the KPS display
            KpsDisplay.PosX = -KpsDisplay.TotalWidth + GameBase.LoadedSkin.KpsDisplayPosX;
            KpsDisplay.PosY = AccuracyDisplay.PosY + AccuracyDisplay.Digits[0].SizeY + GameBase.LoadedSkin.KpsDisplayPosY;

            GradeDisplay = new GradeDisplay(Screen.Ruleset.ScoreProcessor)
            {
                Parent = Container,
                Size = new UDim2D(AccuracyDisplay.Digits[0].SizeX, AccuracyDisplay.Digits[0].SizeY),
                Alignment = Alignment.TopRight,
                Position = new UDim2D(GetGradeDisplayPosX(), AccuracyDisplay.PosY)
            };
            
            // Initialize the failure trannsitioner. 
            ScreenTransitioner = new Sprite()
            {
                Parent = Container,
                ScaleX = 1,
                ScaleY = 1,
                Tint = Color.Black,
                Alpha = 1
            };

            PauseOverlay = new PauseOverlay(Screen) {Parent = Container};
        }

        /// <summary>
        /// 
        /// </summary>
        public void UnloadContent()
        {
            Container.Destroy();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void Update(double dt)
        {
            // Hide navbar in gameplay
            if (!Screen.IsPaused && !Screen.Failed)
                GameBase.Navbar.PerformHideAnimation(dt);

            // Fade the cursor depending on if the user is paused or not.
            if (Screen.IsPaused && !Screen.IsResumeInProgress)
                GameBase.Cursor.FadeIn(dt, 240);
            else
                GameBase.Cursor.FadeOut(dt, 240);
                
            UpdateSongProgressDisplay();
            UpdateScoreAndAccuracyDisplays();
            
            // Change grade display posX
            GradeDisplay.PosX = GetGradeDisplayPosX();
            
            HandlePlayCompletion(dt);
            HandlePause(dt);
            FadeInScreen(dt);
                    
            Container.Update(dt);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Draw()
        {
            Container.Draw();
        }
        
        /// <summary>
        ///     Updates the number displays
        ///     Score and Accuracy
        /// </summary>
        private void UpdateScoreAndAccuracyDisplays()
        {
            // Update score and accuracy displays
            ScoreDisplay.Value = StringHelper.ScoreToString(Screen.Ruleset.ScoreProcessor.Score);

            // Grab the old accuracy
            var oldAcc = AccuracyDisplay.Value;
            
            // Update the new accuracy.
            AccuracyDisplay.Value = StringHelper.AccuracyToString(Screen.Ruleset.ScoreProcessor.Accuracy);
            
            // If the old accuracy's length isn't the same, then we need to reposition the sprite
            // Example: 100.00% to 99.99% needs repositioning.
            if (oldAcc.Length != AccuracyDisplay.Value.Length)
                AccuracyDisplay.PosX = -AccuracyDisplay.TotalWidth + GameBase.LoadedSkin.AccuracyDisplayPosX;
        }

        /// <summary>
        ///     Updates the song progress display.
        /// </summary>
        private void UpdateSongProgressDisplay()
        {
            // Update the current value of the song time progress bar if it is actually initialized
            // and the user wants to actually display it.
            if (ConfigManager.DisplaySongTimeProgress.Value && SongTimeProgressBar != null)
                SongTimeProgressBar.CurrentValue = (float) Screen.Timing.CurrentTime;
        }

        /// <summary>
        ///     Handles the fadeout with failure/play completion.
        /// </summary>
        private void HandlePlayCompletion(double dt)
        {            
            if (!Screen.Failed && !Screen.IsPlayComplete)
                return;

            // Wait a bit before actually fading out the game.
            GameShouldFadeOutTime += dt;
            if (GameShouldFadeOutTime <= 800)
                return;
            
            Screen.Ruleset.Playfield.HandleFailure(dt);
            
            // Pause the audio
            if (GameBase.AudioEngine.IsPlaying && !VolumeFadedOut)
            {
                VolumeFadedOut = true;
                AudioEngine.Fade(0, 1800);
            }
            
            ScreenTransitioner.FadeIn(dt, 240);
        }

        /// <summary>
        ///     Handle pausing & unpausing UI.
        /// </summary>
        /// <param name="dt"></param>
        private void HandlePause(double dt)
        {
            if (!Screen.IsPaused)
                return;
       
            if (Screen.IsResumeInProgress)
                ScreenTransitioner.Fade(dt, 0, PauseFadeTimeScale * 2f);
            else
                ScreenTransitioner.Fade(dt, 0.90f, PauseFadeTimeScale);            
        }

        /// <summary>
        ///     Fades in the screen ready for gameplay.
        /// </summary>
        /// <param name="dt"></param>
        private void FadeInScreen(double dt)
        {
            if (!Screen.IsPaused && !Screen.IsResumeInProgress && !Screen.IsRestartingPlay && !Screen.Failed && !Screen.IsPlayComplete)
                ScreenTransitioner.FadeOut(dt, 720);
        }

        /// <summary>
        ///     F
        /// </summary>
        /// <returns></returns>
        private float GetGradeDisplayPosX() => AccuracyDisplay.PosX - 8;
    }
}