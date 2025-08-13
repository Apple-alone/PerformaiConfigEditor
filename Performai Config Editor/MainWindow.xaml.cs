using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Performai_Config_Editor
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, Dictionary<string, string>> sectionData = new Dictionary<string, Dictionary<string, string>>();
        private string filePath = "segatools.ini";
        private string deviceDir = "DEVICE";

        private Dictionary<string, List<(string Address, string Description)>> serverPresets = new Dictionary<string, List<(string, string)>>
        {
            { "SDEZ", new List<(string, string)>
                {
                    ("play.mumur.net", "MuNET (推荐)"),
                    ("aquadx.init.ink", "AquaDX 中国加速线"),
                    ("aquadx.hydev.org", "AquaDX 主服务器"),
                    ("aqua.naominet.live", "RinNET"),
                    ("custom", "自定义服务器")
                }
            },
            { "SDHD", new List<(string, string)>
                {
                    ("play.mumur.net", "MuNET (推荐)"),
                    ("aquadx.init.ink", "AquaDX 中国加速线"),
                    ("aquadx.hydev.org", "AquaDX 主服务器"),
                    ("aqua.naominet.live", "RinNET"),
                    ("custom", "自定义服务器")
                }
            },
            { "SDDT", new List<(string, string)>
                {
                    ("aqua.naominet.live", "RinNET (推荐)"),
                    ("custom", "自定义服务器")
                }
            },
            { "SDGA", new List<(string, string)>
                {
                    ("play.mumur.net", "MuNET (推荐)"),
                    ("aquadx.init.ink", "AquaDX 中国加速线"),
                    ("aquadx.hydev.org", "AquaDX 主服务器"),
                    ("aqua.naominet.live", "RinNET"),
                    ("custom", "自定义服务器")
                }
            }
        };

        public MainWindow()
        {
            InitializeComponent();
            VersionComboBox.ItemsSource = serverPresets.Keys.ToList();

            // 初始化事件
            LedSerialRadio.Checked += (s, e) => SerialSettingsPanel.Visibility = Visibility.Visible;
            LedPipeRadio.Checked += (s, e) => SerialSettingsPanel.Visibility = Visibility.Collapsed;

            if (File.Exists(filePath)) LoadFile();
        }

        private void LoadFile()
        {
            try
            {
                sectionData.Clear();
                string currentSection = "";

                foreach (var line in File.ReadAllLines(filePath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        sectionData[currentSection] = new Dictionary<string, string>();
                    }
                    else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith(";"))
                    {
                        var parts = trimmed.Split(new[] { '=' }, 2);
                        if (parts.Length == 2 && !string.IsNullOrEmpty(currentSection))
                        {
                            sectionData[currentSection][parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                DetectVersion();
                UpdateUI();
                StatusTextBlock.Text = $"已加载: {Path.GetFileName(filePath)} [{VersionComboBox.SelectedItem}]";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "加载文件失败";
            }
        }

        private void DetectVersion()
        {
            if (sectionData.ContainsKey("led15093") || sectionData.ContainsKey("unity"))
            {
                VersionComboBox.SelectedItem = "SDDT";
            }
            else if (sectionData.ContainsKey("slider") ||
                    (sectionData.ContainsKey("zhousensor") && sectionData["zhousensor"].ContainsKey("side_red")))
            {
                VersionComboBox.SelectedItem = "SDHD";
            }
            else if (sectionData.ContainsKey("keychip") &&
                     sectionData["keychip"].ContainsKey("gameid") &&
                     sectionData["keychip"]["gameid"] == "SDGA")
            {
                VersionComboBox.SelectedItem = "SDGA";
            }
            else
            {
                VersionComboBox.SelectedItem = "SDEZ";
            }
        }

        private void UpdateUI()
        {
            // AIME设置
            AimeEnableCheckBox.IsChecked = GetConfigBool("aime", "enable", true);
            AimePathTextBox.Text = GetConfigValue("aime", "aimePath", "DEVICE\\aime.txt");
            LoadCardFile();

            // 网络设置
            UpdateServerComboBox(GetConfigValue("dns", "default", ""));
            AimeDbTextBox.Text = GetConfigValue("dns", "aimeDB", "");
            RouterDnsTextBox.Text = GetConfigValue("dns", "router", "223.5.5.5");
            NetEnvEnableCheckBox.IsChecked = GetConfigBool("netenv", "enable", true);

            // KeyChip设置
            KeyChipIdTextBox.Text = GetConfigValue("keychip", "id", "");
            GameIdTextBox.Text = VersionComboBox.SelectedItem.ToString();
            SubnetTextBox.Text = GetConfigValue("keychip", "subnet", "192.168.1.0");

            // 版本特定设置
            switch (VersionComboBox.SelectedItem.ToString())
            {
                case "SDHD":
                    LoadSdhdSettings();
                    break;
                case "SDDT":
                    LoadSddtSettings();
                    break;
            }
        }

        private void LoadSdhdSettings()
        {
            // 控制器设置
            if (sectionData.ContainsKey("chuniio") && sectionData["chuniio"].ContainsKey("path"))
            {
                var path = sectionData["chuniio"]["path"].ToLower();
                SdhdControllerCombo.SelectedIndex = path.Contains("yubideck") ? 1 :
                                                  path.Contains("tasoller") ? 2 :
                                                  path.Contains("brokenithm") ? 3 : 0;
            }

            // 红外传感器
            IrEnableCheckBox.IsChecked = sectionData.ContainsKey("ir") &&
                                       sectionData["ir"].ContainsKey("enable") &&
                                       sectionData["ir"]["enable"] == "1";

            // YubiDeck设置
            if (sectionData.ContainsKey("zhousensor"))
            {
                var zs = sectionData["zhousensor"];
                YbRedSlider.Value = zs.ContainsKey("side_red") ? int.Parse(zs["side_red"]) : 0;
                YbGreenSlider.Value = zs.ContainsKey("side_green") ? int.Parse(zs["side_green"]) : 255;
                YbBlueSlider.Value = zs.ContainsKey("side_blue") ? int.Parse(zs["side_blue"]) : 0;
                YbRandomCheckBox.IsChecked = zs.ContainsKey("side_random") && zs["side_random"] == "1";
                YbRealAimeCheckBox.IsChecked = !zs.ContainsKey("real_aime") || zs["real_aime"] == "1";
            }

            SdhdTab.Visibility = Visibility.Visible;
        }

        private void LoadSddtSettings()
        {
            // LED设置
            LedEnableCheckBox.IsChecked = GetConfigBool("led15093", "enable", true);

            bool useSerial = GetConfigBool("led", "cabLedOutputSerial", false);
            LedSerialRadio.IsChecked = useSerial;
            LedPipeRadio.IsChecked = !useSerial;
            SerialSettingsPanel.Visibility = useSerial ? Visibility.Visible : Visibility.Collapsed;

            SerialPortTextBox.Text = GetConfigValue("led", "serialPort", "COM5");
            SerialBaudTextBox.Text = GetConfigValue("led", "serialBaud", "921600");

            // Unity设置
            UnityEnableCheckBox.IsChecked = GetConfigBool("unity", "enable", true);
            TargetAssemblyTextBox.Text = GetConfigValue("unity", "targetAssembly", "");

            SddtTab.Visibility = Visibility.Visible;
        }

        private void UpdateServerComboBox(string currentServer)
        {
            var version = VersionComboBox.SelectedItem.ToString();
            var presets = serverPresets[version];

            ServerComboBox.ItemsSource = presets.Select(p => p.Description).ToList();

            int selectedIndex = 0;
            bool found = false;

            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i].Address == currentServer)
                {
                    selectedIndex = i;
                    found = true;
                    break;
                }
            }

            if (!found && !string.IsNullOrEmpty(currentServer))
            {
                selectedIndex = presets.Count - 1;
                CustomServerTextBox.Text = currentServer;
                CustomServerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                CustomServerGrid.Visibility = Visibility.Collapsed;
            }

            ServerComboBox.SelectedIndex = selectedIndex;
            UpdateServerHelp();
        }

        private void UpdateServerHelp()
        {
            var version = VersionComboBox.SelectedItem.ToString();
            var selectedIndex = ServerComboBox.SelectedIndex;
            var presets = serverPresets[version];

            if (selectedIndex < 0 || selectedIndex >= presets.Count) return;

            var selected = presets[selectedIndex].Address;
            string helpText = selected switch
            {
                "aquadx.init.ink" => "推荐：AquaDX中国加速线，适合中国大陆用户",
                "aquadx.hydev.org" => "注意：AquaDX国际主线服务器位于加拿大，延迟可能较高",
                "play.mumur.net" => "推荐：MuNET服务器，新用户首选",
                "aqua.naominet.live" when version == "SDDT" => "推荐：RinNET是SDDT版本的首选服务器",
                "aqua.naominet.live" => "RinNET服务器",
                _ => ""
            };

            ServerHelpTextBlock.Text = helpText;
        }

        private string GetConfigValue(string section, string key, string defaultValue)
        {
            return sectionData.ContainsKey(section) && sectionData[section].ContainsKey(key)
                ? sectionData[section][key]
                : defaultValue;
        }

        private bool GetConfigBool(string section, string key, bool defaultValue)
        {
            return sectionData.ContainsKey(section) && sectionData[section].ContainsKey(key)
                ? sectionData[section][key] == "1"
                : defaultValue;
        }

        private void LoadCardFile()
        {
            try
            {
                var path = AimePathTextBox.Text;
                if (File.Exists(path))
                {
                    CardContentTextBox.Text = File.ReadAllText(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取卡号文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveCardFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cardPath = AimePathTextBox.Text;
                if (string.IsNullOrWhiteSpace(cardPath))
                {
                    MessageBox.Show("请先指定卡号文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 确保目录存在
                string directory = Path.GetDirectoryName(cardPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(cardPath, CardContentTextBox.Text);
                MessageBox.Show("卡号文件保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存卡号文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCardFile()
        {
            try
            {
                var path = AimePathTextBox.Text;
                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show("请先指定卡号文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, CardContentTextBox.Text);
                MessageBox.Show("卡号文件保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存卡号文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSdhdSettings()
        {
            // 控制器
            sectionData["chuniio"] = new Dictionary<string, string>
            {
                ["path"] = SdhdControllerCombo.SelectedIndex switch
                {
                    1 => "yubideck.dll",
                    2 => "tasoller.dll",
                    3 => "brokenithm.dll",
                    _ => "chuniio.dll"
                }
            };

            // 红外
            if (IrEnableCheckBox.IsChecked == true)
            {
                sectionData["ir"] = new Dictionary<string, string> { ["enable"] = "1" };
            }

            // YubiDeck
            sectionData["zhousensor"] = new Dictionary<string, string>
            {
                ["side_red"] = ((int)YbRedSlider.Value).ToString(),
                ["side_green"] = ((int)YbGreenSlider.Value).ToString(),
                ["side_blue"] = ((int)YbBlueSlider.Value).ToString(),
                ["side_random"] = YbRandomCheckBox.IsChecked == true ? "1" : "0",
                ["real_aime"] = YbRealAimeCheckBox.IsChecked == true ? "1" : "0"
            };
        }

        private void SaveSddtSettings()
        {
            // LED设置
            sectionData["led15093"] = new Dictionary<string, string>
            {
                ["enable"] = LedEnableCheckBox.IsChecked == true ? "1" : "0"
            };

            sectionData["led"] = new Dictionary<string, string>
            {
                ["cabLedOutputPipe"] = LedPipeRadio.IsChecked == true ? "1" : "0",
                ["controllerLedOutputPipe"] = LedPipeRadio.IsChecked == true ? "1" : "0",
                ["cabLedOutputSerial"] = LedSerialRadio.IsChecked == true ? "1" : "0",
                ["controllerLedOutputSerial"] = LedSerialRadio.IsChecked == true ? "1" : "0"
            };

            if (LedSerialRadio.IsChecked == true)
            {
                sectionData["led"]["serialPort"] = SerialPortTextBox.Text;
                sectionData["led"]["serialBaud"] = SerialBaudTextBox.Text;
            }

            // Unity设置
            sectionData["unity"] = new Dictionary<string, string>
            {
                ["enable"] = UnityEnableCheckBox.IsChecked == true ? "1" : "0"
            };

            if (!string.IsNullOrWhiteSpace(TargetAssemblyTextBox.Text))
            {
                sectionData["unity"]["targetAssembly"] = TargetAssemblyTextBox.Text;
            }
        }

        private void SaveFile()
        {
            try
            {
                var lines = new List<string>();

                foreach (var section in sectionData)
                {
                    lines.Add($"[{section.Key}]");
                    foreach (var kvp in section.Value)
                    {
                        lines.Add($"{kvp.Key}={kvp.Value}");
                    }
                    lines.Add("");
                }

                File.WriteAllLines(filePath, lines);
                StatusTextBlock.Text = $"已保存: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "保存文件失败";
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "INI 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*",
                Title = "选择 segatools.ini 文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                deviceDir = Path.GetDirectoryName(filePath);
                LoadFile();
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 收集公共配置
                sectionData["aime"] = new Dictionary<string, string>
                {
                    ["enable"] = AimeEnableCheckBox.IsChecked == true ? "1" : "0",
                    ["aimePath"] = AimePathTextBox.Text
                };

                sectionData["dns"] = new Dictionary<string, string>();
                var version = VersionComboBox.SelectedItem.ToString();
                var presets = serverPresets[version];
                var selectedIndex = ServerComboBox.SelectedIndex;

                if (selectedIndex == presets.Count - 1)
                {
                    sectionData["dns"]["default"] = CustomServerTextBox.Text;
                }
                else
                {
                    sectionData["dns"]["default"] = presets[selectedIndex].Address;
                }

                if (!string.IsNullOrWhiteSpace(AimeDbTextBox.Text))
                {
                    sectionData["dns"]["aimeDB"] = AimeDbTextBox.Text;
                }

                if (!string.IsNullOrWhiteSpace(RouterDnsTextBox.Text))
                {
                    sectionData["dns"]["router"] = RouterDnsTextBox.Text;
                }

                sectionData["netenv"] = new Dictionary<string, string>
                {
                    ["enable"] = NetEnvEnableCheckBox.IsChecked == true ? "1" : "0"
                };

                sectionData["keychip"] = new Dictionary<string, string>
                {
                    ["id"] = KeyChipIdTextBox.Text,
                    ["gameid"] = VersionComboBox.SelectedItem.ToString(),
                    ["subnet"] = SubnetTextBox.Text
                };

                // 保存版本特定配置
                switch (VersionComboBox.SelectedItem.ToString())
                {
                    case "SDHD":
                        SaveSdhdSettings();
                        break;
                    case "SDDT":
                        SaveSddtSettings();
                        break;
                }

                SaveFile();
                SaveCardFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseAimeFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.GetDirectoryName(AimePathTextBox.Text) ?? deviceDir,
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                Title = "选择卡号文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AimePathTextBox.Text = openFileDialog.FileName;
                LoadCardFile();
            }
        }

        private void ShowKeyChipHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"如何获取KeyChip ID:
1. 访问 https://portal.mumur.net
2. 注册/登录或迁移账号
3. 点击【个人资料】→【卡片绑定和机台配置】获取KeyChip ID

如果你使用AquaDX:
1. 访问 https://aquadx.net
2. 登录后获取KeyChip ID";

            MessageBox.Show(helpText, "KeyChip ID 获取帮助", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionComboBox.SelectedItem == null) return;

            SdhdTab.Visibility = Visibility.Collapsed;
            SddtTab.Visibility = Visibility.Collapsed;

            switch (VersionComboBox.SelectedItem.ToString())
            {
                case "SDHD":
                    SdhdTab.Visibility = Visibility.Visible;
                    break;
                case "SDDT":
                    SddtTab.Visibility = Visibility.Visible;
                    break;
            }

            GameIdTextBox.Text = VersionComboBox.SelectedItem.ToString();
            UpdateServerComboBox("");
            StatusTextBlock.Text = $"当前版本: {VersionComboBox.SelectedItem}";
        }

        private void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerComboBox.SelectedIndex == serverPresets[VersionComboBox.SelectedItem.ToString()].Count - 1)
            {
                CustomServerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                CustomServerGrid.Visibility = Visibility.Collapsed;
            }
            UpdateServerHelp();
        }

        private void AdvancedModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AdvancedTab.Visibility = Visibility.Visible;
            LoadToAdvanced_Click(sender, e);
        }

        private void AdvancedModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AdvancedTab.Visibility = Visibility.Collapsed;
        }

        private void LoadToAdvanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdvancedEditorTextBox.Text = File.ReadAllText(filePath);
                StatusTextBlock.Text = "配置已加载到高级编辑器";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载到高级编辑器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAdvanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllText(filePath, AdvancedEditorTextBox.Text);
                LoadFile();
                MessageBox.Show("高级更改已应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用高级更改失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}