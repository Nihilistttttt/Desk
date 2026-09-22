# 更新日志 / 踩坑记录

> 越新的记录越靠前排列。

---

## 2026-09-22 — 窗口宽度固定13列 + 拾色器按钮 + 编辑模式中心十字线 + 窗口位置持久化

### 变更记录
- **拾色器按钮**：MainWindow 右上角新增吸管图标按钮（黑边白底），Click 调用 GlobalColorPickerWindow.CreateNoShow()
- **关闭按钮重绘**：从圆形叉叉改为简洁 X 图标，统一黑边白底风格
- **窗口宽度固定13列**：GridPositionPanel 新增 Columns 依赖属性（默认13），MeasureOverride 返回 max(maxCol, 13) × CellWidth；Window_SizeChanged 水平方向强制 idealW = hOverhead + 13 × cellW
- **WrapPanel→Grid**：RightCardControl 中 WrapPanel 改为 Grid，消除 WrapPanel 不拉伸子元素导致的右侧空隙
- **GridPositionPanel 填满可用宽度**：MeasureOverride 返回 max(desiredWidth, availableSize.Width)，确保面板填满 ListBox 无空隙
- **窗口大小吸附**：SnapWindowSize 提取为独立方法，Window_Loaded 中 Dispatcher.BeginInvoke(Loaded) 延迟调用，解决初始加载时 listBox.ActualWidth=0 跳过调整的问题
- **编辑模式中心十字线**：RightCardControl 中新增 CenterCrosshair（红色半透明十字线+可拖动圆点），UpdateCenterCrosshair 按实际图标范围计算中心；进入编辑模式时 CenterWindowOnGrid 居中窗口使网格中心对齐屏幕中心
- **中心圆点可拖动**：Ellipse 设 IsHitTestVisible=True + Cursor=SizeAll，PreviewMouseLeftButtonDown 调用 MainWindow.DragMove()
- **窗口位置持久化**：AppConfig 新增 WindowLeft/WindowTop 属性，LocationChanged 事件实时保存；CenterWindowOnScreen 仅在无保存值时设默认位置
- **右上角按钮动态对齐**：LayoutUpdated 事件中通过 TransformToAncestor 计算 Card 右边缘与按钮右边缘实际差值，动态修正 MainBtnPanel 右 Margin

### 踩坑与解决措施

#### 坑 17：WrapPanel 不拉伸子元素导致右侧空隙
- **现象**：窗口宽度已设为 13 列 + 开销，但右边仍有约 98px 空隙。
- **根因**：WrapPanel 的 ArrangeOverride 按子元素 desired size 排列，不拉伸到 full width。UniformGrid desired width = GridPositionPanel desired width = 13×CellWidth = 1443，但 WrapPanel 可用宽度 = 1541.6，差值 98.6px 成为空隙。hOverhead 被误算为 110.6（实际应 12），idealW = 1553.6 = WinW，needSnapW=False，窗口不调整。
- **解决**：WrapPanel 改为 Grid（拉伸子元素）；GridPositionPanel.MeasureOverride 返回 max(desired, availableSize.Width) 填满可用宽度。
- **教训**：WrapPanel 不会 Stretch 子元素到容器宽度，需要 full-width 布局时用 Grid。

#### 坝 18：初始加载时 SizeChanged 跳过窗口宽度调整
- **现象**：SnapWindowSize 逻辑正确但窗口宽度始终不变。
- **根因**：LoadData 设置 Width 触发 SizeChanged，此时 listBox.ActualWidth=0（布局未完成），SnapWindowSize return。之后布局完成但 SizeChanged 不再触发。
- **解决**：Window_Loaded 末尾 Dispatcher.BeginInvoke(DispatcherPriority.Loaded) 延迟调用 SnapWindowSize，确保布局完成后执行。
- **教训**：WPF 初始加载时 ActualWidth/ActualHeight 可能为 0，延迟到 Loaded 优先级后再读取。

