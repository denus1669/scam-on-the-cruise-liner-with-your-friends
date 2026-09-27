using Assets.Casino.QuickOutline.Scripts;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class Highlither : MonoBehaviour
{
    [SerializeField] Outline outline;

    private void Awake()
    {
        SetOutlineEnable(false);
        if(outline == null)
        {
            outline = GetComponent<Outline>();
        }
    }

    public void SetOutlineEnable(bool isEnable)
    {
        outline.enabled = isEnable;
    }

    public void SetOutlineColor(Color color) 
    {
        outline.OutlineColor = color;
    }

}
