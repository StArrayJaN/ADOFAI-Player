using System.Numerics;
using ImGuiNET;
using SharpFAI.Editor.Core.Framework.Audio;
using SharpFAI.Editor.Core.Models;
using SharpFAI.Editor.Core.Platform.FileProvider;
using SharpFAI.Editor.Core.UI;
using SharpFAI.Editor.Core.Util;
using SharpFAI.Framework;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Editor.Core.Application;

/// <summary>
/// LevelEditor - UI 渲染部分
/// 从 EditorPlayer.UI.cs 迁移
/// </summary>
public partial class LevelEditor
{
    private void RenderEditorUi()
    {
        RenderMenuBar();
        RenderMainLayout();

        if (_showAboutWindow)
        {
            RenderAboutWindow();
        }

        // 渲染轨道按钮（仅在选中一个地板时）
        if (_selectedFloors.Count == 1)
        {
            RenderTrackButtons();
        }

        RenderStatusBar();
    }

    private void RenderMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("文件 File"))
            {
                if (ImGui.MenuItem("打开关卡 Open Level", "Ctrl+O"))
                {
                    OpenLevelFile();
                }

                ImGui.Separator();

                bool hasLevel = _level != null;

                if (ImGui.MenuItem("保存 Save", "Ctrl+S", false, hasLevel))
                {
                    SaveLevel();
                }

                if (ImGui.MenuItem("另存为 Save As...", "Ctrl+Shift+S", false, hasLevel))
                {
                    SaveLevelAs();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("退出 Exit", "ESC"))
                {
                    GraphicsContext.Close();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("播放 Playback"))
            {
                if (ImGui.MenuItem(_isPlaying ? "暂停 Pause" : "播放 Play", "Space"))
                {
                    if (_isPlaying)
                        PausePlay();
                    else
                        StartPlay();
                }

                if (ImGui.MenuItem("停止 Stop"))
                {
                    StopPlay();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("工具 Tools"))
            {
                if (ImGui.MenuItem("刷新 Refresh", "F5"))
                {
                    if (_levelPath != null)
                    {
                        LoadLevel(_levelPath);
                    }
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("帮助 Help"))
            {
                if (ImGui.MenuItem("关于编辑器 About Editor"))
                {
                    _showAboutWindow = true;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void RenderMainLayout()
    {
        const float menuBarHeight = 25f;
        const float statusBarHeight = 25f;
        const float tabButtonWidth = 40f;
        const float eventSetTabHeight = 35f;
        const float topPadding = 20f;
        const float bottomPadding = 20f;

        var workAreaHeight = GraphicsContext.Height - menuBarHeight - statusBarHeight;

        // 计算顶部面板高度：如果底部面板折叠，则顶部面板占据更多空间
        // Calculate top panel height: if bottom panel is collapsed, top panel takes more space
        float topPanelHeight;
        float eventSetTabStartY;

        if (_bottomPanelCollapsed)
        {
            // 折叠时：顶部面板占据几乎所有空间，标签栏紧贴底部
            // When collapsed: top panel takes almost all space, tabs stick to bottom
            topPanelHeight = workAreaHeight - eventSetTabHeight - topPadding - bottomPadding;
            eventSetTabStartY = GraphicsContext.Height - statusBarHeight - eventSetTabHeight;
        }
        else
        {
            // 展开时：正常布局
            // When expanded: normal layout
            topPanelHeight = workAreaHeight - eventSetTabHeight - _bottomPanelHeight - topPadding - bottomPadding;
            eventSetTabStartY = menuBarHeight + topPadding + topPanelHeight + bottomPadding;
        }

        var topPanelStartY = menuBarHeight + topPadding;

        // 左侧：设置面板 + 右侧子标签
        if (!_leftPanelCollapsed)
        {
            RenderSettingsPanelWithTabs(topPanelStartY, topPanelHeight, tabButtonWidth);
        }
        else
        {
            RenderCollapsedLeftPanel(topPanelStartY, topPanelHeight, tabButtonWidth);
        }

        // 右侧：左侧子标签 + 事件信息面板
        if (!_rightPanelCollapsed)
        {
            RenderEventInfoPanelWithTabs(topPanelStartY, topPanelHeight, tabButtonWidth);
        }
        else
        {
            RenderCollapsedRightPanel(topPanelStartY, topPanelHeight, tabButtonWidth);
        }

        // 底部：事件集子标签 + 事件集面板（标签始终显示）
        // Bottom: Event set tabs + panel (tabs always visible)
        RenderEventSetPanelWithTabs(eventSetTabStartY, eventSetTabHeight, _bottomPanelHeight);
    }

    private void RenderSettingsPanelWithTabs(float startY, float height, float tabWidth)
    {
        const float startX = 0f;

        // 设置面板
        var windowMinSize = new Vector2(200, height);
        var windowMaxSize = new Vector2(GraphicsContext.Width * 0.5f, height);
        var windowSize = new Vector2(_leftPanelWidth, height);
        var windowPos = new Vector2(startX, startY);
        bool windowOpen = true;

        if (ImGuiControls.BeginWindow("设置面板", ref windowOpen, windowPos, windowSize, windowMinSize, windowMaxSize))
        {
            ImGui.Text("设置面板 Settings Panel");
            ImGui.Separator();
            ImGui.Spacing();

            // 音乐文件设置 - 始终可用
            string musicFileName = "未选择";

            if (!string.IsNullOrEmpty(_musicFilePath))
            {
                musicFileName = Path.GetFileName(_musicFilePath);
            }
            else if (_level != null && _level.HasSetting("songFilename"))
            {
                string? songFilename = _level.GetSetting<string>("songFilename");
                if (!string.IsNullOrEmpty(songFilename))
                {
                    musicFileName = songFilename;
                }
            }

            ImGui.PushItemWidth(-80); // 留出按钮空间
            ImGui.InputText("##MusicPath", ref musicFileName, 256, ImGuiInputTextFlags.ReadOnly);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            if (ImGuiControls.FixedButton("选择文件", 70, 0, () =>
            {
                var filePath = FileDialog.GetFileDialog().OpenFile("打开关卡","",new OpenFileFilter()
                {
                    Filter = new Dictionary<string, List<string>>
                    {
                        { "音频文件", ["*.mp3", "*.wav", "*.ogg", "*.flac"] }
                    },
                    IncludeAllFiles = true
                });

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    _musicFilePath = filePath;
                    _statusMessage = $"已选择音乐: {Path.GetFileName(filePath)}";
                }
            }))
            {
                // 按钮已处理
            }

            ImGui.Spacing();

            // BPM 设置 - 始终可用
            ImGuiControls.FloatInput("BPM:", "BPM", ref _bpm, 0.1f, 1.0f, "%.2f", (newValue) =>
            {
                if (newValue < 1f) _bpm = 1f;
                if (newValue > 999f) _bpm = 999f;
                _statusMessage = $"BPM 设置为 {_bpm:F2}";
            });

            ImGui.Spacing();

            // 音高设置 - 始终可用，整数输入
            int pitchInt = (int)_pitch;
            ImGuiControls.IntInput("音高:", "Pitch", ref pitchInt, 1, 10, (newValue) =>
            {
                if (newValue < 50) newValue = 50;
                if (newValue > 200) newValue = 200;
                _pitch = newValue;
                _statusMessage = $"音高设置为 {newValue}%";
            });

            // 在输入框旁边显示百分号提示
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{pitchInt}%");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // 关卡信息 - 仅在加载关卡后显示
            if (_level != null && _levelPath != null)
            {
                ImGui.Text("关卡信息:");
                ImGui.Text($"文件: {Path.GetFileName(_levelPath)}");

                if (_floors != null)
                {
                    ImGui.Text($"地板数量: {_floors.Count}");
                }
            }
            else
            {
                ImGui.TextWrapped("提示: 可以先设置音乐和参数，然后从菜单打开关卡文件。");
            }

            _leftPanelWidth = ImGui.GetWindowWidth();
        }

        ImGuiControls.EndWindow();

        // 右侧紧贴的单个标签按钮
        if (ImGuiControls.VerticalTab("LeftTab", "⚙", "设置", _leftPanelWidth, startY, tabWidth, height, () =>
        {
            // 单击切换折叠状态
            _leftPanelCollapsed = !_leftPanelCollapsed;
        }))
        {
            // 标签按钮已处理
        }
    }

    private void RenderEventInfoPanelWithTabs(float startY, float height, float tabWidth)
    {
        var startX = GraphicsContext.Width - _rightPanelWidth;

        // 左侧紧贴的单个标签按钮
        if (ImGuiControls.VerticalTab("RightTab", "📝", "事件", startX - tabWidth, startY, tabWidth, height, () =>
        {
            // 单击切换折叠状态
            _rightPanelCollapsed = !_rightPanelCollapsed;
        }))
        {
            // 标签按钮已处理
        }

        // 事件信息面板
        var windowMinSize = new Vector2(200, height);
        var windowMaxSize = new Vector2(GraphicsContext.Width * 0.5f, height);
        var windowSize = new Vector2(_rightPanelWidth, height);
        var windowPos = new Vector2(startX, startY);
        bool windowOpen = true;

        if (ImGuiControls.BeginWindow("事件信息", ref windowOpen, windowPos, windowSize, windowMinSize, windowMaxSize))
        {
            ImGui.Text("事件信息 Event Info");
            ImGui.Separator();
            ImGui.Spacing();

            if (_selectedFloors.Count > 0)
            {
                var selectedFloor = _selectedFloors[0];
                var selectedIndex = _selectedFloorIndices[0];

                ImGui.Text($"地板 Floor #{selectedIndex} (共选中 {_selectedFloors.Count} 个)");
                ImGui.Spacing();

                ImGui.Text("📝 地板信息");
                ImGui.Text($"事件数量: {selectedFloor.events.Count}");
                ImGui.Text($"BPM: {selectedFloor.bpm:F2}");
                ImGui.Text($"角度: {selectedFloor.angle:F2}°");
                ImGui.Text($"位置: ({selectedFloor.position.X:F2}, {selectedFloor.position.Y:F2})");
                ImGui.Text($"旋转: {(selectedFloor.isCW ? "顺时针" : "逆时针")}");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (selectedFloor.events.Count > 0)
                {
                    ImGui.Text("事件列表:");
                    if (ImGui.BeginChild("EventsChild", new Vector2(0, 0)))
                    {
                        foreach (var evt in selectedFloor.events)
                        {
                            ImGui.BulletText($"{evt.EventType} (Floor: {evt.Floor})");
                        }
                    }
                    ImGui.EndChild();
                }
            }
            else
            {
                ImGui.TextWrapped("未选中地板。请从地板列表中选择。");
            }

            _rightPanelWidth = ImGui.GetWindowWidth();
        }

        ImGuiControls.EndWindow();
    }

    private void RenderEventSetPanelWithTabs(float startY, float tabHeight, float panelHeight)
    {
        const float startX = 0f;
        var width = GraphicsContext.Width;

        // 顶部水平单个标签按钮 - 始终显示
        // Top horizontal single tab button - always visible
        ImGui.SetNextWindowPos(new Vector2(startX, startY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(width, tabHeight), ImGuiCond.Always);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.1f, 0.95f));

        if (ImGui.Begin("事件集标签", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text("事件集 ↓");
            ImGui.SameLine();

            if (ImGuiControls.FixedButton("🎯 事件集", 80, 25, () =>
            {
                // 单击切换折叠状态
                _bottomPanelCollapsed = !_bottomPanelCollapsed;
            }))
            {
                // 按钮已处理
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("事件集 (单击折叠)");
            }
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);

        // 事件集面板 - 仅在未折叠时显示
        // Event set panel - only show when not collapsed
        if (!_bottomPanelCollapsed)
        {
            ImGui.SetNextWindowPos(new Vector2(startX, startY + tabHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, _bottomPanelHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(width, 100), new Vector2(width, GraphicsContext.Height * 0.5f));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.12f, 0.95f));

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

            if (ImGui.Begin("事件集面板", flags))
            {
                ImGui.Text("事件集面板 Event Set Panel");
                ImGui.Separator();
                ImGui.Spacing();

                if (_floors != null)
                {
                    ImGui.Text("显示事件集内容");
                    ImGui.Spacing();

                    ImGui.Text($"总地板数: {_floors.Count}");
                    ImGui.Text($"当前选中: {(_selectedFloorIndices.Count > 0 ? string.Join(", ", _selectedFloorIndices.Select(i => i + 1)) : "无")}");
                }
                else
                {
                    ImGui.TextWrapped("请先加载关卡文件");
                }

                _bottomPanelHeight = ImGui.GetWindowHeight();
            }

            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }
    }

    private void RenderCollapsedLeftPanel(float startY, float height, float tabWidth)
    {
        if (ImGuiControls.VerticalTab("CollapsedLeft", "⚙", "设置", 0, startY, tabWidth, height, () =>
        {
            HandleSingleTabDoubleClick(ref _lastLeftTabClickTime, ref _leftPanelCollapsed);
        }))
        {
            // 标签按钮已处理
        }
    }

    private void RenderCollapsedRightPanel(float startY, float height, float tabWidth)
    {
        if (ImGuiControls.VerticalTab("CollapsedRight", "📝", "事件", GraphicsContext.Width - tabWidth, startY, tabWidth, height, () =>
        {
            HandleSingleTabDoubleClick(ref _lastRightTabClickTime, ref _rightPanelCollapsed);
        }))
        {
            // 标签按钮已处理
        }
    }

    private void RenderAboutWindow()
    {
        ImGui.SetNextWindowPos(new Vector2(GraphicsContext.Width / 2f - 250, GraphicsContext.Height / 2f - 150), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.Appearing);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.15f, 0.95f));

        if (ImGui.Begin("关于 About SharpFAI Editor", ref _showAboutWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.8f, 1.0f, 1.0f));
            var titleSize = ImGui.CalcTextSize("SharpFAI Editor");
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - titleSize.X) / 2);
            ImGui.Text("SharpFAI Editor");
            ImGui.PopStyleColor();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextWrapped("A Dance of Fire and Ice (ADOFAI) 关卡编辑器");
            ImGui.TextWrapped("使用 C# 和 OpenTK + ImGui 开发");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("功能 Features:");
            ImGui.BulletText("多面板布局");
            ImGui.BulletText("子标签切换（双击折叠）");
            ImGui.BulletText("事件集管理");
            ImGui.BulletText("播放控制");
            ImGui.BulletText("摄像机拖动和缩放");

            ImGui.Spacing();
            ImGui.Spacing();

            const float buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);
            if (ImGuiControls.FixedButton("关闭 Close", buttonWidth, 30, () =>
            {
                _showAboutWindow = false;
            }))
            {
                // 按钮已处理
            }
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
    }

    private void RenderStatusBar()
    {
        const float statusBarHeight = 25f;
        ImGui.SetNextWindowPos(new Vector2(0, GraphicsContext.Height - statusBarHeight));
        ImGui.SetNextWindowSize(new Vector2(GraphicsContext.Width, statusBarHeight));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 5));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.15f, 0.15f, 0.18f, 1.0f));

        if (ImGui.Begin("StatusBar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(_statusMessage);

            ImGui.SameLine(GraphicsContext.Width - 450);
            if (_camera2D != null)
            {
                ImGui.Text($"缩放: {_camera2D.Zoom:F1}x");
                ImGui.SameLine();
            }

            ImGui.SameLine(GraphicsContext.Width - 300);
            if (_isPlaying)
            {
                ImGui.Text($"▶ {_currentTime:F2}s");
                ImGui.SameLine();
            }

            ImGui.SameLine(GraphicsContext.Width - 200);
            ImGui.Text($"FPS: {1.0 / UpdateTime:F0}");

            if (_isShiftPressed)
            {
                ImGui.SameLine(GraphicsContext.Width - 120);
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "⇧ Shift");
            }
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void HandleSingleTabDoubleClick(ref double lastClickTime, ref bool panelCollapsed)
    {
        var currentTime = UpdateTime;

        if ((currentTime - lastClickTime) < DoubleClickTime)
        {
            // 双击，切换折叠状态
            // Double click, toggle collapsed state
            panelCollapsed = !panelCollapsed;
            lastClickTime = 0; // 重置时间避免三击 / Reset to avoid triple click
        }
        else
        {
            // 单击，只记录时间，不做任何操作
            // Single click, just record time, no action
            lastClickTime = currentTime;
        }
    }

    private void RenderKeyboardHints()
    {
        const float hintSize = 80f;
        const float spacing = 10f;
        var centerX = GraphicsContext.Width / 2f;
        var centerY = GraphicsContext.Height / 2f;

        ImGui.SetNextWindowPos(new Vector2(centerX - 200, centerY - 150), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Always);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 20));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.15f, 0.85f));

        if (ImGui.Begin("按键提示 Keyboard Hints", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0, 1));
            ImGui.Text("⇧ Shift 模式");
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("按键布局:");
            ImGui.Spacing();

            // 显示不同的按键布局（根据图片）
            ImGui.Text("第一组: Q W E / A S D / Z X C");
            ImGui.Text("第二组: T Y / H J / N M / V B");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextWrapped("提示: 按住 Shift 可以访问更多按键选项");
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void OpenLevelFile()
    {
        var filePath = FileDialog.GetFileDialog().OpenFile("打开关卡", "", new OpenFileFilter()
        {
            Filter = new Dictionary<string, List<string>>
            {
                { "关卡文件", ["adofai"] }
            },
            IncludeAllFiles = true
        });

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            LoadLevel(filePath);
        }
    }

    private void SaveLevel()
    {
        if (_level == null)
        {
            _statusMessage = "没有可保存的关卡";
            return;
        }

        // 如果没有路径，调用另存为
        if (string.IsNullOrEmpty(_levelPath))
        {
            SaveLevelAs();
            return;
        }

        try
        {
            _level.Save(_levelPath);
            _statusMessage = $"已保存 {Path.GetFileName(_levelPath)}";
        }
        catch (Exception ex)
        {
            _statusMessage = $"保存失败: {ex.Message}";
            Console.WriteLine($"Save error: {ex}");
        }
    }

    private void SaveLevelAs()
    {
        if (_level == null)
        {
            _statusMessage = "没有可保存的关卡";
            return;
        }

        // 生成默认文件名
        string defaultFileName = "level.adofai";
        if (!string.IsNullOrEmpty(_levelPath))
        {
            defaultFileName = Path.GetFileName(_levelPath);
        }

        var filePath = FileDialog.GetFileDialog().SaveFile("保存关卡", "", defaultFileName, new SaveFileFilter()
        {
            Filter = new Dictionary<string, string>
            {
                { "关卡文件", "*.adofai" }
            },
            IncludeAllFiles = false
        });

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                // 确保文件有正确的扩展名
                if (!filePath.EndsWith(".adofai", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".adofai";
                }

                _level.Save(filePath);
                _levelPath = filePath;
                _statusMessage = $"已另存为: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _statusMessage = $"另存为失败 {ex.Message}";
                Console.WriteLine($"Save as error: {ex}");
            }
        }
    }

    private async void LoadLevel(string? levelPath)
    {
        try
        {
            _statusMessage = "正在加载关卡... Loading level...";
            _initialized = false;
            _needsGLInitialization = false;

            // 停止播放
            StopPlay();

            // 释放旧的音频资源
            _music?.Dispose();
            _music = null;
            _hitSound?.Dispose();
            _hitSound = null;

            // 释放旧的轨道资源
            if (_playerFloors != null)
            {
                foreach (var floor in _playerFloors)
                {
                    floor?.Dispose();
                }
                _playerFloors = null;
            }
            _cachedRenderOrder = Array.Empty<int>();
            _needRenderOrderUpdate = true;

            // 释放星球
            _redPlanet?.Dispose();
            _bluePlanet?.Dispose();
            _redPlanet = null;
            _bluePlanet = null;
            _lastPlanet = null;
            _currentPlanet = null;

            // 如果提供了路径，加载新关卡；否则使用现有的_level
            if (!string.IsNullOrEmpty(levelPath))
            {
                _level = new Level(levelPath);
                _levelPath = levelPath;
            }
            else if (_level == null)
            {
                // 如果没有关卡，创建一个新的
                _level = Level.CreateNewLevel();
                _statusMessage = "已创建新关卡 New level created";
            }

            // 从关卡读取设置到编辑器字段
            if (_level.HasSetting("bpm"))
            {
                _bpm = (float)_level.GetSetting<double>("bpm");
            }
            if (_level.HasSetting("pitch"))
            {
                _pitch = (float)_level.GetSetting<double>("pitch");
            }

            _statusMessage = "正在计算音符时间... Calculating note times...";
            var noteTimes = await Task.Run(() => _level.GetNoteTimes().Select(a => a.Item1).ToList());
            _noteTimes = noteTimes.Select(x => x - noteTimes[0]).ToList();

            _statusMessage = "正在生成地板... Generating floors...";
            var floors = await Task.Run(() => _level.CreateFloors(usePositionTrack: true));
            _floors = floors;

            _statusMessage = "正在初始化音频.. Initializing audio...";
            try
            {
                // 尝试获取音频路径
                string? audioPath = null;

                if (!string.IsNullOrEmpty(_musicFilePath))
                {
                    audioPath = _musicFilePath;
                }
                else if (_level.HasSetting("songFilename") && !string.IsNullOrEmpty(_level.GetSetting<string>("songFilename")))
                {
                    try
                    {
                        audioPath = _level.GetAudioPath();
                    }
                    catch
                    {
                        // 音频文件不存在，继续不加载音乐
                        Console.WriteLine("Audio file not found in level settings");
                    }
                }

                if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                {
                    _music = new Music(audioPath);
                    _music.Preload();
                    _statusMessage = "音频加载成功 Audio loaded";
                }
                else
                {
                    _statusMessage = "未找到音频文件，将以无音乐模式运行No audio file found, running without music";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load music: {e}");
                _statusMessage = "音频加载失败，将以无音乐模式运行 Failed to load audio, running without music";
            }

            // 生成命中音效
            try
            {
                string hitSoundPath = Path.Combine(
                    string.IsNullOrEmpty(_levelPath) ? Path.GetTempPath() : Path.GetDirectoryName(_levelPath) ?? "",
                    "hitSound.wav");

                await Task.Run(() => new AudioMerger().Export("kick.wav".ExportAssets(), _noteTimes, hitSoundPath));
                _hitSound = new Music(hitSoundPath);
                _hitSound.Preload();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to create hitsound: {e}");
            }

            _selectedFloorIndices.Clear();
            _selectedFloors.Clear();

            // 初始化播放状态
            _currentIndex = 0;
            _currentFloor = _floors[0];
            _angle = 0;
            _isCw = _currentFloor.isCW;

            // 设置摄像机位置到第一个地板
            if (_floors.Count > 0 && _camera2D != null)
            {
                _camera2D.Position = _floors[0].position;
                _cameraFromPos = _floors[0].position;
                _cameraToPos = _floors[0].position;
                _cameraTimer = 0f;
            }

            // 标记需要在渲染循环中初始化 OpenGL 对象
            _needsGLInitialization = true;
            _statusMessage = $"加载完成！共 {_floors.Count} 个地板，正在初始化渲染..";
        }
        catch (Exception ex)
        {
            _statusMessage = $"加载失败 Load failed: {ex.Message}";
            Console.WriteLine($"Failed to load level: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _initialized = false;
        }
    }

    private void InitializeGLObjects()
    {
        if (!_needsGLInitialization || _floors == null)
            return;

        try
        {
            // 初始化星球（在主线程，必须最先创建）
            _redPlanet = new Planet(System.Drawing.Color.Red);
            _bluePlanet = new Planet(System.Drawing.Color.Blue);
            _redPlanet.Radius = Floor.width;
            _bluePlanet.Radius = Floor.width;
            _lastPlanet = _redPlanet;
            _currentPlanet = _bluePlanet;

            _statusMessage = "正在生成轨道网格... Generating track meshes...";

            // 生成轨道网格（在主线程）
            _playerFloors = _floors.Select(x => new PlayerFloor(x)).ToList();
            _needRenderOrderUpdate = true; // 标记需要更新渲染顺序

            _needsGLInitialization = false;
            _initialized = true;
            _statusMessage = $"加载完成！共 {_floors.Count} 个地板，按空格播放Press Space to play";
        }
        catch (Exception ex)
        {
            _statusMessage = $"OpenGL 初始化失败 {ex.Message}";
            Console.WriteLine($"Failed to initialize GL objects: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _needsGLInitialization = false;
            _initialized = false;
        }
    }
}
