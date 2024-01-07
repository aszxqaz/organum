using UnityEngine;
using UnityEditor;

using MixedReality.Toolkit.SpatialManipulation;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using UnityEngine.Windows;
using GLTFast;
using System.Collections.Generic;

#if WINDOWS_UWP
using System;
using Windows.Storage;
using Windows.System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
#endif

public class ModelLoader : MonoBehaviour
{
    static List<GameObject> currentObjects = new(10);

    public void New()
    {
        ClearAll();
        Add();
    }

    public async void Add()
    {
        var gltf = await PickModel();
        if (gltf == null) return;
        Instantiate(gltf);
    }

    private async Task<GltfImport> PickModel()
    {
        var bytes = await GetModelBytes();
        if (bytes == null) return null;
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(bytes);
        return success ? gltf : null;
    }

    private async Task<byte[]> GetModelBytes()
    {
        var source = new TaskCompletionSource<byte[]>();
#if !UNITY_EDITOR && UNITY_WSA_10_0
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            var filepicker = new FileOpenPicker();
            filepicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filepicker.FileTypeFilter.Add(".glb");
            filepicker.FileTypeFilter.Add(".gltf");

            var file = await filepicker.PickSingleFileAsync();

            UnityEngine.WSA.Application.InvokeOnAppThread(async () =>
            {
                if (file != null) 
                {
                    var buffer = await FileIO.ReadBufferAsync(file);
                    var bytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(buffer);
                    source.SetResult(bytes);
                } else 
                {
                    source.SetResult(null);
                }
            }, false);
        }, false);
#else
        string path = EditorUtility.OpenFilePanel("Choose model", "", "glb");
        if (path != null && path.Length > 0)
        {
            var bytes = File.ReadAllBytes(path);
            source.SetResult(bytes);
        }
        else
        {
            source.SetResult(null);
        }
#endif
        return await source.Task;
    }

    private void Instantiate(GltfImport gltf)
    {
        var obj = new GameObject();
        obj.transform.Translate(new Vector3(0, 1.2f, 1.2f));
        gltf.InstantiateScene(obj.transform);
        GameObject scene = obj.GetNamedChild("Scene");
        List<GameObject> children = new(10);
        scene.GetChildGameObjects(children);

        foreach (GameObject child in children)
        {
            child.AddComponent<BoxCollider>();
            var manipul = child.AddComponent<ObjectManipulator>();
            manipul.selectMode = UnityEngine.XR.Interaction.Toolkit.InteractableSelectMode.Multiple;
        }

        currentObjects.Add(obj);
    }

    private void ClearAll()
    {
        foreach (GameObject cur in currentObjects)
        {
            Destroy(cur);
        }
        currentObjects.Clear();
    }
}
