using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Performai_Config_Editor
{
    public static class MsgBox
    {
        public static MessageBoxResult Show(string message, string caption = "提示",
                                           MessageBoxButton buttons = MessageBoxButton.OK,
                                           MessageBoxImage icon = MessageBoxImage.None)
        {
            Window owner = Application.Current.MainWindow;
            return CustomMessageBox.Show(owner, message, caption, buttons, icon);
        }

        public static MessageBoxResult Show(string message, string details, string caption = "提示",
                                           MessageBoxButton buttons = MessageBoxButton.OK,
                                           MessageBoxImage icon = MessageBoxImage.None)
        {
            Window owner = Application.Current.MainWindow;
            return CustomMessageBox.Show(owner, message, details, caption, buttons, icon);
        }
        
        public static MessageBoxResult Info(string message, string caption = "提示")
        {
            return Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult Warning(string message, string caption = "警告")
        {
            return Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static MessageBoxResult Error(string message, string caption = "错误")
        {
            return Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static MessageBoxResult Question(string message, string caption = "确认")
        {
            return Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public static MessageBoxResult QuestionCancel(string message, string caption = "确认")
        {
            return Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }
        
        public static MessageBoxResult Info(string message, string details, string caption = "提示")
        {
            return Show(message, details, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult Warning(string message, string details, string caption = "警告")
        {
            return Show(message, details, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static MessageBoxResult Error(string message, string details, string caption = "错误")
        {
            return Show(message, details, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static MessageBoxResult Question(string message, string details, string caption = "确认")
        {
            return Show(message, details, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public static MessageBoxResult QuestionCancel(string message, string details, string caption = "确认")
        {
            return Show(message, details, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }
    }

    public partial class MainWindow : Window
    {
        private Dictionary<string, Dictionary<string, string>> sectionData = new Dictionary<string, Dictionary<string, string>>();
        private string filePath = ""; // 初始为空字符串，表示未选择文件
        private string deviceDir = "DEVICE";
        private bool isFileLoaded = false; // 新增：标记文件是否已加载

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

            // 设置版本信息
            VersionTextBlock.Text = $"Performai Config Editor v1.0 | {DateTime.Now.Year}";


            // 不再自动加载文件，等用户手动打开
            // if (File.Exists(filePath)) LoadFile();

            // 初始状态：未加载文件
            isFileLoaded = false;
            StatusTextBlock.Text = "请先打开配置文件";
        }

        private void LoadFile()
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    StatusTextBlock.Text = "请先打开配置文件";
                    isFileLoaded = false;
                    return;
                }

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

                // 设置文件已加载标志
                isFileLoaded = true;

                // 加载文件后检查冲突
                CheckCardFileConflict();
            }
            catch (Exception ex)
            {
                MsgBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "加载文件失败";
                isFileLoaded = false;
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

        // 检测AIME/FeliCa文件冲突
        private void CheckCardFileConflict()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string configDir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(configDir))
                    {
                        string aimePath = Path.Combine(configDir, "DEVICE", "aime.txt");
                        string felicaPath = Path.Combine(configDir, "DEVICE", "felica.txt");

                        bool aimeExists = File.Exists(aimePath);
                        bool felicaExists = File.Exists(felicaPath);

                        if (aimeExists && felicaExists)
                        {
                            // 两个文件都存在，警告用户
                            if (MsgBox.Show(
                                "检测到冲突：aime.txt 和 felica.txt 同时存在！\n\n" +
                                "这可能导致AIME读卡器行为异常。\n" +
                                "建议：\n" +
                                "1. 删除其中一个文件\n" +
                                "2. 或者在配置中禁用其中一个\n\n" +
                                "是否现在打开文件所在目录？",
                                "文件冲突警告",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            {
                                // 打开文件所在目录
                                string deviceDir = Path.Combine(configDir, "DEVICE");
                                if (Directory.Exists(deviceDir))
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", deviceDir);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 冲突检测失败不影响主要功能
                Console.WriteLine($"冲突检测失败: {ex.Message}");
            }
        }

        private void LoadCardFile()
        {
            try
            {
                string configuredPath = AimePathTextBox.Text;
                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    configuredPath = "DEVICE\\aime.txt"; // 默认路径
                }

                string actualPath = GetActualCardFilePath(configuredPath);

                if (File.Exists(actualPath))
                {
                    // 直接读取文件内容，不添加任何模板或注释
                    string fileContent = File.ReadAllText(actualPath);
                    CardContentTextBox.Text = fileContent;

                    // 如果实际读取的文件与配置的文件不同，更新显示
                    if (!actualPath.EndsWith(configuredPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = MakePathRelativeIfPossible(actualPath);
                        AimePathTextBox.Text = relativePath;
                        StatusTextBlock.Text = $"已自动切换到: {Path.GetFileName(actualPath)}";
                    }
                    else
                    {
                        StatusTextBlock.Text = $"已加载卡号文件: {Path.GetFileName(actualPath)}";
                    }

                    // 加载后检查冲突
                    CheckCardFileConflict();
                }
                else
                {
                    // 文件不存在，显示空内容
                    CardContentTextBox.Text = "";
                    StatusTextBlock.Text = "卡号文件不存在";
                }
            }
            catch (Exception ex)
            {
                MsgBox.Show($"读取卡号文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                CardContentTextBox.Text = "";
            }
        }

        // 智能获取实际卡号文件路径
        private string GetActualCardFilePath(string configuredPath)
        {
            // 如果文件存在，直接返回
            string fullPath = GetFullPath(configuredPath);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // 文件不存在，尝试查找另一种格式的文件
            string fileName = Path.GetFileName(configuredPath).ToLower();
            string directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                // 如果配置的是aime.txt但不存在，尝试查找felica.txt
                if (fileName == "aime.txt")
                {
                    string felicaPath = Path.Combine(directory, "felica.txt");
                    if (File.Exists(felicaPath))
                    {
                        return felicaPath;
                    }
                }
                // 如果配置的是felica.txt但不存在，尝试查找aime.txt
                else if (fileName == "felica.txt")
                {
                    string aimePath = Path.Combine(directory, "aime.txt");
                    if (File.Exists(aimePath))
                    {
                        return aimePath;
                    }
                }
                // 如果配置的是其他文件名，尝试查找aime.txt或felica.txt
                else
                {
                    string aimePath = Path.Combine(directory, "aime.txt");
                    if (File.Exists(aimePath))
                    {
                        return aimePath;
                    }

                    string felicaPath = Path.Combine(directory, "felica.txt");
                    if (File.Exists(felicaPath))
                    {
                        return felicaPath;
                    }
                }
            }

            // 如果都不存在，返回原始路径
            return fullPath;
        }

        // 获取完整路径（处理相对路径）
        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            if (File.Exists(filePath))
            {
                string configDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(configDir))
                {
                    return Path.Combine(configDir, path);
                }
            }

            return path;
        }

        // 将完整路径转换为相对路径（如果可能）
        private string MakePathRelativeIfPossible(string fullPath)
        {
            if (File.Exists(filePath))
            {
                string configDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(configDir) && fullPath.StartsWith(configDir, StringComparison.OrdinalIgnoreCase))
                {
                    return fullPath.Substring(configDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }

            return fullPath;
        }

        private void SaveCardFile_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已加载主配置文件
            if (!isFileLoaded || string.IsNullOrEmpty(filePath))
            {
                MsgBox.Show("请先打开配置文件", "无法保存", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string cardPath = AimePathTextBox.Text;
                if (string.IsNullOrWhiteSpace(cardPath))
                {
                    MsgBox.Show("请先指定卡号文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取完整路径
                string fullPath = GetFullPath(cardPath);

                // 确保目录存在
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 保存卡号文件
                string cardContent = CardContentTextBox.Text;

                // 直接保存，不进行任何格式验证
                File.WriteAllText(fullPath, cardContent);

                // 更新状态
                string fileName = Path.GetFileName(fullPath);
                StatusTextBlock.Text = $"卡号文件已保存: {fileName}";

                // 保存后检查冲突
                CheckCardFileConflict();

                MsgBox.Show($"卡号文件保存成功！", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MsgBox.Show($"保存卡号文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCardFile()
        {
            try
            {
                var path = AimePathTextBox.Text;
                if (string.IsNullOrWhiteSpace(path))
                {
                    MsgBox.Show("请先指定卡号文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, CardContentTextBox.Text);
                MsgBox.Show("卡号文件保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MsgBox.Show($"保存卡号文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MsgBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "保存文件失败";
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "INI 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*",
                Title = "选择 segatools.ini 文件",
                InitialDirectory = Directory.Exists(deviceDir) ? deviceDir : Environment.CurrentDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                deviceDir = Path.GetDirectoryName(filePath);
                isFileLoaded = true; // 标记文件已加载
                LoadFile(); // LoadFile中已经包含冲突检查
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已加载文件
            if (!isFileLoaded || string.IsNullOrEmpty(filePath))
            {
                MsgBox.Show("请先打开配置文件", "无法保存", MessageBoxButton.OK, MessageBoxImage.Warning);

                // 提示用户打开文件
                if (MsgBox.Show("是否现在打开配置文件？", "打开文件",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    OpenFile_Click(sender, e);
                }
                return;
            }

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

                // 检查冲突
                CheckCardFileConflict();

                MsgBox.Show("配置文件保存成功！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MsgBox.Show($"保存时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 获取AIME文件的初始目录
        private string GetInitialDirectoryForAimeFile()
        {
            string path = AimePathTextBox.Text;
            string fullPath = GetFullPath(path);

            // 获取目录部分
            string directory = Path.GetDirectoryName(fullPath);

            // 如果目录不存在，使用配置文件所在目录
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                directory = Path.GetDirectoryName(filePath) ?? deviceDir;
            }

            return directory;
        }

        private void ShowKeyChipHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"如何获取KeyChip ID:
1. 访问 https://portal.mumur.net
2. 注册/登录或迁移账号
3. 点击【个人资料】→【卡片绑定和机台配置】获取KeyChip ID

关于子网地址：
如果你不知道这个是什么请保持默认
该选项对应的配置是subnet";

            MsgBox.Show(helpText, "KeyChip ID 获取帮助", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionComboBox.SelectedItem == null) return;

            string selectedVersion = VersionComboBox.SelectedItem.ToString();
            TabItem currentSelectedTab = MainTabControl.SelectedItem as TabItem;

            // 判断当前是否在专用设置页
            bool wasOnVersionSpecificTab = (currentSelectedTab == SdhdTab || currentSelectedTab == SddtTab);

            // 隐藏所有版本特定选项卡
            SdhdTab.Visibility = Visibility.Collapsed;
            SddtTab.Visibility = Visibility.Collapsed;

            // 根据选择的版本显示相应的选项卡
            switch (selectedVersion)
            {
                case "SDHD":
                    SdhdTab.Visibility = Visibility.Visible;
                    // 如果已有配置，重新加载SDHD设置
                    if (sectionData.ContainsKey("chuniio") || sectionData.ContainsKey("zhousensor"))
                    {
                        LoadSdhdSettings();
                    }
                    // 如果之前就在专用选项卡上，切换到SDHD选项卡
                    if (wasOnVersionSpecificTab)
                    {
                        MainTabControl.SelectedItem = SdhdTab;
                    }
                    break;

                case "SDDT":
                    SddtTab.Visibility = Visibility.Visible;
                    // 如果已有配置，重新加载SDDT设置
                    if (sectionData.ContainsKey("led15093") || sectionData.ContainsKey("unity"))
                    {
                        LoadSddtSettings();
                    }
                    // 如果之前就在专用选项卡上，切换到SDDT选项卡
                    if (wasOnVersionSpecificTab)
                    {
                        MainTabControl.SelectedItem = SddtTab;
                    }
                    break;

                default:
                    // SDEZ/SDGA没有专用选项卡
                    // 如果之前就在专用选项卡上，切换回AIME选项卡
                    if (wasOnVersionSpecificTab)
                    {
                        MainTabControl.SelectedIndex = 0;
                    }
                    break;
            }

            GameIdTextBox.Text = selectedVersion;
            UpdateServerComboBox("");
            StatusTextBlock.Text = $"当前版本: {selectedVersion}";
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
            // 显示高级编辑选项卡
            AdvancedTab.Visibility = Visibility.Visible;

            // 设置高级选项卡为选中状态
            MainTabControl.SelectedItem = AdvancedTab;

            // 加载当前配置到编辑器
            try
            {
                if (File.Exists(filePath))
                {
                    AdvancedEditorTextBox.Text = File.ReadAllText(filePath);
                    StatusTextBlock.Text = "配置文件已加载到编辑器";
                }
                else
                {
                    AdvancedEditorTextBox.Text = "配置文件不存在，请先打开或创建一个配置文件";
                    StatusTextBlock.Text = "配置文件不存在";
                }
            }
            catch (Exception ex)
            {
                MsgBox.Show($"加载配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                AdvancedEditorTextBox.Text = $"; 加载文件时出错: {ex.Message}";
            }
        }

        private void AdvancedModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 隐藏高级编辑选项卡
            AdvancedTab.Visibility = Visibility.Collapsed;

            // 如果有其他可见的选项卡，切换到第一个可见的
            if (SdhdTab.Visibility == Visibility.Visible)
            {
                MainTabControl.SelectedItem = SdhdTab;
            }
            else if (SddtTab.Visibility == Visibility.Visible)
            {
                MainTabControl.SelectedItem = SddtTab;
            }
            else
            {
                // 默认切换到AIME选项卡
                MainTabControl.SelectedIndex = 0;
            }
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
                MsgBox.Show($"加载到高级编辑器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAdvanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllText(filePath, AdvancedEditorTextBox.Text);
                LoadFile();
                MsgBox.Show("高级更改已应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MsgBox.Show($"应用高级更改失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}