#### 坑 19：CenterWindowOnScreen 覆盖保存的窗口位置
- **现象**：拖动窗口后重启，位置又回到 (-8, 0)。
- **根因**：CenterWindowOnScreen 在 Window_Loaded 中无条件设 Left=-8, Top=0，覆盖了 LoadData 中从 AppConfig 恢复的位置。
- **解决**：CenterWindowOnScreen 检查 WindowLeft/WindowTop 是否已有保存值（非 NaN），有则 return 不覆盖。
- **教训**：默认值初始化逻辑不能无条件覆盖用户持久化数据。

#### 坑 20：Boolean2VisibilityConverter 未传 ConverterParameter 导致非编辑模式也显示十字线
- **现象**：中心十字线在非编辑模式下也可见。
- **根因**：Boolean2VisibilityConverter 的 Convert 方法在 value=false 时返回 (Visibility)parameter，未传 ConverterParameter 时 parameter=null，强转为 Visibility 得 0 即 Visible。
- **解决**：改为代码控制 Visibility（UpdateCenterCrosshair 方法中按 IconBatch_NoWrite 设置），不依赖 Converter。
- **教训**：使用 Converter 时必须确认所有分支的返回值，未传参数的分支可能产生意外默认值。

---

## 2026-09-21 (4) — 相对路径可编辑 + 防误隐藏 + 新图标自动定位

### 变更记录
- **相对路径可编辑**：IconInfoDialog 中相对路径从只读 TextBlock 改为可编辑 TextBox，SaveProperty 中保存
- **防止误隐藏**：AppWindowLostFocus 加 PreventHide 标志 + 编辑模式检查；AddUrlIcon/AddSystemIcon/PropertyConfig 打开对话框前设 PreventHide=true，Dispatcher.BeginInvoke(Background) 延迟恢复
- **新图标自动定位**：CommonCode.AssignEmptyGridCell 按行优先顺序找第一个空格子，6 处添加图标的地方均调用

### 踩坑与解决措施

#### 坑 15：新添加图标 GridX/GridY=-1 被隐藏
- **现象**：添加 URL/系统项目后看不到新图标。
- **根因**：new IconInfo() 创建时 gridX=-1, gridY=-1（字段初始化器），GridPositionPanel 把 GridX<0 的图标 Arrange 到 (0,0,0,0) 即不可见。
- **解决**：CommonCode.AssignEmptyGridCell 在 Add 前自动找空格子分配坐标。
- **教训**：新增数据模型字段后，所有创建路径都要正确初始化。

#### 坑 16：打开对话框触发 MainWindow Deactivated → HideApp
- **现象**：点击"添加URL项目"/"添加系统项目"后窗口消失，对话框不可见。
- **根因**：ContextMenu 关闭 → MainWindow 失焦 → AppWindow_Deactivated → AppWindowLostFocus → HideApp，对话框跟着隐藏。
- **解决**：PreventHide 标志位 + 编辑模式检查阻止隐藏；Dispatcher.BeginInvoke(Background) 延迟恢复。
- **教训**：WPF 失焦隐藏逻辑需排除子窗口/对话框打开场景。

---

## 2026-09-21 (3) — 编辑模式入口收窄

### 变更记录
- **PropertyConfig**：去掉 `IconBatch_NoWrite` 切换，打开图标属性对话框不再退出编辑模式
- **CheckAndExitEditMode**：去掉自动退出编辑模式逻辑，只更新 UI
- 编辑模式现在只能通过上方菜单按钮切换

### 踩坑与解决措施

#### 坑 14：PropertyConfig/CheckAndExitEditMode 自动切换编辑模式
- **现象**：编辑模式下右键图标→属性，关闭对话框后编辑模式被退出，且透明度不变。
- **根因**：PropertyConfig 中 `IconBatch_NoWrite = !IconBatch_NoWrite` 切换了编辑模式但不处理 CardOpacity；CheckAndExitEditMode 也自动退出编辑模式。
- **解决**：两处均去掉 `IconBatch_NoWrite` 切换，编辑模式只由上方菜单按钮控制。
- **教训**：设计约束"只有上方按键可切换编辑模式"需在所有代码路径中贯彻，不能有旁路切换。

