using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.TextFormatting;

namespace Updater
{
    public partial class GraphicsSettingWindow : Window
    {
        private readonly string _filePath;

        public GraphicsSettingWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            LoadSettings(); // Carichiamo le impostazioni esistenti
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    throw new FileNotFoundException("Configuration file not found.");
                }

                string[] lines = File.ReadAllLines(_filePath);

                foreach (var line in lines)
                {
                    if (line.StartsWith("SIZE_X="))
                    {
                        int sizeX = int.Parse(line.Replace("SIZE_X=", ""));
                        int sizeY = int.Parse(lines[Array.IndexOf(lines, line) + 1].Replace("SIZE_Y=", ""));
                        ResolutionComboBox.SelectedItem = ResolutionComboBox.Items
                            .Cast<ComboBoxItem>()
                            .FirstOrDefault(item => item.Content.ToString() == $"[INTERFACE_{sizeX}X{sizeY}]");
                    }
                    else if (line.StartsWith("FULLSCREEN="))
                    {
                        FullscreenCheckBox.IsChecked = line.Replace("FULLSCREEN=", "") == "TRUE";
                    }
                    else if (line.StartsWith("WATER="))
                    {
                        WaterCheckBox.IsChecked = line.Replace("WATER=", "") == "TRUE";
                    }
                    else if (line.StartsWith("SHADOW="))
                    {
                        ShadowCheckBox.IsChecked = line.Replace("SHADOW=", "") == "TRUE";
                    }
                    else if (line.StartsWith("GAMMA="))
                    {
                        GammaSlider.Value = double.Parse(line.Replace("GAMMA=", ""));
                    }
                    else if(line.StartsWith("GLOW_LEVEL="))
                    {
                        GlowLevelSlider.Value = int.Parse(line.Replace("GLOW_LEVEL=", ""));
                    }
                    else if (line.StartsWith("TEXTURE="))
                    {
                        TextureComboBox.SelectedItem = TextureComboBox.Items
                            .Cast<ComboBoxItem>()
                            .FirstOrDefault(item => item.Content.ToString() == line.Replace("TEXTURE=", ""));
                    }
                    else if (line.StartsWith("RANGE="))
                    {
                        RangeComboBox.SelectedItem = RangeComboBox.Items
                            .Cast<ComboBoxItem>()
                            .FirstOrDefault(item => item.Content.ToString() == line.Replace("RANGE=", ""));
                    }
                    else if (line.StartsWith("SKILLWNDLOCK="))
                    {
                        SkillWndLockCheckBox.IsChecked = line.Replace("SKILLWNDLOCK=", "") == "TRUE";
                    }
                    else if (line.StartsWith("ID="))
                    {
                        LoginIDTextBox.Text = line.Replace("ID=", "");
                    }
                    else if (line.StartsWith("LOGIN_ID_SAVE="))
                    {
                        SaveLoginCheckBox.IsChecked = line.Replace("LOGIN_ID_SAVE=", "") == "TRUE";
                    }
                    else if(line.StartsWith("PETS="))
                    {
                        PetsCheckBox.IsChecked = line.Replace("PETS=", "") == "TRUE";
                    }
                    else if (line.StartsWith("WINGS="))
                    {
                        WingsCheckBox.IsChecked = line.Replace("WINGS=", "") == "TRUE";
                    }
                    else if (line.StartsWith("COSTUMES="))
                    {
                        CostumesCheckBox.IsChecked = line.Replace("COSTUMES=", "") == "TRUE";
                    }
                    else if (line.StartsWith("EFFECTS="))
                    {
                        EffectsCheckBox.IsChecked = line.Replace("EFFECTS=", "") == "TRUE";
                    }
                    else if (line.StartsWith("USE_MOUSE="))
                    {
                        UseMouseCheckBox.IsChecked = line.Replace("USE_MOUSE=", "") == "TRUE";
                    }
                    else if (line.StartsWith("INV_MOUSE="))
                    {
                        InvertMouseCheckBox.IsChecked = line.Replace("INV_MOUSE=", "") == "TRUE";
                    }
                    else if (line.StartsWith("MOUSE_SENSITIVITY="))
                    {
                        MouseSensitivitySlider.Value = double.Parse(line.Replace("MOUSE_SENSITIVITY=", ""));
                    }
                    else if (line.StartsWith("VOL_BGM="))
                    {
                        BGMVolumeSlider.Value = double.Parse(line.Replace("VOL_BGM=", ""));
                    }
                    else if (line.StartsWith("VOL_EFFECT="))
                    {
                        EffectsVolumeSlider.Value = double.Parse(line.Replace("VOL_EFFECT=", ""));
                    }
                    else if (line.StartsWith("VOL_WORLD="))
                    {
                        WorldVolumeSlider.Value = double.Parse(line.Replace("VOL_WORLD=", ""));
                    }
                    else if (line.StartsWith("HELMET="))
                    {
                        HelmetCheckBox.IsChecked = line.Replace("HELMET=", "") == "TRUE";
                    }
                    else if (line.StartsWith("CLOAK="))
                    {
                        CloakCheckBox.IsChecked = line.Replace("CLOAK=", "") == "TRUE";
                    }
                    else if (line.StartsWith("WEAPONEFF="))
                    {
                        WeaponEffCheckBox.IsChecked = line.Replace("WEAPONEFF=", "") == "TRUE";
                    }
                    else if (line.StartsWith("REJECT_FIGHT="))
                    {
                        RejectFightCheckBox.IsChecked = line.Replace("REJECT_FIGHT=", "") == "TRUE";
                    }
                    else if (line.StartsWith("REJECT_TRADE="))
                    {
                        RejectTradeCheckBox.IsChecked = line.Replace("REJECT_TRADE=", "") == "TRUE";
                    }
                    else if (line.StartsWith("REJECT_PARTY="))
                    {
                        RejectPartyCheckBox.IsChecked = line.Replace("REJECT_PARTY=", "") == "TRUE";
                    }
                    else if(line.StartsWith("USE_FILTER="))
                    {
                        UseFilterCheckBox.IsChecked = line.Replace("USE_FILTER=", "") == "TRUE";
                    }

                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetLowSettings_Click(object sender, RoutedEventArgs e)
        {
            ApplyGraphicsPreset(
                texture: "LOW",
                range: "LOW",
                fullscreen: false,
                water: false,
                shadow: false,
                glowLevel: 0,
                helmet: false,
                cloak: false,
                weaponEff: false
            );
        }

        private void SetMidSettings_Click(object sender, RoutedEventArgs e)
        {
            ApplyGraphicsPreset(
                texture: "MEDIUM",
                range: "MEDIUM",
                fullscreen: false,
                water: true,
                shadow: true,
                glowLevel: 2,
                helmet: true,
                cloak: true,
                weaponEff: true
            );
        }

        private void SetHighSettings_Click(object sender, RoutedEventArgs e)
        {
            ApplyGraphicsPreset(
                texture: "HIGH",
                range: "HIGH",
                fullscreen: true,
                water: true,
                shadow: true,
                glowLevel: 4,
                helmet: true,
                cloak: true,
                weaponEff: true
            );
        }

        private void ApplyGraphicsPreset(
            string texture, string range, bool fullscreen, bool water,
            bool shadow, int glowLevel, bool helmet, bool cloak, bool weaponEff)
        {
            try
            {
                string[] lines = File.ReadAllLines(_filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("TEXTURE="))
                    {
                        lines[i] = $"TEXTURE={texture}";
                    }
                    else if (lines[i].StartsWith("RANGE="))
                    {
                        lines[i] = $"RANGE={range}";
                    }
                    else if (lines[i].StartsWith("FULLSCREEN="))
                    {
                        lines[i] = $"FULLSCREEN={(fullscreen ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("WATER="))
                    {
                        lines[i] = $"WATER={(water ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("SHADOW="))
                    {
                        lines[i] = $"SHADOW={(shadow ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("GLOW_LEVEL="))
                    {
                        lines[i] = $"GLOW_LEVEL={glowLevel}";
                    }
                    else if (lines[i].StartsWith("HELMET="))
                    {
                        lines[i] = $"HELMET={(helmet ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("CLOAK="))
                    {
                        lines[i] = $"CLOAK={(cloak ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("WEAPONEFF="))
                    {
                        lines[i] = $"WEAPONEFF={(weaponEff ? "TRUE" : "FALSE")}";
                    }
                }

                File.WriteAllLines(_filePath, lines);

                MessageBox.Show("Graphics preset applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSettings(); // Ricarichiamo i valori aggiornati
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while applying graphics preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Chiude semplicemente la finestra senza salvare
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedResolution = ((ComboBoxItem)ResolutionComboBox.SelectedItem)?.Content.ToString() ?? "";
                string fullscreen = FullscreenCheckBox.IsChecked == true ? "TRUE" : "FALSE";
                string water = WaterCheckBox.IsChecked == true ? "TRUE" : "FALSE";
                string shadow = ShadowCheckBox.IsChecked == true ? "TRUE" : "FALSE";
                int gamma = (int)GammaSlider.Value;
                int glowLevel = (int)GlowLevelSlider.Value;
                string texture = ((ComboBoxItem)TextureComboBox.SelectedItem)?.Content.ToString() ?? "MEDIUM";
                string range = ((ComboBoxItem)RangeComboBox.SelectedItem)?.Content.ToString() ?? "MEDIUM";

                string skillWndLock = SkillWndLockCheckBox.IsChecked == true ? "TRUE" : "FALSE";
                string useFilter = UseFilterCheckBox.IsChecked == true ? "TRUE" : "FALSE";

                string loginID = LoginIDTextBox.Text;
                string saveLogin = SaveLoginCheckBox.IsChecked == true ? "TRUE" : "FALSE";
                string[] lines = File.ReadAllLines(_filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("SIZE_X="))
                    {
                        lines[i] = $"SIZE_X={GetResolutionWidth(selectedResolution)}";
                        lines[i + 1] = $"SIZE_Y={GetResolutionHeight(selectedResolution)}";
                    }
                    else if (lines[i].StartsWith("FULLSCREEN="))
                    {
                        lines[i] = $"FULLSCREEN={fullscreen}";
                    }
                    else if (lines[i].StartsWith("WATER="))
                    {
                        lines[i] = $"WATER={water}";
                    }
                    else if (lines[i].StartsWith("SHADOW="))
                    {
                        lines[i] = $"SHADOW={shadow}";
                    }
                    else if (lines[i].StartsWith("GAMMA="))
                    {
                        lines[i] = $"GAMMA={gamma}";
                    }
                    else if(lines[i].StartsWith("GLOW_LEVEL="))
                    {
                        lines[i] = $"GLOW_LEVEL={glowLevel}";
                    }
                    else if (lines[i].StartsWith("TEXTURE="))
                    {
                        lines[i] = $"TEXTURE={texture}";
                    }
                    else if (lines[i].StartsWith("RANGE="))
                    {
                        lines[i] = $"RANGE={range}";
                    }
                    else if (lines[i].StartsWith("SKILLWNDLOCK="))
                    {
                        lines[i] = $"SKILLWNDLOCK={skillWndLock}";
                    }
                    else if (lines[i].StartsWith("ID="))
                    {
                        lines[i] = $"ID={loginID}";
                    }
                    else if (lines[i].StartsWith("LOGIN_ID_SAVE="))
                    {
                        lines[i] = $"LOGIN_ID_SAVE={saveLogin}";
                    }
                    else if(lines[i].StartsWith("PETS="))
                    {
                        lines[i] = $"PETS={(PetsCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("WINGS="))
                    {
                        lines[i] = $"WINGS={(WingsCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("COSTUMES="))
                    {
                        lines[i] = $"COSTUMES={(CostumesCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("EFFECTS="))
                    {
                        lines[i] = $"EFFECTS={(EffectsCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("USE_MOUSE="))
                    {
                        lines[i] = $"USE_MOUSE={(UseMouseCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("INV_MOUSE="))
                    {
                        lines[i] = $"INV_MOUSE={(InvertMouseCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("MOUSE_SENSITIVITY="))
                    {
                        lines[i] = $"MOUSE_SENSITIVITY={MouseSensitivitySlider.Value}";
                    }
                    else if (lines[i].StartsWith("VOL_BGM="))
                    {
                        lines[i] = $"VOL_BGM={BGMVolumeSlider.Value}";
                    }
                    else if (lines[i].StartsWith("VOL_EFFECT="))
                    {
                        lines[i] = $"VOL_EFFECT={EffectsVolumeSlider.Value}";
                    }
                    else if (lines[i].StartsWith("VOL_WORLD="))
                    {
                        lines[i] = $"VOL_WORLD={WorldVolumeSlider.Value}";
                    }
                    else if (lines[i].StartsWith("HELMET="))
                    {
                        lines[i] = $"HELMET={(HelmetCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("CLOAK="))
                    {
                        lines[i] = $"CLOAK={(CloakCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("WEAPONEFF="))
                    {
                        lines[i] = $"WEAPONEFF={(WeaponEffCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("REJECT_FIGHT="))
                    {
                        lines[i] = $"REJECT_FIGHT={(RejectFightCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("REJECT_TRADE="))
                    {
                        lines[i] = $"REJECT_TRADE={(RejectTradeCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if (lines[i].StartsWith("REJECT_PARTY="))
                    {
                        lines[i] = $"REJECT_PARTY={(RejectPartyCheckBox.IsChecked == true ? "TRUE" : "FALSE")}";
                    }
                    else if(lines[i].StartsWith("USE_FILTER="))
                    {
                        lines[i] = $"USE_FILTER={useFilter}";
                    }
                }

                File.WriteAllLines(_filePath, lines);

                MessageBox.Show("Settings updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetResolutionWidth(string resolution)
        {
            // Rimuove le parentesi quadre e separa la stringa sul carattere '_'
            var match = System.Text.RegularExpressions.Regex.Match(resolution, @"\[INTERFACE_(\d+)X(\d+)\]");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int width))
            {
                return width;
            }
            throw new FormatException($"Invalid resolution format: {resolution}");
        }

        private int GetResolutionHeight(string resolution)
        {
            // Rimuove le parentesi quadre e separa la stringa sul carattere '_'
            var match = System.Text.RegularExpressions.Regex.Match(resolution, @"\[INTERFACE_(\d+)X(\d+)\]");
            if (match.Success && int.TryParse(match.Groups[2].Value, out int height))
            {
                return height;
            }
            throw new FormatException($"Invalid resolution format: {resolution}");
        }


    }
}
