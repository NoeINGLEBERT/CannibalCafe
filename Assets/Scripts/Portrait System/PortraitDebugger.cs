using UnityEngine;

public class PortraitDebugger : MonoBehaviour
{
    public PortraitRenderer renderer;

    void Start()
    {
        renderer.Render(PortraitGenerator.Generate(renderer.database));
    }
}