---

## 2026-09-21 (2) — 图标网格布局 + 拖拽重写 + 面板操作按钮

### 变更记录

#### 图标网格布局系统
- **IconInfo 新增 GridX/GridY**：每个图标独立网格坐标，消除空占位图标需求
- **GridPositionPanel**：自定义 Panel 替换 VirtualizingWrapPanel，按 GridX/GridY 定位，格子大小绑定 ImgPanelWidth/ImgPanelHeight
- **数据迁移**：MigrateIconPositions 删除空白占位图标，按原顺序自动分配网格坐标
- **滚动方向**：从水平滚动改为垂直滚动（多行网格布局）

#### 拖拽系统重写
- **去掉 ListBoxDragDropManager**：改用 WPF 原生 DragDrop.DoDragDrop
- **Preview 事件**：改用 PreviewMouseLeftButtonDown/PreviewMouseMove 避免被 ListBoxItem 吞掉
- **网格拖拽逻辑**：Wrap_Drop 计算目标格子，空位直接放置，已占用则挤开（推到下一空格）
- **实时刷新**：RefreshGridPanel() 调用 InvalidateMeasure+InvalidateArrange，拖拽后即时生效

#### 面板操作按钮
- **MainWindow 菜单按钮**：齿轮按钮左边加菜单图标按钮（三条横线，黑边白底）
- **操作集中化**：添加URL/系统项目、锁定面板、编辑模式等移到按钮 ContextMenu
- **编辑模式状态显示**：菜单项文字反映当前状态（"编辑模式"↔"退出编辑模式"）
- **去掉右键 ContextMenu**：RightCardControl 不再有右键菜单

#### 编辑模式不透明度
- **进入编辑模式**：CardOpacity = 100（完全不透明，方便操作）
- **退出编辑模式**：CardOpacity = 0（完全透明，只显示图标）

---

### 踩坑与解决措施

#### 坑 7：BinaryFormatter 反序列化新字段默认值为 0 而非 -1
- **现象**：旧数据加载后所有图标堆在 (0,0) 位置。
- **根因**：BinaryFormatter 用 GetUninitializedObject 创建对象，不执行字段初始化器，新增的 gridX/gridY 字段默认为 0 而非 -1。迁移检查 `GridX == -1` 永远为 false。
- **解决**：改为检测"多个图标全在 (0,0)"触发迁移。
- **教训**：BinaryFormatter 反序列化不调用构造函数也不执行字段初始化器，新字段默认值是 0/null 而非代码中赋的初值。

#### 坑 8：ListBoxDragDropManager 不支持拖到空格子
- **现象**：拖拽图标到空位置无反应，只有拖到已有图标上才有效。
- **根因**：IndexUnderDragCursor 遍历 ListBoxItem 检查 IsMouseOver，空格子没有 ListBoxItem，返回 -1。Drop 方法中 newIndex < 0 且 oldIndex >= 0 时直接 return，不触发 ProcessDrop。
- **解决**：去掉 ListBoxDragDropManager，改用 WPF 原生 DragDrop.DoDragDrop。
- **教训**：第三方拖拽管理器依赖 ListBoxItem 存在性，不适合稀疏网格布局。

#### 坑 9：ListBoxItem 吞掉 MouseLeftButtonDown 事件
- **现象**：编辑模式下鼠标按住图标拖动无反应，Icon_MouseMove 不触发。
- **根因**：ListBoxItem 内部处理 MouseLeftButtonDown 并标记 e.Handled=true，冒泡事件被拦截，SimpleStackPanel 的 MouseLeftButtonDown/MouseMove 不触发。
- **解决**：改用 PreviewMouseLeftButtonDown/PreviewMouseMove 隧道事件，先于 ListBoxItem 处理。
- **教训**：ListBox 内的子元素鼠标事件用 Preview（隧道）版本，避免被 ListBoxItem 吞掉。

