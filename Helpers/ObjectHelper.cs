using UnityEngine;

namespace TOHE;

public static class ObjectHelper
{
    public static void DestroyTranslator(this GameObject obj)
    {
        var translator = obj.GetComponent<TextTranslatorTMP>();
        if (translator != null)
        {
            Object.Destroy(translator);
        }
    }
    public static void DestroyTranslator(this MonoBehaviour obj) => obj.gameObject.DestroyTranslator();
}
