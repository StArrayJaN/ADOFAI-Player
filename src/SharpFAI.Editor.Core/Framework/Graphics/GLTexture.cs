using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using SharpFAI.Framework;
using SharpFAI.Editor.Core.Framework.Assets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// Cross-platform OpenGL texture implementation using SixLabors.ImageSharp
/// 使用SixLabors.ImageSharp的跨平台OpenGL纹理实现
/// </summary>
public class GLTexture : ITexture
{
    private int _textureId;
    private int _originalWidth;
    private int _originalHeight;
    private int _maxWidth;
    private int _maxHeight;
    private string _texturePath;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Create an empty GL texture / 创建空GL纹理
    /// </summary>
    public GLTexture()
    {
        _textureId = 0;
        Width = 0;
        Height = 0;
        IsLoaded = false;
        _texturePath = string.Empty;
    }

    /// <summary>
    /// Create a GL texture with path and load immediately / 使用路径创建GL纹理并立即加载
    /// </summary>
    public GLTexture(string path, bool autoLoad = false)
    {
        _textureId = 0;
        Width = 0;
        Height = 0;
        IsLoaded = false;
        _texturePath = path;
        if (autoLoad)
        {
            try
            {
                Load();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load texture: {ex.Message}");
            }
        }
    }

    public void Load(string path)
    {
        _texturePath = path;
        Load();
    }

    public void Update(Vector2 offset, Vector2 size, byte[] rgba)
    {
        if (!IsLoaded || _textureId <= 0)
            return;

        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        GL.TexSubImage2D(
            TextureTarget.Texture2D,
            0,                          // mipmap level
            (int)offset.X,              // x offset
            (int)offset.Y,              // y offset
            (int)size.X,                // width
            (int)size.Y,                // height
            PixelFormat.Rgba,           // format
            PixelType.UnsignedByte,     // data type
            rgba                        // pixel data
        );
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(IShader shader)
    {
        if (!IsLoaded || _textureId <= 0)
            return;

        // Activate texture unit 0 (default for most basic shaders)
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        // Set the texture uniform in the shader if provided
        if (shader != null)
        {
            shader.SetInt("uTexture", 0); // Set texture sampler to use texture unit 0
        }
    }

    public void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void SetFilter(TextureFilter min, TextureFilter mag)
    {
        if (!IsLoaded || _textureId <= 0)
            return;

        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        // Set minification filter
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)(min == TextureFilter.Linear ? TextureMinFilter.Linear : TextureMinFilter.Nearest));

        // Set magnification filter
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)(mag == TextureFilter.Linear ? TextureMagFilter.Linear : TextureMagFilter.Nearest));

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void SetWrap(TextureWrap s, TextureWrap t)
    {
        if (!IsLoaded || _textureId <= 0)
            return;

        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        // Set horizontal wrapping mode
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)(s == TextureWrap.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge));

        // Set vertical wrapping mode
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)(t == TextureWrap.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge));

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        if (_textureId > 0)
        {
            GL.DeleteTexture(_textureId);
            _textureId = 0;
        }
        IsLoaded = false;
    }

    /// <summary>
    /// Set maximum texture size and scale accordingly / 设置最大纹理大小并相应缩放
    /// </summary>
    public void SetMaxSize(int maxWidth, int maxHeight)
    {
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
        ScaleToMaxSize();
    }

    /// <summary>
    /// Scale texture dimensions to fit within max size while maintaining aspect ratio
    /// 缩放纹理尺寸以适应最大大小，同时保持宽高比
    /// </summary>
    private void ScaleToMaxSize()
    {
        if (_maxWidth <= 0 || _maxHeight <= 0 || _originalWidth <= 0 || _originalHeight <= 0)
            return;

        if (_originalWidth > _maxWidth || _originalHeight > _maxHeight)
        {
            float scaleX = (float)_maxWidth / _originalWidth;
            float scaleY = (float)_maxHeight / _originalHeight;
            float scale = Math.Min(scaleX, scaleY);

            Width = (int)(_originalWidth * scale);
            Height = (int)(_originalHeight * scale);
        }
        else
        {
            Width = _originalWidth;
            Height = _originalHeight;
        }
    }

    /// <summary>
    /// Load texture from stored path / 从存储的路径加载纹理
    /// </summary>
    public void Load()
    {
        if (IsLoaded) return;

        if (string.IsNullOrEmpty(_texturePath))
            throw new InvalidOperationException("Texture path is not set");

        try
        {
            // Get full path from asset manager
            string fullPath = AssetManager.GetAssetPath(_texturePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Texture file not found: {fullPath}");

            // Delete existing texture if any
            if (_textureId > 0)
            {
                GL.DeleteTexture(_textureId);
                _textureId = 0;
            }

            // Load image using ImageSharp
            using (var image = Image.Load<Rgba32>(fullPath))
            {
                _originalWidth = image.Width;
                _originalHeight = image.Height;
                Console.WriteLine($"[GLTexture] Loaded image: {fullPath}, Original size: {_originalWidth}x{_originalHeight}");

                // Apply max size constraints if set
                ScaleToMaxSize();
                Console.WriteLine($"[GLTexture] After ScaleToMaxSize: Width={Width}, Height={Height}, _maxWidth={_maxWidth}, _maxHeight={_maxHeight}");
                
                // If Width and Height are still 0 (no max size set), use original size
                if (Width <= 0 || Height <= 0)
                {
                    Width = _originalWidth;
                    Height = _originalHeight;
                    Console.WriteLine($"[GLTexture] Using original size: {Width}x{Height}");
                }

                // If scaling is needed, resize the image
                Image<Rgba32> textureImage = image;
                if ((Width != _originalWidth || Height != _originalHeight) && Width > 0 && Height > 0)
                {
                    textureImage = image.Clone(x => x.Resize(Width, Height));
                    Console.WriteLine($"[GLTexture] Resized image to: {Width}x{Height}");
                }
                else
                {
                    Console.WriteLine($"[GLTexture] Using original image without resizing");
                }

                // Generate texture ID
                _textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _textureId);

                // Get pixel data
                byte[] pixelData = new byte[textureImage.Width * textureImage.Height * 4];
                textureImage.CopyPixelDataTo(pixelData);

                // Upload texture data to GPU
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,                          // mipmap level
                    PixelInternalFormat.Rgba,   // internal format
                    textureImage.Width,         // width
                    textureImage.Height,        // height
                    0,                          // border
                    PixelFormat.Rgba,           // format (RGBA for ImageSharp)
                    PixelType.UnsignedByte,     // data type
                    pixelData                   // pixel data
                );

                // Clean up resized image if created
                if (textureImage != image)
                    textureImage.Dispose();
            }

            // Set default filtering and wrapping modes
            SetFilter(TextureFilter.Linear, TextureFilter.Linear);
            SetWrap(TextureWrap.ClampToEdge, TextureWrap.ClampToEdge);

            // Generate mipmaps for better quality when scaling down
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            // Unbind texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load texture '{_texturePath}': {ex.Message}");
            if (_textureId > 0)
            {
                GL.DeleteTexture(_textureId);
                _textureId = 0;
            }
            IsLoaded = false;
            throw;
        }
    }
    /// <summary>
    /// Get the OpenGL texture ID / 获取OpenGL纹理ID
    /// </summary>
    public int GetTextureId()
    {
        return _textureId;
    }
}
