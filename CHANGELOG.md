# 更新日志 / 踩坑记录

> 越新的记录越靠前排列。

---

## 2026-09-21 — 功能新增 + 编译修复 + 残余清理

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