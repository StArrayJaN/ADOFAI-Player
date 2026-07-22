using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.ES30;

namespace SharpFAI.Editor.Platform.Android;

/// <summary>
/// ImGui 渲染器 - 使用 OpenTK GLES3 和 ImGui.NET
/// </summary>
public class ImGuiRenderer : IDisposable
{
    private int shaderProgram;
    private int vbo, ebo, vao;
    private int fontTexture;
    private int attribLocationTex;
    private int attribLocationProjMtx;
    private int attribLocationVtxPos;
    private int attribLocationVtxUV;
    private int attribLocationVtxColor;

    private int displayWidth;
    private int displayHeight;
    private float globalScale = 1.0f;

    public ImGuiRenderer()
    {
        // 创建 ImGui 上下文
        ImGui.CreateContext();

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
    }

    /// <summary>
    /// 设置全局缩放
    /// </summary>
    /// <param name="scale">缩放比例（1.0 = 100%）</param>
    public void SetGlobalScale(float scale)
    {
        if (scale <= 0) scale = 1.0f;

        globalScale = scale;

        var io = ImGui.GetIO();
        var style = ImGui.GetStyle();

        // 方法 1: 缩放字体
        io.FontGlobalScale = scale;

        // 方法 2: 缩放所有 UI 尺寸（只在初始化时调用一次）
        // 注意：这会永久修改 style，所以需要保存原始值或重新加载
        // style.ScaleAllSizes(scale);
    }

    /// <summary>
    /// 获取当前全局缩放
    /// </summary>
    public float GetGlobalScale() => globalScale;

    /// <summary>
    /// 初始化 OpenGL 资源
    /// </summary>
    public void Init()
    {
        CreateDeviceObjects();
        CreateFontsTexture();
    }

    /// <summary>
    /// 创建 OpenGL 设备对象
    /// </summary>
    private void CreateDeviceObjects()
    {
        // 顶点着色器
        string vertexShaderSource = @"#version 300 es
            precision mediump float;
            layout (location = 0) in vec2 Position;
            layout (location = 1) in vec2 UV;
            layout (location = 2) in vec4 Color;
            uniform mat4 ProjMtx;
            out vec2 Frag_UV;
            out vec4 Frag_Color;
            void main()
            {
                Frag_UV = UV;
                Frag_Color = Color;
                gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
            }";

        // 片段着色器
        string fragmentShaderSource = @"#version 300 es
            precision mediump float;
            in vec2 Frag_UV;
            in vec4 Frag_Color;
            uniform sampler2D Texture;
            layout (location = 0) out vec4 Out_Color;
            void main()
            {
                Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
            }";

        // 编译着色器
        int vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

        // 创建程序
        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);

        // 检查链接状态
        GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string infoLog = GL.GetProgramInfoLog(shaderProgram);
            throw new Exception($"Shader program linking failed: {infoLog}");
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // 获取 uniform 位置
        attribLocationTex = GL.GetUniformLocation(shaderProgram, "Texture");
        attribLocationProjMtx = GL.GetUniformLocation(shaderProgram, "ProjMtx");
        attribLocationVtxPos = 0;
        attribLocationVtxUV = 1;
        attribLocationVtxColor = 2;