#### 坑 10：IconListBox 缺少 AllowDrop 导致 Drop 不触发
- **现象**：DragDrop.DoDragDrop 调用成功但 Wrap_Drop 不触发。
- **根因**：移除 ListBoxDragDropManager 后无人设置 listBox.AllowDrop=true，ListBox 默认 AllowDrop=false，拖到 ListBox 内部不触发 Drop 事件。
- **解决**：XAML 中给 IconListBox 加 AllowDrop="True" 和 Drop="Wrap_Drop"。
- **教训**：移除拖拽管理器后需手动设置 AllowDrop。

#### 坑 11：GridX_NoWrite 不触发 OnPropertyChanged 导致 UI 不刷新
- **现象**：拖拽后图标位置在内存中更新了，但 UI 不实时刷新，重启程序才生效。
- **根因**：GridX_NoWrite/GridY_NoWrite 的 setter 故意不触发 OnPropertyChanged（避免频繁保存），WPF 布局系统不知道属性变了，Panel 不重新排列。
- **解决**：Wrap_Drop 末尾调用 RefreshGridPanel() → InvalidateMeasure() + InvalidateArrange()。
- **教训**：NoWrite 属性改值后需手动触发布局刷新。

#### 坑 12：CheckAndExitEditMode 不恢复 CardOpacity
- **现象**：退出编辑模式后面板仍保持 100% 不透明。
- **根因**：CheckAndExitEditMode 直接设 IconBatch_NoWrite=false 而不通过 EditModeHandle，不透明度未恢复。
- **解决**：CheckAndExitEditMode 中检测退出编辑模式时设 CardOpacity=0。
- **教训**：所有退出编辑模式的路径都要处理副作用（不透明度等）。

#### 坑 13：hc:IconElement.Foreground 属性不存在
- **现象**：编译报 MC3072 XML 命名空间中不存在属性 IconElement.Foreground。
- **根因**：HandyControl 的 IconElement 没有 Foreground 附加属性。
- **解决**：改用 Button 自身的 Foreground 属性。
- **教训**：HandyControl 附加属性需查文档确认存在性。

---

## 2026-09-21 (1) — 功能新增 + 编译修复 + 残余清理

### 变更记录

#### 新增功能
- **导入备份**：`CommonCode.ImportBakAppData()` — 选择 `.bak` 文件 → 反序列化 → 覆盖 Data → 重启程序。入口：设置页"其它"按钮 + 托盘菜单"导入备份"项。
- **重置配置**：`CommonCode.ResetAppData()` — 保存默认 `AppData` 覆盖 Data → 重启程序。带二次确认弹窗防误按。入口：设置页"其它"按钮 + 托盘菜单"重置配置"项。

#### 修复
- **图标大小滑块上限**：`ThemeControl.xaml` 滑块 `Maximum` 从 60 → 100，匹配图标实际尺寸范围（默认 92），修复调节后无法回到默认值的问题。
- **编译错误 CS1061**：`CommonCode.cs` 中 `GetAppDataByFile()` 引用了已删除的 `AppConfig.MenuPassword`，删除 line 49-53 的 MenuPassword 引用块和 line 180-186 的 `SavePassword` 死方法。

#### 残余代码清理
- 删除 Quartz / KeyMouseHook / System.Web 引用
- 删除 7 个死代码文件：BacklogNotificatin、SearchResControl、PasswordDialog、AboutControl、PasswordType、SearchType、TodoTaskExecType、UpdateType 等
- 删除 MainWindow.xaml.rej、BacklogImg.png、About.png、EveryThing 插件目录
- 删除 MOUSE_MOVE_COUNT 等无用代码
- 精简 App.config
- 保留 `Microsoft.Extensions.Logging.Abstractions`（XamlFlair.WPF 间接依赖）

