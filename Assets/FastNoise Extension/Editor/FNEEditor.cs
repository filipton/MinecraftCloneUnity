using UnityEditor;
using UnityEngine;

public class FNEEditor : MonoBehaviour
{
    [MenuItem("Assets/Create/Noise")]
    static void CreateNP()
    {
        var selected = Selection.activeObject;
        FastNoiseExtension newNP = new FastNoiseExtension();
        AssetDatabase.CreateAsset(newNP, AssetDatabase.GetAssetPath(selected) + "/New Noise.asset");
        AssetDatabase.SaveAssets();
    }
}