        // 创建缓冲区
        GL.GenBuffers(1, out vbo);
        GL.GenBuffers(1, out ebo);
        GL.GenVertexArrays(1, out vao);
    }

    /// <summary>
    /// 编译着色器
    /// </summary>
    private int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation failed ({type}): {infoLog}");
        }

        return shader;
    }

    /// <summary>
    /// 创建字体纹理
    /// </summary>
    private void CreateFontsTexture()
    {
        var io = ImGui.GetIO();

        // 获取字体纹理数据
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        // 创建 OpenGL 纹理
        GL.GenTextures(1, out fontTexture);
        GL.BindTexture(TextureTarget.Texture2D, fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba,
            width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        // 存储纹理 ID
        io.Fonts.SetTexID((IntPtr)fontTexture);
        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// 开始新帧
    /// </summary>
    public void NewFrame(int width, int height, float deltaTime)
    {
        displayWidth = width;
        displayHeight = height;

        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(width, height);
        io.DisplayFramebufferScale = new Vector2(1.0f, 1.0f);
        io.DeltaTime = deltaTime > 0 ? deltaTime : 1.0f / 60.0f;

        ImGui.NewFrame();
    }

    /// <summary>
    /// 渲染 ImGui 绘制数据
    /// </summary>
    public void Render()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    /// <summary>
    /// 渲染绘制数据
    /// </summary>
    private unsafe void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;

        // 备份 GL 状态
        GL.GetInteger(GetPName.CurrentProgram, out int lastProgram);
        GL.GetInteger(GetPName.TextureBinding2D, out int lastTexture);
        GL.GetInteger(GetPName.ArrayBufferBinding, out int lastArrayBuffer);
        GL.GetInteger(GetPName.ElementArrayBufferBinding, out int lastElementArrayBuffer);
        GL.GetInteger(GetPName.VertexArrayBinding, out int lastVertexArray);
        GL.GetInteger(GetPName.BlendSrcAlpha, out int lastBlendSrcAlpha);
        GL.GetInteger(GetPName.BlendDstAlpha, out int lastBlendDstAlpha);
        GL.GetInteger(GetPName.BlendEquationAlpha, out int lastBlendEquationAlpha);
        bool lastEnableBlend = GL.IsEnabled(EnableCap.Blend);
        bool lastEnableCullFace = GL.IsEnabled(EnableCap.CullFace);
        bool lastEnableDepthTest = GL.IsEnabled(EnableCap.DepthTest);
        bool lastEnableScissorTest = GL.IsEnabled(EnableCap.ScissorTest);

        // 设置渲染状态
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        // 设置视口
        GL.Viewport(0, 0, displayWidth, displayHeight);

        // 设置投影矩阵
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        Span<float> orthoProjection = stackalloc float[]
        {
            2.0f/(R-L),   0.0f,         0.0f,   0.0f,
            0.0f,         2.0f/(T-B),   0.0f,   0.0f,
            0.0f,         0.0f,        -1.0f,   0.0f,
            (R+L)/(L-R),  (T+B)/(B-T),  0.0f,   1.0f,
        };

        GL.UseProgram(shaderProgram);
        GL.Uniform1(attribLocationTex, 0);
        GL.UniformMatrix4(attribLocationProjMtx, 1, false, orthoProjection.ToArray());

        GL.BindVertexArray(vao);

        // 渲染命令列表
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            // 上传顶点和索引数据
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                cmdList.VtxBuffer.Size * sizeof(ImDrawVert),
                cmdList.VtxBuffer.Data, BufferUsageHint.StreamDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                cmdList.IdxBuffer.Size * sizeof(ushort),
                cmdList.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            // 设置顶点属性
            GL.EnableVertexAttribArray(attribLocationVtxPos);
            GL.EnableVertexAttribArray(attribLocationVtxUV);
            GL.EnableVertexAttribArray(attribLocationVtxColor);

            GL.VertexAttribPointer(attribLocationVtxPos, 2, VertexAttribPointerType.Float,
                false, sizeof(ImDrawVert), (IntPtr)0);
            GL.VertexAttribPointer(attribLocationVtxUV, 2, VertexAttribPointerType.Float,
                false, sizeof(ImDrawVert), (IntPtr)8);
            GL.VertexAttribPointer(attribLocationVtxColor, 4, VertexAttribPointerType.UnsignedByte,
                true, sizeof(ImDrawVert), (IntPtr)16);

            // 渲染命令
            for (int cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmd_i];

                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    // 用户回调
                    continue;
                }

                // 设置裁剪矩形
                Vector4 clipRect = pcmd.ClipRect;
                GL.Scissor((int)clipRect.X, (int)(displayHeight - clipRect.W),
                    (int)(clipRect.Z - clipRect.X), (int)(clipRect.W - clipRect.Y));

                // 绑定纹理
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                // 绘制
                GL.DrawElements(PrimitiveType.Triangles, (int)pcmd.ElemCount,
                    DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)));
            }
        }

        // 恢复 GL 状态
        GL.UseProgram(lastProgram);
        GL.BindTexture(TextureTarget.Texture2D, lastTexture);
        GL.BindBuffer(BufferTarget.ArrayBuffer, lastArrayBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, lastElementArrayBuffer);
        GL.BindVertexArray(lastVertexArray);

        if (lastEnableBlend) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (lastEnableCullFace) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
        if (lastEnableDepthTest) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        if (lastEnableScissorTest) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
    }

    public void Dispose()
    {
        if (vao != 0) GL.DeleteVertexArray(vao);
        if (vbo != 0) GL.DeleteBuffer(vbo);
        if (ebo != 0) GL.DeleteBuffer(ebo);
        if (shaderProgram != 0) GL.DeleteProgram(shaderProgram);
        if (fontTexture != 0) GL.DeleteTexture(fontTexture);

        ImGui.DestroyContext();
    }
}