#### 其他
- git remote 从 `gitee.com/Nihilisttt/geek-desk.git` 切换为 `https://github.com/Nihilistttttt/Desk.git`

---

### 踩坑与解决措施

#### 坑 1：误删 MEL.Abstractions 导致编译失败
- **现象**：清理引用时删除了 `Microsoft.Extensions.Logging.Abstractions`，编译报 XamlFlair.WPF 加载失败。
- **根因**：MEL.Abstractions 是 XamlFlair.WPF 的间接依赖，虽未在 packages.config 直接列出但运行时需要。
- **解决**：从 `bin\Debug\lib\` 拷回 `MEL.dll` 并恢复 csproj 三处引用。
- **教训**：删除 NuGet 引用前必须检查间接依赖关系。

#### 坑 2：git checkout HEAD 恢复的是 first commit 版本
- **现象**：`edit` 工具意外删除了 `SaveAppData` 和 `BakAppData` 方法，用 `git checkout HEAD -- Util/CommonCode.cs` 恢复后编译报 CS1061 `AppConfig` 不包含 `MenuPassword` 定义。
- **根因**：仓库 git 历史只有一条 `a51d453 first commit`，`git checkout HEAD` 恢复的是初始版本而非工作区之前的版本，初始版本引用了已被用户删除的 `AppConfig.MenuPassword` 属性。
- **解决**：手动删除 CommonCode.cs 中 MenuPassword 引用块（line 49-53）和 SavePassword 死方法（line 180-186）。
- **教训**：只有一条 commit 时 `git checkout HEAD` 等于回到初始状态，无法恢复工作区中间状态；编辑前应先备份或用 `git stash`。

#### 坑 3：edit 工具意外删除相邻方法
- **现象**：用 `edit` 插入 `ImportBakAppData` 时，`oldString` 匹配范围过大，导致 `SaveAppData` 和 `BakAppData` 两个方法被一并删除，编译报 CS0103 找不到 `SaveAppData`（20 个错误）。
- **根因**：`oldString` 选择了包含多个方法签名的区间，替换后中间方法消失。
- **解决**：用 `git checkout HEAD` 恢复文件后重新精确插入。
- **教训**：`edit` 的 `oldString` 要尽量小而精确，只包含目标插入点附近的最小上下文。

#### 坑 4：软件无法打开（单实例 Mutex 冲突）
- **现象**：新编译版本双击无法打开，无任何响应。
- **根因**：正式安装版 GeekDesk 实例驻留后台，单实例 Mutex（`GeekDesk_Main_` + UUID）阻止新实例启动。
- **解决**：任务管理器结束旧 GeekDesk 进程后新版正常启动。
- **教训**：单实例程序测试前先确保无旧实例驻留；托盘程序关闭面板 ≠ 退出进程。

#### 坑 5：图标大小滑块上限与实际范围不匹配
- **现象**：图标大小硬编码 92，但滑块 `Maximum=60`，调节后无法回到默认值。
- **根因**：滑块上限 60 小于默认值 92，默认值超出可选范围。
- **解决**：`ThemeControl.xaml` 滑块 `Maximum` 改为 100。
- **教训**：修改 UI 参数前先确认实际使用范围与默认值的关系。
#### 坑 6：git commit message 中 \n 被当作字面字符而非换行
- **现象**：`git commit -m "line1\nline2"` 提交后，GitHub 上 commit message 显示字面的 `\n` 而非换行。
- **根因**：bash 双引号字符串中 `\n` 不会被解释为换行符，只是两个字符 `\` 和 `n`。bash 工具传递命令时也会把多行文本中的真实换行转成 `\n` 字面字符。
- **解决**：用 Write 工具写临时文件，再 `git commit -F <file>` 读取，可正确保留换行。或用多个 `-m` 参数（每个 `-m` 为一段）。
- **教训**：在 bash 工具中写多行 commit message，永远不要用 `\n` 转义；用 `-F` 从文件读取最可靠。