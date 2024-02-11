using Utilities.Data;

namespace RenderEngine;
public class RendererTimings
{
    public int TickSpeed;
    public int FrameRate;
    private SharedData? Data;

    public RendererTimings(SharedData data)
    { Refresh(data); }

    public void Refresh(SharedData data)
    {
        Data = data;
        TickSpeed = Data.Properties.Tick * 50;
        FrameRate = 1000 / Data.Properties.Refresh;
    }
}
