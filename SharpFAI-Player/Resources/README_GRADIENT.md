# 文字渐变 Shader 使用说明

## 功能说明
为 ImGui 文字添加彩色渐变效果，支持多种渐变模式。

## 使用方法
1. 运行程序后，打开"调试信息 Debug Info"窗口
2. 在"渐变着色器控制"部分，勾选"启用渐变效果"
3. 选择渐变模式：
   - 水平 Horizontal：从左到右的彩虹渐变
   - 垂直 Vertical：从上到下的彩虹渐变
   - 对角 Diagonal：对角线方向的彩虹渐变
   - 彩虹波浪 Rainbow Wave：动态波浪彩虹效果（默认）

## 技术细节
- Shader 文件位置：`Resources/text_gradient.vert` 和 `Resources/text_gradient.frag`
- 只对文字应用渐变，UI 元素保持原色
- 使用 HSV 色彩空间实现平滑的彩虹渐变
- 支持实时动画效果

## 注意事项
- 默认情况下渐变效果是关闭的，需要手动启用
- 渐变效果会应用到所有 ImGui 文字
- 如果遇到显示问题，可以关闭渐变效果恢复正常显示
