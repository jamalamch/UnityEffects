using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Script.Utils;

/// <summary>
/// Deep illustration of the lamp
/// </summary>
public class CreateDepthMap : MonoBehaviour 
{
    public Shader depthMapShader;
    private Camera _mainCamera;//Main camera
    private Camera _lightCamera;//Light camera
    private List<Vector4> _vList = new List<Vector4>();
	void Start () 
    {
        _lightCamera = GetComponent<Camera>();
        _lightCamera.depthTextureMode = DepthTextureMode.Depth;
        _lightCamera.clearFlags = CameraClearFlags.SolidColor;
        _lightCamera.backgroundColor = Color.white;//The background is set to white, indicating that the background is far away from the perspective, and it will not be affected by the shadow.
        _lightCamera.SetReplacementShader(depthMapShader, "RenderType");//Rendertype type to generate depth maps with replacement rendering methods
        RenderTexture depthMap = new RenderTexture(Screen.width, Screen.height, 0);
        depthMap.format = RenderTextureFormat.ARGB32;
        _lightCamera.targetTexture = depthMap;
        //
        foreach (Camera item in Camera.allCameras)
        {
            if (item.CompareTag("MainCamera"))
            {
                _mainCamera = item;
                break;
            }
        }
	}

    void LateUpdate()
    {
        ShadowUtils.SetLightCamera(_mainCamera, _lightCamera);
    }
}
