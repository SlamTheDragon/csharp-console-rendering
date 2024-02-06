using Utilities;

namespace RenderEngine;

internal sealed class RendererTimings : Configure
{
    public int TickSpeed;
    public int FrameRate;

    public RendererTimings()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (Properties != null)
        {
            TickSpeed = Properties.Tick * 50;
            FrameRate = 1000 / Properties.Refresh;
        }
    }
}
