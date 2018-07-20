﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.Config;
using Quaver.Graphics.Base;
using Quaver.Graphics.Buttons;
using Quaver.Graphics.Buttons.Selection;
using Quaver.Graphics.Sprites;
using Quaver.Graphics.Text;
using Quaver.Main;

namespace Quaver.Graphics.Overlays.Options
{
    internal class OptionsSection
    {
        /// <summary>
        ///    The type of options section.
        /// </summary>
        internal OptionsType Type { get; }

        /// <summary>
        ///     The stringified name of the section.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        ///     The icon displayed for this section.
        /// </summary>
        internal Texture2D Icon { get; }

        /// <summary>
        ///     The container of the options section.
        /// </summary>
        internal Sprite Container { get; }

        /// <summary>
        ///     Probably a bad name, but all of the sprites that are interactable
        ///     (Sliders, Dropdowns, Checkboxes)
        /// </summary>
        private List<Drawable> Interactables { get; }

        /// <summary>
        ///     The y spacing for each options element.
        /// </summary>
        private int SpacingY => Interactables.Count * 50;

        /// <summary>
        ///     Ctor -
        /// </summary>
        internal OptionsSection(OptionsType type, OptionsOverlay overlay, string name, Texture2D icon)
        {
            Type = type;
            Name = name;
            Icon = icon;

            Container = new Sprite()
            {
                Parent = overlay,
                Size = new UDim2D(650, 450),
                Alignment = Alignment.MidCenter,
                PosY = 100,
                Tint = new Color(0f, 0f, 0f, 0f),
                Visible = false
            };

            Interactables = new List<Drawable>();
        }

        /// <summary>
        ///     Adds a slider to the given container
        /// </summary>
        internal void AddSliderOption(BindedInt value, string name)
        {
            AddTextField(name);

            // Create the slider.
            var slider = new Slider(value, new Vector2(380, 3))
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
                PosY = SpacingY + 8,
                Tint = Colors.MainAccentInactive,
                ProgressBall = { Tint = Colors.MainAccentInactive }
            };

            // Make sure the slider's colors get updated accordingly.
            slider.OnUpdate = dt =>
            {
                if (slider.MouseInHoldSequence)
                {
                    slider.Tint = Colors.MainAccent;
                    slider.ProgressBall.Tint = Colors.MainAccent;
                }
                else
                {
                    slider.Tint = Colors.MainAccentInactive;
                    slider.ProgressBall.Tint = Colors.MainAccentInactive;
                }
            };

            Interactables.Add(slider);
        }

        /// <summary>
        ///     Adds a checkbox option
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <param name="onClick"></param>
        internal void AddCheckboxOption(BindedValue<bool> value, string name, EventHandler onClick = null)
        {
            AddTextField(name);

            // Create the checkbox.
            var checkbox = new Checkbox(value, new Vector2(20, 20))
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
                PosY = SpacingY,
                Tint = Colors.MainAccentInactive
            };

            checkbox.Clicked += onClick;
            Interactables.Add(checkbox);
        }

        /// <summary>
        ///     Adds a dropdown option
        /// </summary>
        internal void AddDropdownOption(Dropdown dropdown, string name)
        {
            AddTextField(name);

            dropdown.Parent = Container;
            dropdown.Alignment = Alignment.TopRight;
            dropdown.PosY = SpacingY  + 8;
            dropdown.SizeX = 380;

            Interactables.Add(dropdown);
        }

        /// <summary>
        ///     Adds a single keybind option
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        internal void AddKeybindOption(BindedValue<Keys> value, string name)
        {
            AddTextField(name);

            // Create the keybind button.
            var keybind = new KeybindButton(value, new Vector2(90, 25))
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
                PosY = SpacingY
            };

            Interactables.Add(keybind);
        }

        /// <summary>
        ///     Adds multiple keybind buttons on the same row.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="name"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void AddKeybindOption(List<BindedValue<Keys>> values, string name)
        {
            AddTextField(name);

            for (var i = 0; i < values.Count; i++)
            {
                // Create the keybind button.
                new KeybindButton(values[i], new Vector2(60, 25))
                {
                    Parent = Container,
                    Alignment = Alignment.TopRight,
                    PosY = SpacingY,
                    PosX = (i - values.Count) * 70 + 70
                };
            }

            Interactables.Add(Container.Children.Last());
        }

        /// <summary>
        ///     Adds a button to the list of interactable options menu elements.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="name"></param>
        internal void AddButton(Button button, string name)
        {
            AddTextField(name);

            button.Parent = Container;
            button.Alignment = Alignment.TopRight;
            button.PosY = SpacingY;
            button.SizeX = 200;
            button.SizeY = 30;

            Interactables.Add(button);
        }

        /// <summary>
        ///     Method that adds the text field to the left for each options element.
        /// </summary>
        /// <param name="text"></param>
        private void AddTextField(string text)
        {
            new SpriteText()
            {
                TextAlignment = Alignment.TopLeft,
                Alignment = Alignment.TopLeft,
                Text = text,
                Font = Fonts.Medium12,
                Parent = Container,
                TextBoxStyle = TextBoxStyle.OverflowSingleLine,
                PosY = SpacingY
            };
        }
    }
}