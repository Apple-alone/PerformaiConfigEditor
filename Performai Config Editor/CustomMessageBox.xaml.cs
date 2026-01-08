using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Performai_Config_Editor
{
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBox()
        {
            InitializeComponent();
            this.Loaded += CustomMessageBox_Loaded;
        }

        private void CustomMessageBox_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载后设置焦点到第一个按钮
            if (ButtonPanel.Children.Count > 0 && ButtonPanel.Children[0] is Button firstButton)
            {
                firstButton.Focus();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // 创建消息框的静态方法
        public static MessageBoxResult Show(Window owner, string message, string caption = "提示",
                                           MessageBoxButton buttons = MessageBoxButton.OK,
                                           MessageBoxImage icon = MessageBoxImage.None)
        {
            var msgBox = new CustomMessageBox
            {
                Owner = owner,
                TitleText = { Text = caption },
                MessageText = { Text = message }
            };

            // 设置图标
            SetIcon(msgBox, icon);

            // 添加按钮
            AddButtons(msgBox, buttons);

            // 显示对话框
            msgBox.ShowDialog();

            return msgBox.Result;
        }

        // 带详细信息的版本
        public static MessageBoxResult Show(Window owner, string message, string details,
                                           string caption = "提示",
                                           MessageBoxButton buttons = MessageBoxButton.OK,
                                           MessageBoxImage icon = MessageBoxImage.None)
        {
            var msgBox = new CustomMessageBox
            {
                Owner = owner,
                TitleText = { Text = caption },
                MessageText = { Text = message },
                DetailText = { Text = details, Visibility = Visibility.Visible }
            };

            // 设置图标
            SetIcon(msgBox, icon);

            // 添加按钮
            AddButtons(msgBox, buttons);

            // 调整窗口大小以适应详细信息
            msgBox.Height = 300;

            // 显示对话框
            msgBox.ShowDialog();

            return msgBox.Result;
        }

        private static void SetIcon(CustomMessageBox msgBox, MessageBoxImage icon)
        {
            // 创建颜色画笔
            var errorBrush = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));     // #E74C3C
            var warningBrush = new SolidColorBrush(Color.FromArgb(255, 255, 193, 7));   // #FFC107
            var infoBrush = new SolidColorBrush(Color.FromArgb(255, 52, 152, 219));     // #3498DB
            var questionBrush = new SolidColorBrush(Color.FromArgb(255, 44, 62, 80));   // #2C3E50
            var defaultBrush = new SolidColorBrush(Color.FromArgb(255, 149, 165, 166)); // #95A5A6

            switch (icon)
            {
                case MessageBoxImage.Error:
                    msgBox.IconText.Text = "❌";
                    msgBox.IconBorder.Background = errorBrush;
                    break;
                case MessageBoxImage.Warning:
                    msgBox.IconText.Text = "⚠️";
                    msgBox.IconBorder.Background = warningBrush;
                    break;
                case MessageBoxImage.Information:
                    msgBox.IconText.Text = "ℹ️";
                    msgBox.IconBorder.Background = infoBrush;
                    break;
                case MessageBoxImage.Question:
                    msgBox.IconText.Text = "❓";
                    msgBox.IconBorder.Background = questionBrush;
                    break;
                default:
                    msgBox.IconText.Text = "💡";
                    msgBox.IconBorder.Background = defaultBrush;
                    break;
            }
        }

        private static void AddButtons(CustomMessageBox msgBox, MessageBoxButton buttons)
        {
            msgBox.ButtonPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton(msgBox, "确定", MessageBoxResult.OK, isDefault: true);
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton(msgBox, "取消", MessageBoxResult.Cancel);
                    AddButton(msgBox, "确定", MessageBoxResult.OK, isDefault: true);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton(msgBox, "否", MessageBoxResult.No);
                    AddButton(msgBox, "是", MessageBoxResult.Yes, isDefault: true);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton(msgBox, "取消", MessageBoxResult.Cancel);
                    AddButton(msgBox, "否", MessageBoxResult.No);
                    AddButton(msgBox, "是", MessageBoxResult.Yes, isDefault: true);
                    break;
            }

            // 重新排列按钮顺序（确定/是在右侧）
            ReorderButtons(msgBox);
        }

        private static void AddButton(CustomMessageBox msgBox, string text, MessageBoxResult result, bool isDefault = false)
        {
            var button = new Button
            {
                Content = text,
                Tag = result,
                Style = msgBox.FindResource("MessageBoxButton") as Style,
                IsDefault = isDefault
            };

            button.Click += (s, e) =>
            {
                msgBox.Result = result;
                msgBox.DialogResult = (result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
                msgBox.Close();
            };

            // 添加到按钮面板
            msgBox.ButtonPanel.Children.Add(button);
        }

        private static void ReorderButtons(CustomMessageBox msgBox)
        {
            // 确保按钮顺序为：取消/否/是 或 取消/确定
            var buttons = msgBox.ButtonPanel.Children;

            if (buttons.Count >= 2)
            {
                // 找到"确定"或"是"按钮
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i] is Button btn && (btn.Content.ToString() == "确定" || btn.Content.ToString() == "是"))
                    {
                        // 如果是第一个，移动到最后一个
                        if (i == 0)
                        {
                            var button = buttons[i];
                            buttons.RemoveAt(i);
                            buttons.Add(button);
                        }
                        break;
                    }
                }
            }
        }

        // 处理键盘事件
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // 按ESC键相当于点击取消或关闭
                if (ButtonPanel.Children.Count > 0)
                {
                    // 查找取消按钮
                    foreach (UIElement child in ButtonPanel.Children)
                    {
                        if (child is Button btn && btn.Content.ToString() == "取消")
                        {
                            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            e.Handled = true;
                            return;
                        }
                    }

                    // 如果没有取消按钮，关闭窗口
                    CloseButton_Click(null, null);
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                // 按Enter键触发默认按钮
                if (ButtonPanel.Children.Count > 0)
                {
                    foreach (UIElement child in ButtonPanel.Children)
                    {
                        if (child is Button btn && btn.IsDefault)
                        {
                            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
        }

        // 支持拖拽窗口
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}