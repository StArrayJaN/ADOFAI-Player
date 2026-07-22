using System.Numerics;
using OpenTK.Graphics.OpenGL;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Models;

    public class PlayerFloor : IDisposable
{
    private GLMesh mesh;
    private GLMesh textureMesh; // Separate mesh for texture rendering
    public readonly Floor floor;
    public bool isHit;
    public bool isCW;
    public bool isSelected; // 是否被选中
    public float noteTime;
    private bool _disposed;
    public GLTexture texture;
    public float textureAngle; // Icon rotation angle in radians
    public bool flipTexture; // Flip texture horizontally
    public bool flipTextureVertical; // Flip texture vertically
    public float textureScale = 1f; // Icon scale multiplier
    private Vector4[] _originalColors; // 保存原始颜色
    
    public PlayerFloor(Floor floor, GLTexture texture = null)
    {
        this.floor = floor;
        var poly = floor.GeneratePolygon();
        _originalColors = new Vector4[poly.colors.Length];
        for (int i = 0; i < poly.colors.Length; i++)
        {
            _originalColors[i] = new Vector4(
                poly.colors[i].R / 255.0f,
                poly.colors[i].G / 255.0f,
                poly.colors[i].B / 255.0f,
                poly.colors[i].A / 255.0f
            );
        }
        mesh = new (poly.vertices, poly.triangles.Select(a => (int)a).ToArray(), _originalColors)
        {
            Position = new(floor.position.X, floor.position.Y, 0)
        };
        this.texture = texture;

        // Create texture quad mesh
        CreateTextureQuad();
    }

    private void CreateTextureQuad()
    {
        // Scale texture to fit inside the floor polygon's white fill area
        // White fill uses (width - outline*2), and the shape varies by angle,
        // so use a conservative scale factor
        float size = Floor.width;

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-size, -size, 0),
            new Vector3(size, -size, 0),
            new Vector3(size, size, 0),
            new Vector3(-size, size, 0)
        };

        int[] indices = new int[] { 0, 1, 2, 0, 2, 3 };

        Vector4[] colors = new Vector4[]
        {
            new Vector4(1, 1, 1, 1),
            new Vector4(1, 1, 1, 1),
            new Vector4(1, 1, 1, 1),
            new Vector4(1, 1, 1, 1)
        };

        // Flip Y coordinate to match OpenGL texture coordinate system
        Vector2[] texCoords = new Vector2[]
        {
            new Vector2(0, 1),  // bottom-left
            new Vector2(1, 1),  // bottom-right
            new Vector2(1, 0),  // top-right
            new Vector2(0, 0)   // top-left
        };

        textureMesh = new GLMesh(vertices, indices, colors, texCoords)
        {
            Position = new(floor.position.X, floor.position.Y, 0)
        };
    }
    
    /// <summary>
    /// 设置选中状态，选中时显示为绿色
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        isSelected = selected;
        
        // 更新颜色
        var colors = new Vector4[_originalColors.Length];
        for (int i = 0; i < _originalColors.Length; i++)
        {
            if (selected)
            {
                // 选中时：将白色部分变为绿色，保持黑色边框
                if (_originalColors[i].X > 0.5f && _originalColors[i].Y > 0.5f && _originalColors[i].Z > 0.5f)
                {
                    // 白色部分变为绿色
                    colors[i] = new Vector4(0.2f, 0.8f, 0.2f, 1.0f);
                }
                else
                {
                    // 保持黑色边框
                    colors[i] = _originalColors[i];
                }
            }
            else
            {
                // 未选中时恢复原始颜�?
                colors[i] = _originalColors[i];
            }
        }
        
        mesh.UpdateColors(colors);
    }
    
    public void Render(IShader shader)
    {
        if (isHit) return;

        // Always disable texture for floor mesh rendering
        shader.SetInt("uUseTexture", 0);
        mesh.Render(shader);
    }

    /// <summary>
    /// Render texture overlay on top of the floor
    /// 在地板顶部渲染纹理覆盖层
    /// </summary>
    public void RenderTextureOverlay(IShader shader)
    {
        if (isHit || texture?.IsLoaded != true || textureMesh == null)
            return;

        // Ensure texture coordinates are enabled and bound
        textureMesh.Bind();
        
        // Bind texture
        texture.Bind(shader);

        // Render the texture quad with proper texture coordinates
        GL.DrawElements(PrimitiveType.Triangles, textureMesh.Indices.Length, DrawElementsType.UnsignedInt, 0);
        
        // Check for OpenGL errors
        var error = GL.GetError();
        if (error != ErrorCode.NoError)
        {
            Console.WriteLine($"[RenderTextureOverlay] OpenGL Error: {error}");
        }

        // Unbind texture
        texture.Unbind();
        
        // Unbind mesh
        textureMesh.Unbind();
    }
    
    public void ApplyTextureTransform()
    {
        if (textureMesh == null) return;

        float cosA = MathF.Cos(textureAngle);
        float sinA = MathF.Sin(textureAngle);

        // Base UVs (Y-compensated: V=1 at quad bottom)
        Vector2[] baseUV = new Vector2[]
        {
            new Vector2(0, 1),  // bottom-left
            new Vector2(1, 1),  // bottom-right
            new Vector2(1, 0),  // top-right
            new Vector2(0, 0)   // top-left
        };

        Vector2[] texCoords = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float u = baseUV[i].X;
            float v = baseUV[i].Y;

            // Convert Y-compensated → standard (V=0 at bottom)
            float vStd = 1f - v;

            // Center around (0.5, 0.5)
            float cu = u - 0.5f;
            float cv = vStd - 0.5f;

            // Rotate (matches ADOFAI shader: R(iconAngle) * uv1)
            float ru = cu * cosA - cv * sinA;
            float rv = cu * sinA + cv * cosA;

            // Shift back to [0, 1]
            float su = ru + 0.5f;
            float svStd = rv + 0.5f;

            // Convert back to Y-compensated
            float sv = 1f - svStd;

            // Flip AFTER rotation (matches ADOFAI shader)
            if (flipTexture)
                su = 1f - su;
            if (flipTextureVertical)
                sv = 1f - sv;

            // Scale UVs inward from center to avoid sampling edge pixels
            // When the icon rotates, UV corners can go outside [0,1] causing
            // the ClampToEdge to stretch edge pixels into visible colored blocks
            su = (su - 0.5f) * 0.88f + 0.5f;
            sv = (sv - 0.5f) * 0.88f + 0.5f;

            texCoords[i] = new Vector2(su, sv);
        }
        textureMesh.UpdateTexCoords(texCoords);
    }

    public Matrix4x4 GetTextureModelMatrix()
    {
        // Rotation is handled in UV space (ApplyTextureTransform), no vertex rotation needed
        Vector3 pos = textureMesh?.Position ?? Vector3.Zero;
        return Matrix4x4.CreateScale(textureScale) * Matrix4x4.CreateTranslation(pos);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        mesh?.Dispose();
        textureMesh?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

