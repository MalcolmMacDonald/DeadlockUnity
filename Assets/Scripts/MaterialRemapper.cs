using System.Collections.Generic;
using System.Text.RegularExpressions;
using EasyButtons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MaterialRemapper : MonoBehaviour
{
    private const string materialsPath = "Assets/DeadlockExtractedAssets/Materials/";
    private const string unusedMaterialsPath = "Assets/DeadlockExtractedAssets/UnusedMaterials/";
    private const string texturesPath = "Assets/DeadlockExtractedAssets/Textures/";

    [Button]
    public void LinkMaterialTextures()
    {
        var materials = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });
        var textureGUIDs = AssetDatabase.FindAssets("t:Texture", new[] { texturesPath });
        var textureNames = new List<string>();
        var textures = new List<Texture>();
        foreach (var texGuid in textureGUIDs)
        {
            var texPath = AssetDatabase.GUIDToAssetPath(texGuid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
            textureNames.Add(tex.name);
            textures.Add(tex);
        }

        foreach (var matGuid in materials)
        {
            var matPath = AssetDatabase.GUIDToAssetPath(matGuid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            var normalMap = mat.GetTexture("_BumpMap");
            var matName = "";
            matName = normalMap == null ? mat.name : normalMap.name;


            //first instance of number after underscore (eg. _01)
            var underscoreRegex = new Regex(@"_(\d+)");
            var match = underscoreRegex.Match(matName);
            if (match.Success)
            {
                matName = matName.Substring(0, match.Index);
            }

            Debug.Log(matName);

            //find all textures that start with the material name
            var foundTextures = FindTexturesForMaterial(matName, textureNames, textures);
            if (foundTextures.Length == 0)
            {
                Debug.LogWarning($"No textures found for material: {matName}");
                continue;
            }

            foreach (var foundTexture in foundTextures)
            {
                Debug.Log($"Found texture {foundTexture.name} for material {mat.name}");
                if (foundTexture.name.Contains("_orm_"))
                {
                    continue;
                }

                if (foundTexture.name.Contains("_color_"))
                {
                    mat.mainTexture = foundTexture;
                }
                else if (foundTexture.name.Contains("_ao_"))
                {
                    mat.SetTexture("_OcclusionMap", foundTexture);
                    mat.SetFloat("_OcclusionStrength", 1f);
                }
                else if (foundTexture.name.Contains("_trans_"))
                {
                    //set surface type to transparent
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetTexture("_BaseMap", foundTexture);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }
            }

            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
    }

    private Texture[] FindTexturesForMaterial(string matName, List<string> textureNames, List<Texture> textures)
    {
        var foundTextures = new List<Texture>();
        for (var i = 0; i < textureNames.Count; i++)
        {
            if (textureNames[i].StartsWith(matName))
            {
                foundTextures.Add(textures[i]);
            }
        }

        return foundTextures.ToArray();
    }
}


/*[Button]
public void RemapMaterials()
{
    var renderers = GetComponentsInChildren<Renderer>();
    foreach (var renderer in renderers)
    {
        var materials = renderer.sharedMaterials;
        for (var i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat == null)
            {
                continue;
            }

            //if material ends in .002 or any number, replace it with the .001 version
            var matNumber = mat.name.Substring(mat.name.Length - 3);

            if (!int.TryParse(matNumber, out var number))
            {
                continue;
            }

            if (mat.name.Substring(mat.name.Length - 4)[0] != '.')
            {
                continue;
            }

            var newMatName = mat.name.Substring(0, mat.name.Length - 4);
            Debug.Log($"Remapping material: {mat.name} to {newMatName}");

            var foundMats = AssetDatabase.FindAssets(newMatName + " t:Material", new[] { materialsPath });
            if (foundMats.Length == 0)
            {
                continue;
            }

            var newMatPath = AssetDatabase.GUIDToAssetPath(foundMats[0]);
            var newMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);
            if (newMat == null)
            {
                continue;
            }

            materials[i] = newMat;
        }


        renderer.sharedMaterials = materials;
    }
}

[Button]
public void DeleteDuplicateMaterials()

{
*/