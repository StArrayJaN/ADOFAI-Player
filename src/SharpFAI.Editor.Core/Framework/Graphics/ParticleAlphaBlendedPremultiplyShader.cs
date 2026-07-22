using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpFAI.Editor.Core.Framework.Graphics
{
    /// <summary>
    /// 粒子 Alpha 混合预乘着色器程序
    /// 对应 Unity 的 "Legacy Shaders/Particles/Alpha Blended Premultiply"
    /// </summary>
    public class ParticleAlphaBlendedPremultiplyShader : IDisposable
    {
        private int _programId;
        private int _vertexShaderId;
        private int _fragmentShaderId;

        // Uniform 位置缓存
        private int _projectionMatrixLoc;
        private int _viewMatrixLoc;
        private int _modelMatrixLoc;
        private int _mainTexLoc;
        private int _mainTexSTLoc;
        private int _cameraDepthTextureLoc;
        private int _invFadeLoc;

        public int ProgramId => _programId;

        public ParticleAlphaBlendedPremultiplyShader(string vertexShaderPath, string fragmentShaderPath)
        {
            CompileShaders(vertexShaderPath, fragmentShaderPath);
            CacheUniformLocations();
        }

        private void CompileShaders(string vertexPath, string fragmentPath)
        {
            // 读取着色器源代码
            string vertexSource = System.IO.File.ReadAllText(vertexPath);
            string fragmentSource = System.IO.File.ReadAllText(fragmentPath);

            // 编译顶点着色器
            _vertexShaderId = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShaderId, vertexSource);
            GL.CompileShader(_vertexShaderId);

            // 检查顶点着色器编译错误
            GL.GetShader(_vertexShaderId, ShaderParameter.CompileStatus, out int vertexSuccess);
            if (vertexSuccess == 0)
            {
                string infoLog = GL.GetShaderInfoLog(_vertexShaderId);
                throw new Exception($"顶点着色器编译失败:\n{infoLog}");
            }

            // 编译片段着色器
            _fragmentShaderId = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_fragmentShaderId, fragmentSource);
            GL.CompileShader(_fragmentShaderId);

            // 检查片段着色器编译错误
            GL.GetShader(_fragmentShaderId, ShaderParameter.CompileStatus, out int fragmentSuccess);
            if (fragmentSuccess == 0)
            {
                string infoLog = GL.GetShaderInfoLog(_fragmentShaderId);
                throw new Exception($"片段着色器编译失败:\n{infoLog}");
            }

            // 链接程序
            _programId = GL.CreateProgram();
            GL.AttachShader(_programId, _vertexShaderId);
            GL.AttachShader(_programId, _fragmentShaderId);
            GL.LinkProgram(_programId);

            // 检查链接错误
            GL.GetProgram(_programId, GetProgramParameterName.LinkStatus, out int linkSuccess);
            if (linkSuccess == 0)
            {
                string infoLog = GL.GetProgramInfoLog(_programId);
                throw new Exception($"着色器程序链接失败:\n{infoLog}");
            }

            // 清理着色器对象
            GL.DetachShader(_programId, _vertexShaderId);
            GL.DetachShader(_programId, _fragmentShaderId);
            GL.DeleteShader(_vertexShaderId);
            GL.DeleteShader(_fragmentShaderId);
        }

        private void CacheUniformLocations()
        {
            GL.UseProgram(_programId);

            _projectionMatrixLoc = GL.GetUniformLocation(_programId, "uProjectionMatrix");
            _viewMatrixLoc = GL.GetUniformLocation(_programId, "uViewMatrix");
            _modelMatrixLoc = GL.GetUniformLocation(_programId, "uModelMatrix");
            _mainTexLoc = GL.GetUniformLocation(_programId, "uMainTex");
            _mainTexSTLoc = GL.GetUniformLocation(_programId, "uMainTex_ST");
            _cameraDepthTextureLoc = GL.GetUniformLocation(_programId, "uCameraDepthTexture");
            _invFadeLoc = GL.GetUniformLocation(_programId, "uInvFade");

            GL.UseProgram(0);
        }

        public void Use()
        {
            GL.UseProgram(_programId);
        }

        public void SetProjectionMatrix(Matrix4 matrix)
        {
            GL.UniformMatrix4(_projectionMatrixLoc, false, ref matrix);
        }

        public void SetViewMatrix(Matrix4 matrix)
        {
            GL.UniformMatrix4(_viewMatrixLoc, false, ref matrix);
        }

        public void SetModelMatrix(Matrix4 matrix)
        {
            GL.UniformMatrix4(_modelMatrixLoc, false, ref matrix);
        }

        public void SetMainTexture(int textureUnit)
        {
            GL.Uniform1(_mainTexLoc, textureUnit);
        }

        public void SetMainTextureST(Vector4 st)
        {
            GL.Uniform4(_mainTexSTLoc, st);
        }

        public void SetCameraDepthTexture(int textureUnit)
        {
            GL.Uniform1(_cameraDepthTextureLoc, textureUnit);
        }

        public void SetInvFade(float invFade)
        {
            GL.Uniform1(_invFadeLoc, invFade);
        }

        public void Dispose()
        {
            if (_programId != 0)
            {
                GL.DeleteProgram(_programId);
                _programId = 0;
            }
        }
    }
}
