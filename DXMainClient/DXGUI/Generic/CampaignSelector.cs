﻿using ClientCore;
using Microsoft.Xna.Framework;
using System;
using DTAClient.Domain;
using System.IO;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using Updater;
using DTAClient.Domain.Singleplayer;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// A battle (as in Battle.ini entry) list box that can
    /// draw difficulty rank icons on its items.
    /// </summary>
    public class BattleListBox : XNAListBox
    {
        public BattleListBox(WindowManager windowManager) : base(windowManager)
        {
        }

        private Texture2D[] rankTextures;

        public override void Initialize()
        {
            base.Initialize();

            rankTextures = new Texture2D[]
            {
                AssetLoader.LoadTexture("rankEasy.png"),
                AssetLoader.LoadTexture("rankNormal.png"),
                AssetLoader.LoadTexture("rankHard.png")
            };
        }

        private Texture2D DifficultyRankToTexture(DifficultyRank rank)
        {
            if (rank == DifficultyRank.NONE)
                return null;

            return rankTextures[(int)rank - 1];
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            int height = 2;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];

                if (height > Height)
                    break;

                int x = TextBorderDistance;

                if (i == SelectedIndex)
                {
                    int drawnWidth;

                    if (DrawSelectionUnderScrollbar || !ScrollBar.IsDrawn() || !EnableScrollbar)
                    {
                        drawnWidth = Width - 2;
                    }
                    else
                    {
                        drawnWidth = Width - 2 - ScrollBar.Width;
                    }

                    FillRectangle(new Rectangle(1, height, drawnWidth,
                        lbItem.TextLines.Count * LineHeight),
                        FocusColor);
                }

                int textureYPosition = 0;

                if (lbItem.Texture != null)
                {
                    int textureHeight = lbItem.Texture.Height;
                    int textureWidth = lbItem.Texture.Width;

                    if (lbItem.Texture.Height > LineHeight)
                    {
                        double scaleRatio = textureHeight / (double)LineHeight;
                        textureHeight = LineHeight;
                        textureWidth = (int)(textureWidth / scaleRatio);
                    }
                    else
                        textureYPosition = (LineHeight - textureHeight) / 2;

                    DrawTexture(lbItem.Texture,
                        new Rectangle(x, height + textureYPosition,
                        textureWidth, textureHeight), Color.White);

                    x += textureWidth + 2;
                }

                var mission = (Mission)lbItem.Tag;
                Texture2D rankTexture = DifficultyRankToTexture(mission.Rank);
                if (rankTexture != null)
                {
                    DrawTexture(rankTexture,
                        new Rectangle(Width - rankTexture.Width - TextBorderDistance, height + textureYPosition,
                        rankTexture.Width, rankTexture.Height), Color.White);
                }

                x += lbItem.TextXPadding;

                for (int j = 0; j < lbItem.TextLines.Count; j++)
                {
                    DrawStringWithShadow(lbItem.TextLines[j], FontIndex,
                        new Vector2(x, height + j * LineHeight + lbItem.TextYPadding),
                        lbItem.TextColor);
                }

                height += lbItem.TextLines.Count * LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }

    public class CampaignSelector : XNAWindow
    {
        private const int DEFAULT_WIDTH = 650;
        private const int DEFAULT_HEIGHT = 600;

        private static readonly string[] DifficultyNames = new string[] { "Easy", "Medium", "Hard" };
        private static readonly string[] DifficultyNamesUIDefault = new string[] { "EASY", "NORMAL", "HARD" };

        private static readonly string[] DifficultyIniPaths = new string[]
        {
            "INI/Map Code/Difficulty Easy.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Hard.ini"
        };

        public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;

        private XNALabel lblEasy;
        private XNALabel lblNormal;
        private XNALabel lblHard;

        private CheaterWindow cheaterWindow;

        private readonly string[] filesToCheck = new string[]
        {
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/Enhance.ini",
            "INI/Rules.ini",
            "INI/Map Code/Difficulty Hard.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Easy.ini"
        };

        private Mission missionToLaunch;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            Name = "CampaignSelector";

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblSelectCampaign.Text = "MISSIONS:";

            lbCampaignList = new BattleListBox(WindowManager);
            lbCampaignList.Name = "lbCampaignList";
            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbCampaignList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbCampaignList.ClientRectangle = new Rectangle(12, 
                lblSelectCampaign.Bottom + 6, 300, 516);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                lbCampaignList.Right + 12, 
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:";

            tbMissionDescription = new XNATextBlock(WindowManager);
            tbMissionDescription.Name = "tbMissionDescription";
            tbMissionDescription.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X, 
                lblMissionDescriptionHeader.Bottom + 6,
                Width - 24 - lbCampaignList.Right, 430);
            tbMissionDescription.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            tbMissionDescription.Alpha = 1.0f;

            tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                tbMissionDescription.Width, tbMissionDescription.Height);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL";
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new XNATrackbar(WindowManager);
            trbDifficultySelector.Name = "trbDifficultySelector";
            trbDifficultySelector.ClientRectangle = new Rectangle(
                tbMissionDescription.X, lblDifficultyLevel.Bottom + 6,
                tbMissionDescription.Width, 30);
            trbDifficultySelector.MinValue = 0;
            trbDifficultySelector.MaxValue = 2;
            trbDifficultySelector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);
            trbDifficultySelector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton_difficulty.png");

            lblEasy = new XNALabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.TextAnchor = LabelTextAnchorInfo.RIGHT;
            lblEasy.AnchorPoint = new Vector2(trbDifficultySelector.X,
                trbDifficultySelector.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN);
            lblEasy.FontIndex = 1;
            lblEasy.Text = DifficultyNamesUIDefault[0];

            lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.TextAnchor = LabelTextAnchorInfo.HORIZONTAL_CENTER;
            lblNormal.AnchorPoint = new Vector2(trbDifficultySelector.ClientRectangle.Center.X, lblEasy.AnchorPoint.Y);
            lblNormal.FontIndex = 1;
            lblNormal.Text = DifficultyNamesUIDefault[1];

            lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.TextAnchor = LabelTextAnchorInfo.LEFT;
            lblHard.AnchorPoint = new Vector2(trbDifficultySelector.Right, lblEasy.AnchorPoint.Y);
            lblHard.FontIndex = 1;
            lblHard.Text = DifficultyNamesUIDefault[2];

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.ClientRectangle = new Rectangle(12, Height - 35, 133, 23);
            btnLaunch.Text = "Launch";
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 145,
                btnLaunch.Y, 133, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(lbCampaignList);
            AddChild(tbMissionDescription);
            AddChild(lblDifficultyLevel);
            AddChild(btnLaunch);
            AddChild(btnCancel);
            AddChild(trbDifficultySelector);
            AddChild(lblEasy);
            AddChild(lblNormal);
            AddChild(lblHard);

            // Set control attributes from INI file
            base.Initialize();

            // Center on screen
            CenterOnParent();

            trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

            cheaterWindow = new CheaterWindow(WindowManager);
            DarkeningPanel dp = new DarkeningPanel(WindowManager);
            dp.AddChild(cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            cheaterWindow.CenterOnParent();
            cheaterWindow.YesClicked += CheaterWindow_YesClicked;
            cheaterWindow.Disable();

            EnabledChanged += CampaignSelector_EnabledChanged;
        }

        private void CampaignSelector_EnabledChanged(object sender, EventArgs e)
        {
            if (Enabled)
                ListBattles();
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            var mission = lbCampaignList.SelectedItem.Tag as Mission;

            if (string.IsNullOrEmpty(mission.Scenario))
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            string[] difficultyLabels = mission.DifficultyLabels != null ? mission.DifficultyLabels : DifficultyNamesUIDefault;
            lblEasy.Text = difficultyLabels[0].ToUpperInvariant();
            lblNormal.Text = difficultyLabels[1].ToUpperInvariant();
            lblHard.Text = difficultyLabels[2].ToUpperInvariant();

            tbMissionDescription.Text = mission.GUIDescription;

            if (!mission.Enabled)
            {
                btnLaunch.AllowClick = false;
                return;
            }

            btnLaunch.AllowClick = true;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            int selectedMissionId = lbCampaignList.SelectedIndex;

            var mission = lbCampaignList.SelectedItem.Tag as Mission;

            if (!ClientConfiguration.Instance.ModMode && 
                (!CUpdater.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
            {
                // Confront the user by showing the cheater screen
                missionToLaunch = mission;
                cheaterWindow.Enable();
                return;
            }

            LaunchMission(mission);
        }

        private bool AreFilesModified()
        {
            foreach (string filePath in filesToCheck)
            {
                if (!CUpdater.IsFileNonexistantOrOriginal(filePath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when the user wants to proceed to the mission despite having
        /// being called a cheater.
        /// </summary>
        private void CheaterWindow_YesClicked(object sender, EventArgs e)
        {
            LaunchMission(missionToLaunch);
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        /// <param name="scenario">The internal name of the scenario.</param>
        /// <param name="requiresAddon">True if the mission is for Firestorm / Enhanced Mode.</param>
        private void LaunchMission(Mission mission)
        {
            bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

            Logger.Log("About to write spawn.ini.");
            StreamWriter swriter = new StreamWriter(ProgramConstants.GamePath + "spawn.ini");
            swriter.WriteLine("; Generated by DTA Client");
            swriter.WriteLine("[Settings]");
            if (copyMapsToSpawnmapINI)
                swriter.WriteLine("Scenario=spawnmap.ini");
            else
                swriter.WriteLine("Scenario=" + mission.Scenario);

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (UserINISettings.Instance.GameSpeed == 0)
                UserINISettings.Instance.GameSpeed.Value = 1;

            swriter.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
            swriter.WriteLine("Firestorm=" + mission.RequiredAddon);
            swriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(mission.Side));
            swriter.WriteLine("IsSinglePlayer=Yes");
            swriter.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
            swriter.WriteLine("Side=" + mission.Side);
            swriter.WriteLine("BuildOffAlly=" + mission.BuildOffAlly);
            if (UserINISettings.Instance.EnableSPAutoSave)
                swriter.WriteLine("AutoSaveGame=" + ClientConfiguration.Instance.SinglePlayerAutoSaveInterval);

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;

            swriter.WriteLine("DifficultyModeHuman=" + (mission.PlayerAlwaysOnNormalDifficulty ? "1" : trbDifficultySelector.Value.ToString()));
            swriter.WriteLine("DifficultyModeComputer=" + GetComputerDifficulty());

            IniFile difficultyIni = new IniFile(ProgramConstants.GamePath + DifficultyIniPaths[trbDifficultySelector.Value]);
            string difficultyName = DifficultyNames[trbDifficultySelector.Value];

            swriter.WriteLine();
            swriter.WriteLine();
            swriter.WriteLine();
            swriter.Close();

            if (copyMapsToSpawnmapINI)
            {
                IniFile mapIni = new IniFile(ProgramConstants.GamePath + mission.Scenario);
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni.WriteIniFile(ProgramConstants.GamePath + "spawnmap.ini");
            }

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
            UserINISettings.Instance.SaveSettings();

            ((MainMenuDarkeningPanel)Parent).Hide();

            discordHandler?.UpdatePresence(mission.GUIName, difficultyName, mission.IconPath, true);
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(new GameSessionManager(GameSessionType.SINGLEPLAYER, DateTime.Now.Ticks, WindowManager.AddCallback));
        }

        private int GetComputerDifficulty() =>
            Math.Abs(trbDifficultySelector.Value - 2);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            // Logger.Log("GameProcessExited: Updating Discord Presence.");
            discordHandler?.UpdatePresence();
        }

        public void ListBattles()
        {
            lbCampaignList.Clear();

            CampaignHandler.Instance.BattleList.ForEach(mission =>
            {
                XNAListBoxItem item = new XNAListBoxItem();
                item.Tag = mission;
                item.Text = mission.GUIName;
                if (!mission.Enabled)
                {
                    item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                }
                else if (string.IsNullOrEmpty(mission.Scenario) && string.IsNullOrWhiteSpace(mission.CampaignInternalName))
                {
                    item.TextColor = AssetLoader.GetColorFromString(
                        ClientConfiguration.Instance.ListBoxHeaderColor);
                    item.IsHeader = true;
                    item.Selectable = false;
                }
                else
                {
                    item.TextColor = lbCampaignList.DefaultItemColor;
                }

                if (!string.IsNullOrEmpty(mission.IconPath))
                    item.Texture = AssetLoader.LoadTexture(mission.IconPath);

                lbCampaignList.AddItem(item);
            });
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
