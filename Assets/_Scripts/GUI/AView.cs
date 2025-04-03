using UnityEngine;

public abstract class AView : MonoBehaviour
{
    public abstract void Initialize();
    public virtual void DoHide()
    {
        gameObject.SetActive(false);
    }

    public virtual void DoShow(object args)
    {
        gameObject.SetActive(true);
    }
}
