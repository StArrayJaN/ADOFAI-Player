using System.Numerics;
using SharpFAI.Framework;

namespace SharpFAI_Player.Framework;

public class PlayerFloor : IDisposable
{
    private GLMesh mesh;
    public readonly Floor floor;
    public bool isHit;
    private bool _disposed;
    
    public PlayerFloor(Floor floor)
    {
        this.floor = floor;
        var poly = floor.GeneratePolygon();
        var colors = new Vector4[poly.colors.Length];
        for (int i = 0; i < poly.colors.Length; i++)
        {
            colors[i] = new Vector4(
                poly.colors[i].R / 255.0f,
                poly.colors[i].G / 255.0f,
                poly.colors[i].B / 255.0f,
                poly.colors[i].A / 255.0f
            );
        }
        mesh = new (poly.vertices, poly.triangles.Select(a => (int)a).ToArray(), colors)
        {
            Position = new(floor.position.X, floor.position.Y, 0)
        };
    }
    
    public void Render(IShader shader)
    {
        if (isHit) return;
        mesh.Render(shader);
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        mesh?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}